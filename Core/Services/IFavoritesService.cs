using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public interface IFavoritesService
{
    Task<IReadOnlyList<FavoriteItem>> GetAllAsync();
    Task AddAsync(FavoriteItem item);
    Task RemoveAsync(string id);
    void Launch(FavoriteItem item);
}
