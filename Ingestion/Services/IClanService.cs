using DataAccess.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public interface IClanService
    {
        Task AddStatsForClanAsync(Clan clan, CancellationToken cancellation = default);
    }
}