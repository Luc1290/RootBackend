/*using Explorer.Models;
using RootBackend.Data;

namespace Explorer.Storage
{
    public class ExplorerRepository
    {
        private readonly MemoryContext _context;

        public ExplorerRepository(MemoryContext context)
        {
            _context = context;
        }

        public async Task SaveWeatherLogAsync(WeatherResult result)
        {
            var log = new WeatherLog
            {
                City = result.City,
                Temperature = result.Temperature,
                WindSpeed = result.WindSpeed,
                Timestamp = DateTime.UtcNow
            };

            _context.WeatherLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
*/