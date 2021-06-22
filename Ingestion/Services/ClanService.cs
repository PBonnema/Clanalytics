using DataAccess.Models;
using DataAccess.Repository;
using Serilog;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public class ClanService : IClanService
    {
        private readonly IClanRepository _clanRepository;
        private readonly ILogger _logger;

        public ClanService(IClanRepository clanRepository, ILogger logger)
        {
            _clanRepository = clanRepository;
            _logger = logger;
        }

        public async Task AddStatsForClanAsync(Clan clan, CancellationToken cancellation = default)
        {
            var storedClan = await _clanRepository.GetByClanIdAsync(clan.ClanId, cancellation);
            if (storedClan == null)
            {
                await _clanRepository.CreateAsync(clan, cancellation);
                _logger.Debug($"Added stats for new clan {storedClan.Tag}");
            }
            else
            {
                clan.LeaderboardCompHistory = storedClan.LeaderboardCompHistory.Concat(clan.LeaderboardCompHistory);
                clan.Id = storedClan.Id;
                await _clanRepository.UpdateByClanIdAsync(storedClan.ClanId, clan, cancellation);
                _logger.Debug($"Updated stats for clan {storedClan.Tag}");
            }
        }
    }
}
