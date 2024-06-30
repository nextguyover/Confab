using Confab.Data.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace Confab.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) {
            
        }

        public DbSet<UserSchema> Users => Set<UserSchema>();
        public DbSet<CommentSchema> Comments => Set<CommentSchema>();
        public DbSet<CommentEditSchema> CommentEdits => Set<CommentEditSchema>();
        public DbSet<GlobalSettingsSchema> GlobalSettings => Set<GlobalSettingsSchema>();
        public DbSet<CommentLocationSchema> CommentLocations => Set<CommentLocationSchema>();
        public DbSet<AutoModerationRuleSchema> AutoModerationRules => Set<AutoModerationRuleSchema>();
        //public DbSet<VoteSchema> Votes => Set<VoteSchema>();

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<CommentSchema>()
        //        .HasOne(c => c.Author)
        //        .WithMany(u => u.AuthoredComments)
        //        .HasForeignKey(c => c.AuthorId);

        //    // Configure other relationships as needed

        //    base.OnModelCreating(modelBuilder);
        //}

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder
        //        .UseSqlServer("your_connection_string")
        //        .UseLazyLoadingProxies() // Enable lazy loading proxies
        //        .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.DetachedLazyLoadingWarning));

        //    optionsBuilder.UseJsonOptions(jsonOptions =>
        //    {
        //        jsonOptions.SerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        //    });
        //}
    }
}
