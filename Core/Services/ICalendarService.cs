using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(int maxCount = 5);
    Task AddAsync(CalendarEvent calendarEvent);
    Task RemoveAsync(string id);
    Task<CalendarEvent?> GetNextAsync();
}
