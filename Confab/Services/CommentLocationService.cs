using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Exceptions;
using Confab.Models.AdminPanel.CommentSettings;
using Confab.Models.AdminPanel.PageDetection;
using Confab.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using static Confab.Models.AdminPanel.CommentSettings.LocalCommentSettings;

namespace Confab.Services
{
    public class CommentLocationService : ICommentLocationService
    {
        public static string PageDetectionRegex;

        public async Task<CommentLocationSchema> GetLocation(DataContext dbCtx, string locationString)
        {
            string locationStrConverted = ParseLocation(locationString);
            return await dbCtx.CommentLocations.SingleOrDefaultAsync(l => l.LocationStr == locationStrConverted);
        }

        public async Task<CommentLocationSchema> CreateNewLocation(DataContext dbCtx, string locationString)
        {
            string locationStrConverted = ParseLocation(locationString);

            CommentLocationSchema locationObj = new CommentLocationSchema
            {
                LocationStr = locationStrConverted
            };

            dbCtx.Add(locationObj);
            await dbCtx.SaveChangesAsync();

            return locationObj;
        }

        private static string ParseLocation(string locationString)
        {

            if (locationString.IsNullOrEmpty())
            {
                throw new InvalidLocationException();
            }
            return Regex.Match(locationString, PageDetectionRegex).Value;
        }
    }
}
