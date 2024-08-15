using Confab.Data.DatabaseModels;
using Confab.Data;
using Microsoft.EntityFrameworkCore;
using Confab.Models.AdminPanel.ContentModeration;
using static Confab.Data.DatabaseModels.AutoModerationRuleSchema;
using Confab.Services.Interfaces;

namespace Confab.Services
{
    public class AutoModerationService : IAutoModerationService
    {
        public async Task<List<ModerationRule>> GetModerationRules(DataContext dbCtx)
        {
            return (await dbCtx.AutoModerationRules.ToListAsync()).ConvertAll((AutoModerationRuleSchema rule) =>
            {
                return new ModerationRule
                {
                    FilterRegex = rule.FilterRegex,
                    ReturnError = rule.ReturnError,
                    MatchAction = rule.MatchAction,
                    NotifyAdmins = rule.NotifyAdmins
                };
            });
        }

        public async Task SetModerationRules(DataContext dbCtx, List<ModerationRule> newRules)
        {
            List<AutoModerationRuleSchema> existingRules = await dbCtx.AutoModerationRules.ToListAsync();
            dbCtx.AutoModerationRules.RemoveRange(existingRules);

            await dbCtx.AutoModerationRules.AddRangeAsync(newRules.ConvertAll((ModerationRule rule) =>
            {
                return new AutoModerationRuleSchema
                {
                    FilterRegex = rule.FilterRegex,
                    ReturnError = rule.ReturnError,
                    MatchAction = rule.MatchAction,
                    NotifyAdmins = rule.NotifyAdmins
                };
            }));

            await dbCtx.SaveChangesAsync();
        }
    }
}
