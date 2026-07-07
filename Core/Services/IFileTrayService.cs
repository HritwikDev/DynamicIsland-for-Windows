using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public interface IFileTrayService
{
    Task<IReadOnlyList<FileTrayItem>> GetAllAsync();
    Task AddAsync(string filePath);
    Task RemoveAsync(string id);
    void OpenWithDefaultApp(FileTrayItem item);
}
