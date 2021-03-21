using DataAccess.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly IMongoCollection<Player> _players;
        private readonly DateTime _now;

        public PlayerRepository(BlockTanksStatsDatabaseSettings settings, DateTime now)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _players = database.GetCollection<Player>(settings.PlayersCollectionName);
            _now = now;
        }

        public async Task<IEnumerable<Player>> GetAsync(CancellationToken cancellation = default) =>
            await (await _players.FindAsync(_ => true, cancellationToken: cancellation)).ToListAsync(cancellation);

        public async Task<Player> GetByPlayerIdAsync(string playerId, CancellationToken cancellation = default) =>
            await (await _players.FindAsync(player => player.PlayerId == playerId, cancellationToken: cancellation)).FirstOrDefaultAsync(cancellation);

        public async Task<Player> CreateAsync(Player player, CancellationToken cancellation = default)
        {
            player.Timestamp = _now;

            foreach (var leaderboardCompHistory in player.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = _now;
            }

            await _players.InsertOneAsync(player, cancellationToken: cancellation);
            return player;
        }

        public async Task UpdateByPlayerIdAsync(string playerId, Player playerIn, CancellationToken cancellation = default)
        {
            playerIn.Timestamp = _now;

            foreach (var leaderboardCompHistory in playerIn.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = leaderboardCompHistory.Timestamp == default ? _now : leaderboardCompHistory.Timestamp;
            }

            await _players.ReplaceOneAsync(player => player.PlayerId == playerId, playerIn, cancellationToken: cancellation);
        }

        public async Task RemoveByPlayerIdAsync(Player playerIn, CancellationToken cancellation = default) =>
            await _players.DeleteOneAsync(player => player.PlayerId == playerIn.PlayerId, cancellationToken: cancellation);

        public async Task RemoveByPlayerIdAsync(string playerId, CancellationToken cancellation = default) =>
            await _players.DeleteOneAsync(player => player.PlayerId == playerId, cancellationToken: cancellation);
    }
}
