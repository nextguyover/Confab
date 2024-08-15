using Confab.Data;
using Confab.Models;
using Confab.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Confab.Exceptions;
using Jdenticon;
using Microsoft.AspNetCore.Http.Json;
using Confab.Middleware;
using Confab.Data.DatabaseModels;
using Confab.Models.AdminPanel.CommentSettings;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.UserAuth;
using System.Data;
using Confab.Models.AdminPanel.ContentModeration;
using Confab.Services.Interfaces;
using Confab.Emails;
using Confab.Emails.TemplateSubstitution;
using Confab.Models.AdminPanel.Emails;
using Confab.Models.Moderation;
using System.Security.Cryptography;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestHeaders |
                            HttpLoggingFields.RequestBody |
                            HttpLoggingFields.ResponseHeaders |
                            HttpLoggingFields.ResponseBody;
});

builder.Host.UseSystemd();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>           //enable use of Bearer tokens in Swagger
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors();

string JwtKey = "";
if (File.Exists("jwt-key"))
{
    JwtKey = File.ReadAllText("jwt-key");
}
else
{
    string jwtChars = "ABCDEFGHIJKLMONOPQRSTUVWXYZabcdefghijklmonopqrstuvwxyz0123456789";
    int strlen = 64;
    char[] randomChars = new char[strlen];

    for (int i = 0; i < strlen; i++)
    {
        randomChars[i] = jwtChars[RandomNumberGenerator.GetInt32(0, jwtChars.Length)];
    }

    JwtKey = new string(randomChars);
    File.WriteAllText("jwt-key", JwtKey);
}

UserService.JwtKey = JwtKey;

UserService.JwtValidationParams = new TokenValidationParameters()
{
    ValidateActor = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["ConfabParams:ExternalUrl"],
    ValidAudience = builder.Configuration["ConfabParams:ExternalUrl"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
    TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
};

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
);

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ICommentService, CommentService>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<ICommentLocationService, CommentLocationService>();
builder.Services.AddSingleton<IAutoModerationService, AutoModerationService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.Configure<JsonOptions>(options =>
         options.SerializerOptions.DefaultIgnoreCondition
   = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull);

builder.Services.Configure<JsonOptions>(options =>
         options.SerializerOptions.Converters.Add(new DateTimeConverter()));

/*
    Regex for getting path with forward slash prepended: (?<=(?:(?:[^@:\/\s]+):\/?)?\/?(?:(?:[^@:\/\s]+)(?::(?:[^@:\/\s]+))?@)?(?:[^@:\/\s]+)(?::(?:\d+))?(?:(?:\/\w+)*))(?:\/[\w\-\.]*[^#?\s]*)(?=(?:.*)?(?:#[\w\-]+)?$)
    Regex for getting path without forward slash prepended: (?<=(?:(?:[^@:\/\s]+):\/?)?\/?(?:(?:[^@:\/\s]+)(?::(?:[^@:\/\s]+))?@)?(?:[^@:\/\s]+)(?::(?:\d+))?(?:(?:\/\w+)*\/))(?:[\w\-\.]+[^#?\s]*)?(?=(?:.*)?(?:#[\w\-]+)?$)

    Default regex with backslashes escaped: (?<=(?:(?:[^@:\\/\\s]+):\\/?)?\\/?(?:(?:[^@:\\/\\s]+)(?::(?:[^@:\\/\\s]+))?@)?(?:[^@:\\/\\s]+)(?::(?:\\d+))?(?:(?:\\/\\w+)*))(?:\\/[\\w\\-\\.]*[^#?\\s]*)(?=(?:.*)?(?:#[\\w\\-]+)?$)
 */

CommentLocationService.PageDetectionRegex = builder.Configuration["CommentSettings:PageDetectionRegex"];

CommentService.ManualModerationEnabled = bool.Parse(builder.Configuration["CommentSettings:Moderation:ManualModerationEnabled"]);
CommentService.MaxModQueueCommentCountPerUser = int.Parse(builder.Configuration["CommentSettings:Moderation:MaxModQueueCommentCountPerUser"]);
CommentService.EditMode = Enum.Parse<CommentService.CommentEditMode>(builder.Configuration["CommentSettings:Edits:Mode"]);
CommentService.EditDurationAfterCreationMins = int.Parse(builder.Configuration["CommentSettings:Edits:DurationAfterCreationMins"]);
CommentService.HistoryBadgeMode = Enum.Parse<CommentService.EditHistoryBadgeMode>(builder.Configuration["CommentSettings:Edits:ShowEditBadgeOnComment"]);
CommentService.ShowEditHistory = bool.Parse(builder.Configuration["CommentSettings:Edits:ShowEditHistory"]);
CommentService.RateLimitingEnabled = bool.Parse(builder.Configuration["CommentSettings:RateLimiting:Enabled"]);
CommentService.RateLimitingTimeDurationMins = int.Parse(builder.Configuration["CommentSettings:RateLimiting:TimeDurationMins"]);
CommentService.RateLimitingMaxCommentsPerTimeDuration = int.Parse(builder.Configuration["CommentSettings:RateLimiting:MaxCommentsPerTimeDuration"]);

if (CommentService.ShowEditHistory && CommentService.HistoryBadgeMode == CommentService.EditHistoryBadgeMode.None)
    throw new InvalidConfigException("Invalid configuration in appsettings.json. When ShowEditHistory=true, ShowEditBadgeOnComment must be a value other than None");

UserService.VerificationCodeExpirySeconds = int.Parse(builder.Configuration["UserAuthParams:VerificationCodeExpirySeconds"]);
UserService.MaxVerificationCodeAttempts = int.Parse(builder.Configuration["UserAuthParams:MaxVerificationCodeAttempts"]);
UserService.MaxVerificationCodeEmails = int.Parse(builder.Configuration["UserAuthParams:MaxVerificationCodeEmails"]);
UserService.MaxVerificationCodeEmailResetDurationHours = int.Parse(builder.Configuration["UserAuthParams:MaxVerificationCodeEmailResetDurationHours"]);
UserService.MaxNewSignups = int.Parse(builder.Configuration["UserAuthParams:MaxNewSignups"]);
UserService.MaxNewSignupsDurationMinutes = int.Parse(builder.Configuration["UserAuthParams:MaxNewSignupsDurationMinutes"]);

UserService.AnonymousCommentingEnabled = bool.Parse(builder.Configuration["AnonymousCommenting:Enabled"]);

UserService.CustomUsernamesEnabled = bool.Parse(builder.Configuration["Usernames:CustomUsernamesEnabled"]);
UserService.UsernameChangeCooldownTimeMins = int.Parse(builder.Configuration["Usernames:UsernameChangeCooldownTimeMins"]);

EmailService.SmtpServer = builder.Configuration["Emails:SMTP:Server"];
EmailService.SmtpPort = int.Parse(builder.Configuration["Emails:SMTP:Port"]);
EmailService.UseTLS = bool.Parse(builder.Configuration["Emails:SMTP:UseTLS"]);
EmailService.AuthCodeEmailsMailbox = new SmptMailbox(
    builder.Configuration["Emails:SendingAddresses:AuthCodeEmails:Address"],
    builder.Configuration["Emails:SendingAddresses:AuthCodeEmails:Username"],
    builder.Configuration["Emails:SendingAddresses:AuthCodeEmails:Password"]);
EmailService.UserNotificationEmailsMailbox = new SmptMailbox(
    builder.Configuration["Emails:SendingAddresses:UserNotificationEmails:Address"],
    builder.Configuration["Emails:SendingAddresses:UserNotificationEmails:Username"],
    builder.Configuration["Emails:SendingAddresses:UserNotificationEmails:Password"]);
EmailService.AdminNotificationEmailsMailbox = new SmptMailbox(
    builder.Configuration["Emails:SendingAddresses:AdminNotificationEmails:Address"],
    builder.Configuration["Emails:SendingAddresses:AdminNotificationEmails:Username"],
    builder.Configuration["Emails:SendingAddresses:AdminNotificationEmails:Password"]);

EmailService.ModQueueReminderSchedule = new ModQueueReminderScheduleData(builder.Configuration.GetSection("Emails:AdminModQueueRemindersHrsAfterInactivity").GetChildren().ToArray().Select(c => c.Value).ToList().ConvertAll<int>(i => int.Parse(i)));

AllEmailsTemplatingScaffold.ServiceName = builder.Configuration["Emails:TemplateParameters:ServiceName"];
AllEmailsTemplatingScaffold.SiteUrl = builder.Configuration["Emails:TemplateParameters:SiteUrl"];
AllEmailsTemplatingScaffold.ConfabBackendApiUrl = builder.Configuration["Emails:TemplateParameters:ConfabBackendApiUrl"];

AuthCodeTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/auth-code.html"));
AdminAutoModNotifTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/admin-automod-notif.html"));
AdminCommentNotifTopLvlTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/admin-comment-notif-top-level.html"));
AdminCommentNotifTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/admin-comment-notif.html"));
AdminModQueueReminderTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/admin-mod-queue-reminder.html"));
UserCommentReplyNotifTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/user-comment-reply-notif.html"));
AdminEditNotifTemplatingData.TemplateFile = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "./App_Data/email_templates/admin-edit-notif.html"));

builder.Services.AddHostedService<PeriodicEmailService>();

var app = builder.Build();

app.UseHttpLogging();

app.Logger.LogInformation("Initialising Confab...");

using (var scope = app.Services.CreateScope())
{
    EmailService.logger = scope.ServiceProvider.GetRequiredService<ILogger<EmailService>>();
    UserService.logger = scope.ServiceProvider.GetRequiredService<ILogger<UserService>>();
    ApiLoggingMiddleware.logger = scope.ServiceProvider.GetRequiredService<ILogger<ApiLoggingMiddleware>>();
}

try
{
    app.Urls.Add("http://*:" + builder.Configuration["ConfabParams:Server:Port"]);
}
catch { }

using (var scope =
  app.Services.CreateScope())
using (var dbCtx = scope.ServiceProvider.GetService<DataContext>())
{
    try {   // Create the database directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(dbCtx.Database.GetDbConnection().DataSource));
    } catch {}
    //dbCtx.Database.Migrate();     //TODO: apply migrations and change this back
    dbCtx.Database.EnsureCreated();

    if (await dbCtx.GlobalSettings.SingleOrDefaultAsync() == null)
    {
        dbCtx.GlobalSettings.Add(new GlobalSettingsSchema());     //initialise the single global settings record, if it doesn't already exist
    }

    List<UserSchema> currentAdminsInDb = await dbCtx.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
    List<string> currentAdmins = builder.Configuration.GetSection("UserRoles:Admin").GetChildren().ToArray().Select(c => c.Value).ToList();
    foreach (UserSchema currentAdminInDb in currentAdminsInDb)
    {
        if (!currentAdmins.Contains(currentAdminInDb.Email))        //firstly, remove any admins that have been removed from appsettings.json
        {
            currentAdminInDb.Role = UserRole.Standard;
            dbCtx.Users.Update(currentAdminInDb);

            app.Logger.LogInformation("User " + currentAdminInDb.Email + " has been removed as an Admin");
        }
    }

    foreach (string currentAdmin in currentAdmins)      //secondly, add/create new admins
    {
        UserSchema adminUser = await dbCtx.Users.Where(u => u.Email == currentAdmin).SingleOrDefaultAsync();

        if(adminUser == null)
        {
            var userService = scope.ServiceProvider.GetService<IUserService>();

            try
            {
                adminUser = await userService.CreateNewUser(dbCtx, UserRole.Admin, currentAdmin);
                app.Logger.LogInformation("Created new Admin user with email: " + adminUser.Email);
            } catch
            {
                app.Logger.LogCritical("Unable to create admin user.");
            }
        }
        else
        {
            if (adminUser.Role != UserRole.Admin)
            {
                adminUser.Role = UserRole.Admin;
                dbCtx.Users.Update(adminUser);

                app.Logger.LogInformation("User " + adminUser.Email + " has been set as an Admin");
            }

        }
    }

    await dbCtx.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(x => x
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(origin => true)); // allow any origin during development
}
else
{
    app.UseCors(x => x
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins(builder.Configuration.GetSection("ConfabParams:CommentsAtLocation").GetChildren().ToArray().Select(c => c.Value).ToArray())); //allow only these origins
}

app.UseStaticFiles();

app.UseMiddleware<AddCacheHeadersMiddleware>();

if (app.Logger.IsEnabled(LogLevel.Trace))
{
    app.UseMiddleware<ApiLoggingMiddleware>();
}

app.MapGet("/user/anonymous-commenting-enabled", async () => 
{
    return Results.Ok(new { Enabled = UserService.AnonymousCommentingEnabled });
});

app.MapPost("/user/login", async (UserLogin userLogin, IUserService userService, IEmailService emailService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    if (userLogin == null) return Results.StatusCode(400);

    if (userLogin.LoginCode.IsNullOrEmpty())    //if LoginCode not provided, user is requesting a code via email
    {
        try
        {
            await userService.SendVerificationCode(userLogin, emailService, locationService, dbCtx, app.Environment.IsDevelopment());
        } 
        catch (Exception ex) 
        {
            if (ex is RateLimitException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.EmailCodeRequestRateLimitFailure
                });
            }

            if (ex is VerificationEmailsRateLimitException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.VerificationEmailsRateLimit
                });
            }

            if (ex is MaxNewSignupsLimitException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.MaxNewSignupsLimitFailure
                });
            }

            if (ex is InvalidEmailException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.EmailInvalidFailure
                });
            }

            if (ex is UserBannedException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.EmailUserBannedFailure
                });
            }

            if (ex is UserLoginDisabledException || ex is AccountCreationDisabledException)
            {
                return Results.BadRequest(new LoginResponse
                {
                    Outcome = LoginOutcome.AuthenticationDisabled
                });
            }

            if (ex is EmailSendErrorException && app.Environment.IsDevelopment())
            {
                app.Logger.LogWarning(ex.ToString());
                return Results.Ok(new LoginResponse
                {
                    Outcome = LoginOutcome.Success
                });
            }

            if (ex is UninitialisedLocationException)
            {
                return Results.BadRequest(new LoginResponse());
            }
            if (ex.ToString().IndexOf("UNIQUE constraint failed: Users.PublicId") != -1)
            {
                app.Logger.LogError("UNIQUE constraint failed for Users.PublicId when creating a new user. " +
                    "This could have happened because your Confab DB contains a large number of users.");
                return Results.StatusCode(500);
            }

            app.Logger.LogError(ex.ToString());
            return Results.StatusCode(500);
        }

        return Results.Ok(new LoginResponse
        {
            Outcome = LoginOutcome.Success
        });
    }

    try    //if LoginCode is provided, user is trying to login
    {
        return Results.Ok(await userService.Login(userLogin, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is UserLoginVerificationCodeExpiredException)
        {
            return Results.BadRequest(new LoginResponse
            {
                Outcome = LoginOutcome.VerificationCodeExpiredFailure      //return this for user not found because we don't want bad actor access to whether email is registered
            });
        }

        if (ex is InvalidEmailException)
        {
            return Results.BadRequest(new LoginResponse
            {
                Outcome = LoginOutcome.EmailInvalidFailure
            });
        }

        if (ex is UserBannedException)
        {
            return Results.BadRequest(new LoginResponse
            {
                Outcome = LoginOutcome.EmailUserBannedFailure
            });
        }

        if (ex is UserLoginFailedException)
        {
            return Results.BadRequest(new LoginResponse
            {
                Outcome = LoginOutcome.VerificationCodeInvalidFailure
            });
        }

        if (ex is UserLoginDisabledException || ex is AccountCreationDisabledException)
        {
            return Results.BadRequest(new LoginResponse
            {
                Outcome = LoginOutcome.AuthenticationDisabled
            });
        }

        if (ex is UninitialisedLocationException)
        {
            return Results.BadRequest(new LoginResponse());
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/user/anon-login", async (HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    //try {
        return Results.Ok(await userService.AnonLogin(context, dbCtx));
    //}
    //catch (Exception ex)
    //{
    //    app.Logger.LogError(ex.ToString());
    //    return Results.StatusCode(500);
    //}
});

app.MapGet("/user/change-username", async (HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    try
    {
        if (await userService.GetChangeUsernameAvailable(context, dbCtx))
        {
            return Results.StatusCode(200);
        }
        else
        {
            return Results.StatusCode(403);
        }
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        return Results.StatusCode(500);
    }
});

app.MapPost("/user/change-username", async (HttpContext context, UsernameChange usernameChange, IUserService userService, DataContext dbCtx) => {
    try
    {
        await userService.ChangeUsername(usernameChange, context, dbCtx);
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is RateLimitException)
        {
            return Results.StatusCode(429);
        }
        if (ex is UsernameUnavailableException)
        {
            return Results.StatusCode(409);
        }
        if (ex is UserNotFoundException || ex is InvalidUsernameException || ex is CustomUsernameNotAllowedException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/user/get-info", async (HttpContext context, IUserService userService, DataContext dbCtx) => {
    try
    {
        return Results.Ok(await userService.GetCurrentUser(context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is UserNotFoundException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapGet("/user/get-profile-picture/{userId}", async (string userId, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    if (await userService.UserIdExists(userId, dbCtx))
    {
        Stream identicon = Identicon.FromValue(userId, 100, "SHA1").SaveAsSvg();
        return Results.Stream(identicon, "image/svg+xml");
    }
    else
    {
        return Results.StatusCode(400);
    }
}).WithMetadata(new CacheResponseMetadata());

app.MapGet("/user/reply-notifications", async (HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    try
    {
        return Results.Ok(await userService.GetReplyNotifications(context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is UserNotFoundException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/user/reply-notifications", async (UserReplyNotifications data, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    try
    {
        await userService.SetReplyNotifications(data, context, dbCtx);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is UserNotFoundException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/comment/new", async (HttpContext context, CommentCreate commentData, ICommentService commentService, ICommentLocationService locationService, IEmailService emailService, DataContext dbCtx) =>
{
    try
    {
        return Results.Ok(await commentService.Create(commentData, locationService, emailService, context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is InvalidCommentIdException || ex is InvalidCommentException || ex is UninitialisedLocationException || ex is CommentAwaitingModerationException || ex is CommentingNotEnabledException || ex is InvalidLocationException)
        {
            return Results.StatusCode(400);
        }
        if(ex is UserReachedModQueueMaxCountException || ex is UserCommentRateLimitException)
        {
            return Results.StatusCode(429);
        }
        if (ex is AutoModerationFailedException)
        {
            return Results.BadRequest(new CommentAutoModFeedback { Feedback = ((AutoModerationFailedException)ex).Feedback });
        }
        if(ex.ToString().IndexOf("UNIQUE constraint failed: Comments.PublicId") != -1)
        {
            app.Logger.LogError("UNIQUE constraint failed for Comments.PublicId when creating a new comment. " +
                "This could have happened because your Confab DB contains a large number of comments.");
            return Results.StatusCode(500);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/comment/get-at-location/", async (CommentGetAtLocation commentGetAtLocation, HttpContext context, ICommentService commentService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    try
    {
        return Results.Ok(await commentService.GetAtLocation(commentGetAtLocation, locationService, context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is UninitialisedLocationException || ex is InvalidLocationException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/comment/commenting-enabled-at-location/", async (CommentLocation commentLocation, HttpContext context, ICommentService commentService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    try
    {
        return Results.Ok(await commentService.CommentingEnabledAtLocation(commentLocation, locationService, context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is UninitialisedLocationException || ex is InvalidLocationException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/comment/vote/", async (CommentVote commentVote, HttpContext context, ICommentService commentService, DataContext dbCtx) =>
{
    try
    {
        await commentService.Vote(commentVote, context, dbCtx);
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is InvalidCommentIdException || ex is InvalidCommentVoteException || ex is CommentAwaitingModerationException || ex is VotingNotEnabledException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/comment/edit", async (CommentEdit commentEdit, HttpContext context, ICommentService commentService, IEmailService emailService, DataContext dbCtx) =>
{
    try
    {
        await commentService.Edit(commentEdit, context, emailService, dbCtx);
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is InvalidCommentIdException || ex is InvalidCommentException || ex is CommentNotEditableException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is InvalidAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }
        if (ex is AutoModerationFailedException)
        {
            return Results.BadRequest(new CommentAutoModFeedback { Feedback = ((AutoModerationFailedException)ex).Feedback });
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/comment/delete", async (CommentId commentId, HttpContext context, ICommentService commentService, DataContext dbCtx) =>
{
    try
    {
        await commentService.Delete(commentId, context, dbCtx);
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is InvalidCommentIdException || ex is CommentAwaitingModerationException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is InvalidAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/comment/undelete", async (CommentId commentId, HttpContext context, ICommentService commentService, DataContext dbCtx) =>
{
    try
    {
        await commentService.Undelete(commentId, context, dbCtx);       //only admin, TODO: transfer to /admin
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is InvalidCommentIdException || ex is CommentAwaitingModerationException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is InvalidAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/comment/delete-all", async (UserPublicId userPublicId, HttpContext context, ICommentService commentService, DataContext dbCtx) => //TODO: Put this under /admin/
{
    try
    {
        await commentService.DeleteUserContent(userPublicId, context, dbCtx);       //only admin, TODO: transfer to /admin
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException || ex is InvalidCommentIdException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is InvalidAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

app.MapPost("/comment/history", async (CommentId commentId, HttpContext context, IUserService userService, ICommentService commentService, DataContext dbCtx) =>
{
    try
    {
        return Results.Ok(await commentService.GetCommentHistory(commentId, context, dbCtx));
    }
    catch (Exception ex)
    {
        if (ex is InvalidCommentIdException || ex is EditHistoryDisabledException)
        {
            return Results.StatusCode(400);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/admin/comment/moderation-accept", async (CommentId commentId, ICommentService commentService, IEmailService emailService, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            await commentService.ModerationActionCommentAccept(context, commentId, emailService, dbCtx);
        }
        catch (Exception ex)
        {
            if (ex is InvalidCommentIdException)
            {
                return Results.StatusCode(400);
            }
        }
        
        return Results.Ok();
    });
});

app.MapPost("/admin/comment/permanently-delete", async (CommentId commentId, ICommentService commentService, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            await commentService.PermanentlyDelete(commentId, context, dbCtx);
        }
        catch (Exception ex)
        {
            if (ex is InvalidCommentIdException)
            {
                return Results.StatusCode(400);
            }
        }

        return Results.Ok();
    });
});

app.MapPost("/admin/comment/moderation-permanently-delete-all", async (UserPublicId userId, ICommentService commentService, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            await commentService.ModerationActionPermanentlyDeleteAllAwaitingApproval(userId, dbCtx);
        }
        catch (Exception ex)
        {
            if (ex is InvalidCommentIdException || ex is UserNotFoundException)
            {
                return Results.StatusCode(400);
            }
        }

        return Results.Ok();
    });
});

app.MapGet("/admin/comment/moderation-queue", async (ICommentService commentService, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            return Results.Ok(await commentService.GetModerationQueue(dbCtx));
        }
        catch (Exception ex)
        {
            if (ex is InvalidCommentIdException || ex is UserNotFoundException)
            {
                return Results.StatusCode(400);
            }
        }
        return Results.StatusCode(500);
    });
});

app.MapPost("/admin/ban-user", async (UserPublicId userPublicId, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        await userService.BanUser(userPublicId, context, dbCtx);
        return Results.Ok();
    });
});

app.MapPost("/admin/un-ban-user", async (UserPublicId userPublicId, HttpContext context, IUserService userService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        await userService.UnBanUser(userPublicId, context, dbCtx);
        return Results.Ok();
    });
});

app.MapPost("/admin/settings/get-global", async (HttpContext context, IUserService userService, ISettingsService settingsService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        return Results.Ok(await settingsService.GetGlobalCommentSettings(dbCtx));
    });
});

app.MapPost("/admin/settings/set-global", async (HttpContext context, GlobalCommentSettings newSettings, IUserService userService, ISettingsService settingsService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        await settingsService.SetGlobalCommentSettings(dbCtx, newSettings);
        return Results.Ok();
    });
});


app.MapPost("/admin/settings/get-local", async (HttpContext context, CommentLocation commentLocation, IUserService userService, ISettingsService settingsService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            return Results.Ok(await settingsService.GetLocalCommentSettings(locationService, commentLocation, dbCtx));
        }
        catch(Exception ex)
        {
            if(ex is InvalidLocationException)
            {
                return Results.BadRequest();
            }

            app.Logger.LogError(ex.ToString());
            return Results.StatusCode(500);
        }
    });
});

app.MapPost("/admin/settings/set-local", async (HttpContext context, SetLocalCommentSettings newSettings, IUserService userService, ISettingsService settingsService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            await settingsService.SetLocalCommentSettings(newSettings, locationService, dbCtx);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            if (ex is InvalidLocationException || ex is InvalidLocationException)
            {
                return Results.StatusCode(400);
            }

            app.Logger.LogError(ex.ToString());
            return Results.StatusCode(500);
        }
    });
});


app.MapPost("/admin/settings/get-email", async (HttpContext context, CommentLocation location, IUserService userService, ISettingsService settingsService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            return Results.Ok(await settingsService.GetEmailSettings(location, locationService, dbCtx));
        }
        catch(Exception ex)
        {
            if(ex is UninitialisedLocationException || ex is InvalidLocationException)
            {
                return Results.StatusCode(400);
            }
            app.Logger.LogError(ex.ToString());
            return Results.StatusCode(500);
        }
    });
});


app.MapPost("/admin/settings/set-email", async (HttpContext context, EmailSettings newSettings, IUserService userService, ISettingsService settingsService, ICommentLocationService locationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        try
        {
            await settingsService.SetEmailSettings(newSettings, locationService, dbCtx);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            if (ex is UninitialisedLocationException || ex is InvalidLocationException)
            {
                return Results.StatusCode(400);
            }
            app.Logger.LogError(ex.ToString());
            return Results.StatusCode(500);
        }
    });
});

app.MapGet("/admin/settings/moderation-rules", async (HttpContext context, IUserService userService, IAutoModerationService moderationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        return Results.Ok(await moderationService.GetModerationRules(dbCtx));
    });
});

app.MapPost("/admin/settings/moderation-rules", async (HttpContext context, List<ModerationRule> newRuleList, IUserService userService, IAutoModerationService moderationService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        await moderationService.SetModerationRules(dbCtx, newRuleList);
        return Results.Ok();
    });
});

app.MapPost("/admin/settings/sign-out-all-users", async (HttpContext context, IUserService userService, ISettingsService settingsService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        await settingsService.SignOutAllUsers(dbCtx);
        return Results.Ok();
    });
});

app.MapGet("/admin/statistics", async (HttpContext context, IUserService userService, ICommentService commentService, DataContext dbCtx) =>
{
    return await DoIfAdmin(context, userService, dbCtx, async () =>
    {
        Statistics userStats = await userService.GetStats(dbCtx);
        Statistics commentStats = await commentService.GetStats(dbCtx);

        Statistics allStats = new Statistics
        {
            TotalUsers = userStats.TotalUsers,
            ActiveUsers_24h = userStats.ActiveUsers_24h,
            ActiveUsers_7d = userStats.ActiveUsers_7d,
            ActiveUsers_30d = userStats.ActiveUsers_30d,
            ActiveUsers_1y = userStats.ActiveUsers_1y,

            TotalComments = commentStats.TotalComments,
            NewComments_24h = commentStats.NewComments_24h,
            NewComments_7d = commentStats.NewComments_7d,
            NewComments_30d = commentStats.NewComments_30d,
            NewComments_1y = commentStats.NewComments_1y,
        };
        return Results.Ok(allStats);
    });
});

async Task<IResult> DoIfAdmin(HttpContext context, IUserService userService, DataContext dbCtx, Func<Task<IResult>> performIfAdmin)
{
    try
    {
        if (!(await userService.IsAdmin(context, dbCtx)))
        {
            return Results.StatusCode(401);
        }

        return await performIfAdmin.Invoke();
    }
    catch (Exception ex)
    {
        if (ex is UserNotFoundException)
        {
            return Results.StatusCode(400);
        }
        if (ex is MissingAuthorizationException || ex is UserBannedException)
        {
            return Results.StatusCode(401);
        }

        app.Logger.LogError(ex.ToString());
        return Results.StatusCode(500);
    }
}

//async Task<IResult> DoIfUserAuth(HttpContext context, IUserService userService, DataContext dbCtx, Func<Task<IResult>> actionToPerform)
//{
//    try
//    {
//        if (!(await userService.IsAdmin(context, dbCtx)))
//        {
//            return Results.StatusCode(401);
//        }

//        return await actionToPerform.Invoke();
//    }
//    catch (Exception ex)
//    {
//        if (ex is UserNotFoundException)
//        {
//            return Results.StatusCode(400);
//        }
//        if (ex is MissingAuthorizationException || ex is UserBannedException)
//        {
//            return Results.StatusCode(401);
//        }

//        throw;      //TODO get rid of this
//        Console.WriteLine(ex.ToString());
//        return Results.StatusCode(500);
//    }
//}

//async Task<IResult> DoIfUserAuthOptional(HttpContext context, IUserService userService, DataContext dbCtx, Func<Task<IResult>> actionToPerform)
//{

//}

app.Logger.LogInformation("Confab initialised successfully, starting application.");

app.Run();

public partial class Program { };