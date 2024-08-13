using Confab.Data.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace Confab.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<UserSchema> Users => Set<UserSchema>();
        public DbSet<CommentSchema> Comments => Set<CommentSchema>();
        public DbSet<CommentEditSchema> CommentEdits => Set<CommentEditSchema>();
        public DbSet<GlobalSettingsSchema> GlobalSettings => Set<GlobalSettingsSchema>();
        public DbSet<CommentLocationSchema> CommentLocations => Set<CommentLocationSchema>();
        public DbSet<AutoModerationRuleSchema> AutoModerationRules => Set<AutoModerationRuleSchema>();
    }
}
