using DataAccess.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class PlayerRepository : Repository<Player>, IPlayerRepository
    {
        public PlayerRepository(BlockTanksStatsDatabaseSettings settings, DateTime now)
            : base(settings, settings.PlayersCollectionName, now)
        {
        }

        public async Task<IEnumerable<Player>> GetAllByNamesAsync(IEnumerable<string> playerNames, CancellationToken cancellation = default) =>
            await (await _models.FindAsync(player => playerNames.Contains(player.DisplayName), cancellationToken: cancellation)).ToListAsync(cancellation);

        public async Task<Player> GetByPlayerIdAsync(string playerId, CancellationToken cancellation = default) =>
            await (await _models.FindAsync(player => player.PlayerId == playerId, cancellationToken: cancellation)).FirstOrDefaultAsync(cancellation);

        public override async Task<Player> CreateAsync(Player player, CancellationToken cancellation = default)
        {
            foreach (var leaderboardCompHistory in player.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = _now;
            }

            return await base.CreateAsync(player, cancellation);
        }

        public async Task UpdateByPlayerIdAsync(string playerId, Player playerIn, CancellationToken cancellation = default)
        {
            playerIn.Timestamp = playerIn.Timestamp == default ? _now : playerIn.Timestamp;

            foreach (var leaderboardCompHistory in playerIn.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = leaderboardCompHistory.Timestamp == default ? _now : leaderboardCompHistory.Timestamp;
            }

            await _models.ReplaceOneAsync(player => player.PlayerId == playerId, playerIn, cancellationToken: cancellation);
        }

        public async Task<IEnumerable<string>> FilterPlayersNotInClanAsync(IEnumerable<string> playerNames, CancellationToken cancellation = default)
        {
            return (await GetAllByNamesAsync(playerNames, cancellation))
                .Where(p => p.LeaderboardCompHistory.All(l => l.Timestamp < _now))
                .Select(p => p.DisplayName).ToList();
        }
    }
}
