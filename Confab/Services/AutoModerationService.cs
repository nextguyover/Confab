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
        public async Task<List<ModerationRule>> GetModerationRules(DataContext context)
        {
            return (await context.AutoModerationRules.ToListAsync()).ConvertAll((AutoModerationRuleSchema rule) =>
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

        public async Task SetModerationRules(DataContext context, List<ModerationRule> newRules)
        {
            List<AutoModerationRuleSchema> existingRules = await context.AutoModerationRules.ToListAsync();
            context.AutoModerationRules.RemoveRange(existingRules);

            await context.AutoModerationRules.AddRangeAsync(newRules.ConvertAll((ModerationRule rule) =>
            {
                return new AutoModerationRuleSchema
                {
                    FilterRegex = rule.FilterRegex,
                    ReturnError = rule.ReturnError,
                    MatchAction = rule.MatchAction,
                    NotifyAdmins = rule.NotifyAdmins
                };
            }));

            await context.SaveChangesAsync();
        }
    }
}
