using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public interface IPlayerService
    {
        Task AddStatsForPlayerAsync(Player player, CancellationToken cancellation = default);
        Task FetchClanMemberStats(IEnumerable<string> clanTags, CancellationToken cancellation = default);
        Task FetchTrackedPlayerStats(IEnumerable<string> trackedPlayerNames, CancellationToken cancellation = default);
    }
}