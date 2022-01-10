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

        public async Task<IEnumerable<Player>> GetByClanAsync(string clanTag, CancellationToken cancellation = default) =>
            await (await _models.FindAsync(p => p.ClanTag == clanTag, cancellationToken: cancellation)).ToListAsync(cancellation);
        // TODO Bug: you now also get players with the correct clan tag but are not actually in the clan anymore.
        // For example, because they couldn't be updated for some while or their account was deleted (so their stats don't update anymore).
        // Also, if a tracked clan was recreated and we have players in the old clan in the database, then you get players of both the old and the new clan.
        // this method just doesn't make sense. Make a GetByClanId instead. Player objects don't have a clan id though. They have a clan tag.
        // So this requires a database migration where we do a translation of clan tag to clan id and then throw away the clan tag.
        // Make this translation by simply looking up the current id of the clan with that tag. There might be clans who got recreated already but that should be just a few (1?).

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

        public async Task<IEnumerable<string>> FilterPlayersNotAlreadyFetchedAsync(IEnumerable<string> playerNames, CancellationToken cancellation = default)
        {
            return playerNames
                .Except((await GetAllByNamesAsync(playerNames, cancellation))
                    .Where(p => p.LeaderboardCompHistory.Any(l => l.Timestamp >= _now))
                    .Select(p => p.DisplayName)
                )
                .ToList();

            // Scenarios:
            // - some old ones are in the clan, all inactive, latest one is in the clan (is in list)
            // - some old ones are in the clan, all inactive, latest one is not in the clan (is in list)
            // - some old ones are in the clan, active one too (is not in list, filtered)
            // - some old ones are in the clan, active one is not (is in list)
            // - none of the old ones are in the clan, all inactive, latest one is in the clan (is not in list, filtered)
            // - none of the old ones are in the clan, all inactive, latest one is not in the clan (is in list)
            // - none of the old ones are in the clan, active one is (is not in list, filtered)
            // - none of the old ones are in the clan, active one is not (is in list)
        }
    }
}
