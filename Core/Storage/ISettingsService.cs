using System.Threading.Tasks;

namespace DynamicIsland.Core.Storage;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}
