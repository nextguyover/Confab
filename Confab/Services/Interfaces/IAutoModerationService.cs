using Confab.Data;
using Confab.Models.AdminPanel.ContentModeration;

namespace Confab.Services.Interfaces
{
    public interface IAutoModerationService
    {
        Task<List<ModerationRule>> GetModerationRules(DataContext context);
        Task SetModerationRules(DataContext context, List<ModerationRule> newRules);
    }
}
