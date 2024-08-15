using Confab.Data;
using Confab.Models.AdminPanel.ContentModeration;

namespace Confab.Services.Interfaces
{
    public interface IAutoModerationService
    {
        Task<List<ModerationRule>> GetModerationRules(DataContext dbCtx);
        Task SetModerationRules(DataContext dbCtx, List<ModerationRule> newRules);
    }
}
