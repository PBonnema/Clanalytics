using DataAccess.Models;
using DataAccess.Repository;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public class ClanService : IClanService
    {
        private readonly IClanRepository _clanRepository;

        public ClanService(IClanRepository clanRepository)
        {
            _clanRepository = clanRepository;
        }

        public async Task AddStatsForClanAsync(Clan clan, CancellationToken cancellation = default)
        {
            var storedClan = await _clanRepository.GetByClanIdAsync(clan.ClanId, cancellation);
            if (storedClan == null)
            {
                await _clanRepository.CreateAsync(clan, cancellation);
            }
            else
            {
                clan.LeaderboardCompHistory = storedClan.LeaderboardCompHistory.Concat(clan.LeaderboardCompHistory);
                clan.Id = storedClan.Id;
                await _clanRepository.UpdateByClanIdAsync(storedClan.ClanId, clan, cancellation);
            }
        }
    }
}
