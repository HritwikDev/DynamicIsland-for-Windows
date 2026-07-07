using System.Threading.Tasks;

namespace DynamicIsland.Core.Services;

public interface IUpdateService
{
    Task CheckAndApplyUpdatesOnStartupAsync();
}
