using DataAccess.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public interface IPlayerService
    {
        Task AddStatsForPlayerAsync(Player player, CancellationToken cancellation = default);
    }
}