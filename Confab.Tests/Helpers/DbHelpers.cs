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
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                CommentLocationSchema location = context.CommentLocations.Where(l => l.LocationStr == locationStr).SingleOrDefault();
                if (location == null)
                {
                    location = new CommentLocationSchema { LocationStr = locationStr, LocalStatus = CommentLocationSchema.CommentingStatus.Enabled };
                    context.CommentLocations.Add(location);
                }
                else
                {
                    location.LocalStatus = CommentLocationSchema.CommentingStatus.Enabled;
                    context.CommentLocations.Update(location);
                }

                context.SaveChanges();
            }
        }
    }
}