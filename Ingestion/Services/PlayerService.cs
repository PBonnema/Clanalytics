using DataAccess.Models;
using DataAccess.Repository;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public class PlayerService
    {
        private readonly IPlayerRepository _playerRepository;

        public PlayerService(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public async Task AddStatsForPlayerAsync(Player player, CancellationToken cancellation = default)
        {
            var storedPlayer = await _playerRepository.GetByPlayerIdAsync(player.PlayerId, cancellation);
            if (storedPlayer == null)
            {
                await _playerRepository.CreateAsync(player, cancellation);
            } else
            {
                player.LeaderboardCompHistory = storedPlayer.LeaderboardCompHistory.Concat(player.LeaderboardCompHistory);
                player.Id = storedPlayer.Id;
                await _playerRepository.UpdateByPlayerIdAsync(storedPlayer.PlayerId, player, cancellation);
            }
        }
    }
}
