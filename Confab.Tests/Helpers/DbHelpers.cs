using Confab.Data;
using Confab.Data.DatabaseModels;

namespace Confab.Tests.Helpers
{
    public class DbHelpers
    {
        public static void EnableCommentingAtLocation(CustomWebApplicationFactory<Program> factory, string locationStr)
        {
            using (var scope = factory.Services.CreateScope())
            {
                DataContext dbCtx = scope.ServiceProvider.GetRequiredService<DataContext>();

                CommentLocationSchema location = dbCtx.CommentLocations.Where(l => l.LocationStr == locationStr).SingleOrDefault();
                if (location == null)
                {
                    location = new CommentLocationSchema { LocationStr = locationStr, LocalStatus = CommentLocationSchema.CommentingStatus.Enabled };
                    dbCtx.CommentLocations.Add(location);
                }
                else
                {
                    location.LocalStatus = CommentLocationSchema.CommentingStatus.Enabled;
                    dbCtx.CommentLocations.Update(location);
                }

                dbCtx.SaveChanges();
            }
        }
    }
}