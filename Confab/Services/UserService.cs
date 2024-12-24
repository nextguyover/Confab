using Confab.Data;
using Confab.Models;
using Confab.Data.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Confab.Exceptions;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.UserAuth;
using Confab.Services.Interfaces;
using Confab.Emails.TemplateSubstitution;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Net;

namespace Confab.Services
{
    public class UserService : IUserService
    {
        public static int VerificationCodeExpirySeconds = 300;
        public static int MaxVerificationCodeAttempts = 3;

        public static int MaxVerificationCodeEmails = 5;
        public static int MaxVerificationCodeEmailResetDurationHours = 24;

        public static int MaxNewSignups = -1;
        public static int MaxNewSignupsDurationMinutes = 60;

        public static bool AnonymousCommentingEnabled = false;

        public static bool CustomUsernamesEnabled = false;
        public static int UsernameChangeCooldownTimeMins = 60;

        public static string JwtKey;
        public static TokenValidationParameters JwtValidationParams;
        
        private static ILogger _logger;
        public static ILogger logger { set { _logger = value; } }

        public static async Task UpdateLastActive(UserSchema author, DataContext dbCtx)
        {
            author.LastActive = DateTime.UtcNow;
            dbCtx.Users.Update(author);
            await dbCtx.SaveChangesAsync();
        }
        public static async Task<UserSchema> GetUserFromJWT(HttpContext httpContext, DataContext dbCtx)
        {
            int userId = Convert.ToInt32(AuthClaims.Claims.GetClaim(httpContext, (await dbCtx.GlobalSettings.SingleAsync()).UserAuthJwtValidityStart, AuthClaims.Claims.Id));
            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.Id.Equals(userId));

            if(user != null)
                await UpdateLastActive(user, dbCtx);

            return user;
        }

        public async Task SendVerificationCode(UserLogin userLogin, IEmailService emailService, ICommentLocationService locationService, DataContext dbCtx, bool isDevelopment)
        {
            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.Email.Equals(userLogin.Email));

            await UserService.EnsureNotBanned(user, dbCtx);

            CommentLocationSchema parsedLocation = null;
            try
            {
                parsedLocation = await locationService.GetLocation(dbCtx, userLogin?.Location);
            }
            catch { }

            if (user?.Role != UserRole.Admin && (userLogin.Location == null || parsedLocation == null))
            {
                throw new UninitialisedLocationException();
            }

            string verificationCode = GenerateRandomNumberString(6);

            if(user == null)    //if user doesn't exist, create user record
            {
                if(!(await dbCtx.GlobalSettings.SingleAsync()).AccountCreationEnabled)
                {
                    throw new AccountCreationDisabledException();
                }
                user = await CreateNewUser(dbCtx, UserRole.Standard, userLogin.Email);
            }

            await VerifyUserLoginEnabled(user, dbCtx);

            //check whether a verification code has been sent recently
            if(user.VerificationExpiry > DateTime.UtcNow)
            {
                throw new RateLimitException();
            }

            //check whether too many verification emails have been sent in the past given duration
            if(user.VerificationCodeEmailCount >= MaxVerificationCodeEmails)     //if exceeded number of max verification emails
            {
                if(user.VerificationCodeFirstEmail > DateTime.UtcNow.AddHours(-1 * MaxVerificationCodeEmailResetDurationHours))    //if verification code cooldown has not passed
                {
                    throw new VerificationEmailsRateLimitException();
                }
                else    //if verification code cooldown has elapsed, reset verification code email counter
                {
                    user.VerificationCodeEmailCount = 0;
                }
            }

            user.VerificationCode = verificationCode;
            user.VerificationExpiry = DateTime.UtcNow.AddSeconds(VerificationCodeExpirySeconds);
            user.VerificationCodeAttempts = 0;
            user.LastActive = DateTime.UtcNow;

            _logger.LogDebug($"Sending verification code {user.VerificationCode} to {user.Email}");

            NameValueCollection autoLoginQueryString = System.Web.HttpUtility.ParseQueryString(string.Empty);   //https://stackoverflow.com/a/1877016/9112181
            autoLoginQueryString.Add("Confab_email", user.Email);
            autoLoginQueryString.Add("Confab_authCode", user.VerificationCode);

            if (await emailService.SendEmail(new AuthCodeTemplatingData 
                { 
                    AuthCode = user.VerificationCode,
                    AuthCodeAutoLoginURL = AllEmailsTemplatingScaffold.SiteUrl + (parsedLocation?.LocationStr ?? "") + "?" + autoLoginQueryString.ToString(),
                    UserEmail = user.Email,
                    Username = UserService.GetUsername(user),
                    UserProfilePicUrl = AllEmailsTemplatingScaffold.SiteUrl + "/user/get-profile-picture/" + user.PublicId,
            }))
            {
                if(user.VerificationCodeEmailCount == 0)
                {
                    user.VerificationCodeFirstEmail = DateTime.UtcNow;
                }
                user.VerificationCodeEmailCount += 1;

                dbCtx.Users.Update(user);
                await dbCtx.SaveChangesAsync();
            }
            else
            {
                if(!isDevelopment)
                {
                    user.VerificationExpiry = DateTime.MinValue;
                }

                dbCtx.Users.Update(user);
                await dbCtx.SaveChangesAsync();

                throw new EmailSendErrorException();
            }
        }

        public async Task<UserSchema> CreateNewUser(DataContext dbCtx, UserRole role, string email)
        {
            if(MaxNewSignups != -1 && await dbCtx.Users.Where(u => 
                u.RecordCreation > DateTime.UtcNow.AddMinutes(MaxNewSignupsDurationMinutes * -1) &&
                u.AccountCreation == DateTime.MinValue  // only count new sign ups that haven't logged in yet
            ).CountAsync() >= MaxNewSignups)
            {
                throw new MaxNewSignupsLimitException();
            }

            UserSchema user = new UserSchema();
            user.Role = role;
            user.RecordCreation = DateTime.UtcNow;

            if (!EmailService.ValidateEmail(email))
            {
                _logger.LogError("User bypassed UI email validity check, or invalid account email(s) in appsettings.json");
                throw new InvalidEmailException();
            }
            user.Email = email;
            
            user.PublicId = GenerateUserId();

            dbCtx.Users.Add(user);
            await dbCtx.SaveChangesAsync();

            return user;
        }
        
        public async Task<UserSchema> CreateNewAnonUser(DataContext context, ClientIPSchema clientIP)
        {
            UserSchema user = new UserSchema(){
                CreationIP = clientIP,
                IsAnon = true,
                PublicId = GenerateUserId(),
                Role = UserRole.Standard,
                RecordCreation = DateTime.UtcNow
            };
            
            user.PublicId = GenerateUserId();

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }
        
        private static string GenerateUserId()        //https://stackoverflow.com/a/1344258/9112181
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return "u_" + new string(stringChars);
        }

        static string GenerateRandomNumberString(int length)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                StringBuilder sb = new StringBuilder();

                foreach (byte b in randomBytes)
                {
                    int digit = b % 10; // Ensure it's a single digit
                    sb.Append(digit);
                }

                return sb.ToString();
            }
        }

        public async Task<LoginResponse> Login(UserLogin userLogin, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.Email.Equals(userLogin.Email));
            
            UserSchema anonUser = null;
            try{
                anonUser = await GetUserFromJWT(httpContext, dbCtx); // decode JWT, get anon user (if sent)
            } catch (MissingAuthorizationException) {}  //if no JWT, don't assign anonUser

            if (user == null)    //if user doesn't exist, can't login
            {
                throw new UserNotFoundException();
            }

            await VerifyUserLoginEnabled(user, dbCtx);

            await UserService.EnsureNotBanned(user, dbCtx);
            await UserService.EnsureNotBanned(anonUser, dbCtx); // check if anon user is banned (prevents merge)

            // check if verification code has expired
            if (user.VerificationExpiry < DateTime.UtcNow || user.VerificationCodeAttempts >= MaxVerificationCodeAttempts)
            {
                user.VerificationCodeAttempts += 1;
                user.VerificationExpiry = DateTime.MinValue;
                dbCtx.Users.Update(user);
                await dbCtx.SaveChangesAsync();
                throw new UserLoginVerificationCodeExpiredException();
            }

            // check if verification code is incorrect
            if (!user.VerificationCode.Equals(userLogin.LoginCode))
            {
                user.VerificationCodeAttempts += 1;
                dbCtx.Users.Update(user);
                await dbCtx.SaveChangesAsync();
                throw new UserLoginFailedException();
            }

            // everything checks out, proceed with login

            // update user record to reflect successful login
            user.VerificationExpiry = DateTime.MinValue;
            user.VerificationCode = null;
            user.LastActive = DateTime.UtcNow;

            user.VerificationCodeEmailCount = 0;
            user.VerificationCodeFirstEmail = DateTime.MinValue;

            if (user.AccountCreation == DateTime.MinValue)
            {
                user.AccountCreation = DateTime.UtcNow;
            }

            dbCtx.Users.Update(user);
            await dbCtx.SaveChangesAsync();

            // check if merge anon account is requested
            if (userLogin.MergeAnonAccount)
            {
                if (anonUser != null)
                {
                    await MergeAnonAccount(user, anonUser, dbCtx);
                }
            }

            // generate JWT
            var claims = new[]
            {
                new Claim(AuthClaims.Claims.Id, user.Id.ToString()),
                //new Claim(AuthClaims.Claims.Email, user.Email),
                //new Claim(AuthClaims.Claims.Role, user.Role.ToString()),
                //new Claim(AuthClaims.Claims.PublicId, user.PublicId),
                //new Claim(AuthClaims.Claims.IsBanned, user.PublicId),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));

            var token = new JwtSecurityTokenHandler().CreateJwtSecurityToken(
                    issuer: JwtValidationParams.ValidIssuer,
                    audience: JwtValidationParams.ValidAudience,
                    subject: new ClaimsIdentity(claims),
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddDays(7),
                    null,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                    encryptingCredentials: new EncryptingCredentials(key, JwtConstants.DirectKeyUseAlg,
                        SecurityAlgorithms.Aes256CbcHmacSha512)
                );

            return new LoginResponse
            {
                Outcome = LoginOutcome.Success,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }

        private async Task MergeAnonAccount(UserSchema authenticatedUser, UserSchema anonUser, DataContext dbCtx)
        {
            // count number of authenticatedUser's comments before merge
            int authUserInitialCommentCount = await dbCtx.Comments.Where(c => c.Author == authenticatedUser).CountAsync();

            // change all anonUser's comments to authenticatedUser
            List<CommentSchema> anonUserComments = await dbCtx.Comments.Where(c => c.Author == anonUser).ToListAsync();

            foreach (CommentSchema comment in anonUserComments)
            {
                comment.Author = authenticatedUser;
                dbCtx.Comments.Update(comment);
            }

            // port anonUser's votes to authenticatedUser.
            await dbCtx.Entry(anonUser).Collection(u => u.UpvotedComments).LoadAsync();
            await dbCtx.Entry(anonUser).Collection(u => u.DownvotedComments).LoadAsync();

            await dbCtx.Entry(authenticatedUser).Collection(u => u.UpvotedComments).LoadAsync();
            await dbCtx.Entry(authenticatedUser).Collection(u => u.DownvotedComments).LoadAsync();

            // if both users have voted on the same comment, anonUser's vote will overwrite authenticatedUser's vote
            foreach (CommentSchema comment in anonUser.UpvotedComments)
            {
                if (!authenticatedUser.UpvotedComments.Contains(comment))
                {
                    authenticatedUser.UpvotedComments.Add(comment);
                }
                authenticatedUser.DownvotedComments.Remove(comment);
            }

            foreach (CommentSchema comment in anonUser.DownvotedComments)
            {
                if (!authenticatedUser.DownvotedComments.Contains(comment))
                {
                    authenticatedUser.DownvotedComments.Add(comment);
                }
                authenticatedUser.UpvotedComments.Remove(comment);
            }

            // if authenticatedUser previously did not have comments, change authenticatedUser's publicId to anonUser's publicId
            if (authUserInitialCommentCount != 0)
            {
                authenticatedUser.PublicId = anonUser.PublicId;
            }

            // update authenticatedUser
            dbCtx.Users.Update(authenticatedUser);
            // and delete anonUser
            dbCtx.Users.Remove(anonUser);

            await dbCtx.SaveChangesAsync();
        }

        public async Task<LoginResponse> AnonLogin(HttpContext httpContext, DataContext dbCtx)
        {
            if (!UserService.AnonymousCommentingEnabled) throw new AnonymousCommentingDisabledException();

            IPAddress clientIP = GetClientIP(httpContext);

            ClientIPSchema IPRecord = await dbCtx.ClientIPs.SingleOrDefaultAsync(o => o.IPAddressBytes.Equals(clientIP.GetAddressBytes()));
            if(IPRecord == null)
            {
                IPRecord = new ClientIPSchema { IPAddress = clientIP };
                dbCtx.ClientIPs.Add(IPRecord);
            }

            if(IPRecord.IsBanned)
            {
                throw new UserBannedException();
            }

            // check if account creation is disabled
            if(!(await dbCtx.GlobalSettings.SingleAsync()).AccountCreationEnabled)
            {
                throw new AccountCreationDisabledException();
            }
            // check if account login is disabled
            if(!(await dbCtx.GlobalSettings.SingleAsync()).AccountLoginEnabled)
            {
                throw new UserLoginDisabledException();
            }

            UserSchema user = await CreateNewAnonUser(dbCtx, IPRecord);
            await dbCtx.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(AuthClaims.Claims.Id, user.Id.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));

            var token = new JwtSecurityTokenHandler().CreateJwtSecurityToken(
                    issuer: JwtValidationParams.ValidIssuer,
                    audience: JwtValidationParams.ValidAudience,
                    subject: new ClaimsIdentity(claims),
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddDays(365),
                    null,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                    encryptingCredentials: new EncryptingCredentials(key, JwtConstants.DirectKeyUseAlg,
                        SecurityAlgorithms.Aes256CbcHmacSha512)
                );

            return new LoginResponse { 
                Outcome = LoginOutcome.Success, 
                Token = new JwtSecurityTokenHandler().WriteToken(token) 
            };
        }

        private static IPAddress GetClientIP(HttpContext httpContext)   // https://stackoverflow.com/a/45046326/9112181
        {
            HttpRequest request = httpContext.Request;

            // handle standardized 'Forwarded' header
            string forwarded = request.Headers["Forwarded"];
            if (!String.IsNullOrEmpty(forwarded))
            {
                foreach (string segment in forwarded.Split(',')[0].Split(';'))
                {
                    string[] pair = segment.Trim().Split('=');
                    if (pair.Length == 2 && pair[0].Equals("for", StringComparison.OrdinalIgnoreCase))
                    {
                        string ip = pair[1].Trim('"');

                        // IPv6 addresses are always enclosed in square brackets
                        int left = ip.IndexOf('['), right = ip.IndexOf(']');
                        if (left == 0 && right > 0)
                        {
                            return IPAddress.Parse(ip.Substring(1, right - 1));
                        }

                        // strip port of IPv4 addresses
                        int colon = ip.IndexOf(':');
                        if (colon != -1)
                        {
                            return IPAddress.Parse(ip.Substring(0, colon));
                        }

                        // this will return IPv4, "unknown", and obfuscated addresses
                        return IPAddress.Parse(ip);
                    }
                }
            }

            // handle non-standardized 'X-Forwarded-For' header
            string xForwardedFor = request.Headers["X-Forwarded-For"];
            if (!String.IsNullOrEmpty(xForwardedFor))
            {
                return IPAddress.Parse(xForwardedFor.Split(',')[0]);
            }

            return httpContext.Connection.RemoteIpAddress;
        }

        private async Task VerifyUserLoginEnabled(UserSchema user, DataContext dbCtx)
        {
            if(user.Role != UserRole.Admin && !(await dbCtx.GlobalSettings.SingleAsync()).AccountLoginEnabled)
            {
                throw new UserLoginDisabledException();
            }
        }

        public async Task<bool> GetChangeUsernameAvailable(HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);       //TODO: do authorisation in program.cs with a single function

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if (user.IsAnon) return false;

            return CustomUsernamesEnabled || user.Role == UserRole.Admin;
        }

        public async Task ChangeUsername(UsernameChange usernameChange, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if((!CustomUsernamesEnabled && user.Role != UserRole.Admin) || user.IsAnon)
            {
                throw new CustomUsernameNotAllowedException();
            }

            if (usernameChange.NewUsername.IsNullOrEmpty())
            {
                usernameChange.NewUsername = null;
            }
            else
            {
                await ValidateUsername(user, usernameChange.NewUsername, dbCtx);
            }

            if(user.Role != UserRole.Admin && user.LastUsernameChange.AddMinutes(UsernameChangeCooldownTimeMins) > DateTime.UtcNow)
            {
                throw new RateLimitException();
            }

            user.LastUsernameChange = DateTime.UtcNow;

            user.Username = usernameChange.NewUsername;
            dbCtx.Users.Update(user);
            await dbCtx.SaveChangesAsync();
        }

        private async Task ValidateUsername(UserSchema user, string username, DataContext dbCtx)
        {
            if (user.Role == UserRole.Admin) return;

            if (username.Length < 2 || username.Length > 15 || !Regex.Match(username, "^[a-zA-Z0-9_]+$").Success)            //username can be empty (starts using anonymous username again)
                throw new InvalidUsernameException();

            if (await dbCtx.Users.AnyAsync(u => u.Username == username && u != user))
                throw new UsernameUnavailableException();

            if(username.ToLower() == "anonymous")
                throw new UsernameUnavailableException();
        }

        public async Task<bool> UserIdExists(string publicUserId, DataContext dbCtx)
        {
            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.PublicId.Equals(publicUserId));

            return user != null;
        }

        public static string GetUsername(UserSchema userSchema)
        {
            return userSchema?.Username == null || (!CustomUsernamesEnabled && userSchema.Role != UserRole.Admin) 
                ? GenerateAnonUsername(userSchema) : userSchema?.Username;
        }

        private static string GenerateAnonUsername(UserSchema userSchema)
        {
            string[] adjectiveArr = { "abiding", "able", "absorbing", "abundant", "acclaimed", "active", "adaptable", "adaptive", "adept", "admirable", "admired", "adorable", "adroit", "adventurous", "aesthetic", "affable", "affectionate", "affirmative", "agile", "agreeable", "airy", "alchemical", "alert", "alluring", "altruistic", "amazing", "ambitious", "ambrosial", "amiable", "amicable", "amped", "ample", "amused", "amusing", "analytical", "angelic", "animated", "anticipative", "appealing", "appetizing", "appreciable", "appreciative", "approachable", "artful", "artistic", "assertive", "astonishing", "astounding", "astute", "athletic", "attentive", "attractive", "auspicious", "authentic", "awesome", "balanced", "balmy", "beaming", "beauteous", "beautiful", "beguiling", "beloved", "benevolent", "benign", "blazing", "blessed", "blissed-out", "blissful", "bodacious", "boisterous", "bold", "boundless", "bountiful", "brave", "breathtaking", "bright", "brilliant", "bubbly", "buoyant", "calm", "candid", "capable", "captivating", "carefree", "caring", "catalytic", "charismatic", "charming", "cheerful", "cherished", "chirpy", "chivalrous", "clairvoyant", "clarion", "classy", "clever", "cogent", "cognizant", "colorful", "comely", "comfortable", "committed", "communal", "compassionate", "composed", "conducive", "confident", "connected", "conscientious", "conscious", "considerate", "consistent", "contagious", "content", "convincing", "convivial", "cordial", "courageous", "cozy", "creative", "crisp", "crucial", "cuddly", "cultivated", "cultured", "curious", "cute", "dandy", "dapper", "daring", "dauntless", "dazzling", "dear", "decisive", "dedicated", "definitive", "delicate", "delicious", "delightful", "dependable", "desirable", "determined", "devoted", "diligent", "divine", "driven", "durable", "dynamic", "eager", "earnest", "easygoing", "ebullient", "eclectic", "effervescent", "efficient", "effortless", "elegant", "eloquent", "eminent", "empathetic", "enchanted", "enchanting", "encouraging", "endearing", "enduring", "energetic", "engaging", "enigmatic", "enjoyable", "enlivened", "entertaining", "enthusiastic", "ephemeral", "epic", "equanimous", "essential", "eternal", "ethical", "eudaimonic", "euphoric", "evolving", "exalted", "excellent", "exciting", "exemplary", "exhilarating", "expressive", "exquisite", "extraordinary", "exuberant", "exultant", "fabulous", "fair", "faithful", "fantastic", "fascinating", "fearless", "fecund", "fervent", "fetching", "fierce", "flourishing", "fluid", "focused", "fortuitous", "foxy", "fragrant", "free-spirited", "fresh", "friendly", "fulfilled", "fulfilling", "fun-loving", "funny", "gallant", "generative", "generous", "genius", "gentle", "genuine", "glamorous", "gleaming", "gleeful", "glorious", "glowing", "gorgeous", "graceful", "gracious", "grand", "grateful", "gratifying", "great-hearted", "gregarious", "grinning", "groovy", "groundbreaking", "guiding", "halcyon", "happy", "harmonious", "healing", "healthy", "heartfelt", "heartwarming", "heavenly", "helpful", "heroic", "high-spirited", "hilarious", "honest", "hopeful", "humble", "humorous", "idealistic", "illustrious", "imaginative", "impartial", "impeccable", "impressive", "incandescent", "inclusive", "incomparable", "incredible", "ineffable", "ingenious", "innovative", "inquisitive", "insightful", "inspired", "inspiring", "inspirited", "intelligent", "intrepid", "intuitive", "inventive", "invigorated", "invigorating", "invincible", "jaunty", "jovial", "joyful", "joyous", "jubilant", "judicious", "juxtaposed", "keen", "kind", "kind-hearted", "knowing", "knowledgeable", "laudable", "lavish", "lively", "lovable", "lovely", "loving", "loyal", "luminous", "magical", "magnificent", "majestic", "marvelous", "masterful", "mellifluous", "mellow", "melodic", "mesmerizing", "mindful", "miraculous", "mirthful", "motivated", "motivating", "natural", "noble", "nurturing", "open-hearted", "optimistic", "outstanding", "panoramic", "passionate", "patient", "peaceful", "perceptive", "perseverant", "persevering", "playful", "pleasant", "pleasing", "pleasurable", "plentiful", "plucky", "polished", "polite", "positive", "powerful", "practical", "precious", "prestigious", "prismatic", "proactive", "profound", "prosperous", "radiant", "refreshing", "relaxed", "reliable", "remarkable", "resilient", "resourceful", "respectful", "resplendent", "reverent", "revitalizing", "rhapsodic", "riveting", "robust", "romantic", "sagacious", "satisfying", "savvy", "scintillating", "scrumptious", "sensational", "sensible", "serendipitous", "serene", "shining", "sincere", "skilled", "skillful", "smart", "smooth", "sociable", "soft", "soothing", "soulful", "sparkling", "spectacular", "spellbinding", "spirited", "splendid", "spontaneous", "steadfast", "stellar", "stimulating", "strategic", "striking", "strong", "stunning", "stylish", "sublime", "sunny", "superb", "supportive", "surprising", "sustained", "sustaining", "sweeping", "sweet", "sympathetic", "synchronized", "talented", "tasty", "tenacious", "terrific", "thoughtful", "thrilling", "thriving", "titillating", "tranquil", "transcendent", "triumphant", "trusting", "trustworthy", "ubiquitous", "unabashed", "unconditional", "unstoppable", "upbeat", "uplifting", "valiant", "vibrant", "victorious", "vigorous", "virtuous", "vital", "vivacious", "warm", "warmhearted", "welcoming", "whimsical", "wholehearted", "wise", "witty", "wonderful", "wondrous", "worthy", "zealous", "zestful" };
            string[] animalArr = { "alpaca", "anteater", "antelope", "armadillo", "axolotl", "baboon", "barracuda", "bat", "bear", "bison", "bonobo", "butterfly", "caiman", "camel", "capybara", "cat", "chameleon", "cheetah", "cobra", "cow", "crocodile", "deer", "dhole", "dik-dik", "dingo", "dog", "dolphin", "duck", "dugong", "eagle", "echidna", "eel", "elephant", "elephant seal", "falcon", "fennec fox", "flamingo", "fossa", "frog", "gazelle", "gecko", "gharial", "gibbon", "giraffe", "gnu", "gorilla", "hamster", "hedgehog", "heron", "hippo", "hornbill", "horse", "ibex", "iguana", "impala", "jackal", "jaguar", "jaguarundi", "jellyfish", "kangaroo", "koala", "komodo dragon", "kookaburra", "lemming", "lemur", "leopard", "lion", "lizard", "llama", "lobster", "lorikeet", "loris", "mandrill", "mantis", "meerkat", "mole", "mongoose", "monkey", "narwhal", "numbat", "ocelot", "octopus", "okapi", "orangutan", "ostrich", "owl", "panda", "pangolin", "parrot", "peacock", "penguin", "pigeon", "platypus", "polar bear", "porcupine", "puffin", "puma", "quail", "quokka", "rabbit", "raccoon", "rat", "rattlesnake", "red panda", "rhino", "rhinoceros", "salamander", "salmon", "scorpion", "seahorse", "seal", "serval", "shark", "sloth", "sloth bear", "snake", "sparrow", "squid", "squirrel", "swan", "tamarin", "tapir", "tarsier", "tasmanian devil", "tiger", "toucan", "turtle", "uakari", "uguisu", "umbrellabird", "vaquita", "vervet monkey", "vicuña", "vulture", "wallaby", "wallaroo", "walrus", "warthog", "wildebeest", "wombat", "x-ray tetra", "xenops", "yak", "yellow-bellied marmot", "yellow-eyed penguin", "yellowjacket", "zebra", "zebra dove", "zebra shark", "zebradonkey", "zonkey", "zorilla" };

            // use PublicId as seed for random number generator
            Random random = new Random(userSchema.PublicId.GetHashCode());

            return (Capitalise(adjectiveArr[random.Next(0, adjectiveArr.Length)]) + " " + Capitalise(animalArr[random.Next(0, animalArr.Length)]));
        }

        private static string Capitalise(string word)
        {
            return word.Substring(0, 1).ToUpper() + word.Substring(1).ToLower();
        }

        public async Task<User> GetCurrentUser(HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            User userToReturn = userSchemaToUser(user);
            if(!CustomUsernamesEnabled && user.Role != UserRole.Admin)
            {
                userToReturn.Username = null;
            }

            return userToReturn;
        }

        public async Task<bool> IsAdmin(HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            return user.Role == UserRole.Admin;
        }

        public async Task BanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema currentUser = await GetUserFromJWT(httpContext, dbCtx);

            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.PublicId.Equals(userPublicId.Id));

            if (user == null || currentUser == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(currentUser, dbCtx);

            if (user == currentUser)    //prevent admin banning themselves
            {
                throw new InvalidAuthorizationException();
            }

            await SetUserBanState(user, true, dbCtx);
        }
        public async Task UnBanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema currentUser = await GetUserFromJWT(httpContext, dbCtx);

            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.PublicId.Equals(userPublicId.Id));

            if (user == null || currentUser == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(currentUser, dbCtx);

            await SetUserBanState(user, false, dbCtx);
        }

        public static async Task SetUserBanState(UserSchema user, bool isBannned, DataContext dbCtx){
            // if user is anon, ban their IP
            if (user.IsAnon)
            {
                if(user.CreationIP == null)
                    await dbCtx.Entry(user).Reference(u => u.CreationIP).LoadAsync();

                user.CreationIP.IsBanned = isBannned;
                dbCtx.ClientIPs.Update(user.CreationIP);
            }
            else // if user is not anon, ban the user
            {
                user.IsBanned = isBannned;
                dbCtx.Users.Update(user);
            }

            await dbCtx.SaveChangesAsync();
        }

        public async Task<UserReplyNotifications> GetReplyNotifications(HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);
            if (user == null) 
            {
                throw new UserNotFoundException();
            }
            await UserService.EnsureNotBanned(user, dbCtx);

            if (user.IsAnon) throw new InvalidAuthorizationException();

            return new UserReplyNotifications
            {
                Enabled = user.ReplyNotificationsEnabled
            };
        }
        public async Task SetReplyNotifications(UserReplyNotifications newData, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema user = await GetUserFromJWT(httpContext, dbCtx);
            if (user == null)
            {
                throw new UserNotFoundException();
            }
            await UserService.EnsureNotBanned(user, dbCtx);

            if (user.IsAnon) throw new InvalidAuthorizationException();

            user.ReplyNotificationsEnabled = newData.Enabled;
            dbCtx.Users.Update(user);
            await dbCtx.SaveChangesAsync();
        }

        public async Task<Statistics> GetStats(DataContext dbCtx)
        {
            Statistics stats = new Statistics();
            stats.TotalUsers = (await dbCtx.Users.ToListAsync()).Count;
            stats.ActiveUsers_24h = (await dbCtx.Users.Where(u => u.LastActive > DateTime.UtcNow.AddHours(-24)).ToListAsync()).Count;
            stats.ActiveUsers_7d = (await dbCtx.Users.Where(u => u.LastActive > DateTime.UtcNow.AddDays(-7)).ToListAsync()).Count;
            stats.ActiveUsers_30d = (await dbCtx.Users.Where(u => u.LastActive > DateTime.UtcNow.AddDays(-30)).ToListAsync()).Count;
            stats.ActiveUsers_1y = (await dbCtx.Users.Where(u => u.LastActive > DateTime.UtcNow.AddYears(-1)).ToListAsync()).Count;

            return stats;
        }

        public static async Task EnsureNotBanned(UserSchema user, DataContext dbCtx)
        {
            if (user?.IsAnon == true)
            {
                if(user.CreationIP == null)
                    await dbCtx.Entry(user).Reference(u => u.CreationIP).LoadAsync();

                if(user.CreationIP.IsBanned == true)
                    throw new UserBannedException();
            }
            else if (user?.IsBanned == true)
            {
                throw new UserBannedException();
            }
        }

        public static async Task<bool> GetUserIsBanned(UserSchema user, DataContext dbCtx)
        {
            if (user?.IsAnon == true)
            {
                if(user.CreationIP == null)
                    await dbCtx.Entry(user).Reference(u => u.CreationIP).LoadAsync();

                return user.CreationIP.IsBanned == true;
            }
            else
            {
                return user?.IsBanned == true;
            }
        }

        public static User userSchemaToUser(UserSchema userDb)
        {
            if (userDb == null) return null;

            User user = new User
            {
                Username = userDb.Username,
                UserId = userDb.PublicId,
                Role = userDb.Role,
                Email = userDb.Email,
                IsAnon = userDb.IsAnon
            };

            return user;
        }

        public async Task<bool> GetAnonCommentsAllowed(HttpContext httpContext, DataContext dbCtx)
        {
            if (!UserService.AnonymousCommentingEnabled) return false;

            // check if IP is banned or rate limited
            IPAddress clientIP = UserService.GetClientIP(httpContext);

            ClientIPSchema IPRecord = await dbCtx.ClientIPs.SingleOrDefaultAsync(o => o.IPAddressBytes.Equals(clientIP.GetAddressBytes()));

            if (IPRecord != null)
            {
                if (IPRecord.IsBanned)
                {
                    return false;
                }

                // TODO: implement rate limit check
            }

            return true;            
        }
    }
}
