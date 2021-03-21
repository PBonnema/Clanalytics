using DataAccess.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class ClanRepository : IClanRepository
    {
        private readonly IMongoCollection<Clan> _clans;
        private readonly DateTime _now;

        public ClanRepository(BlockTanksStatsDatabaseSettings settings, DateTime now)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _clans = database.GetCollection<Clan>(settings.ClansCollectionName);
            _now = now;
        }

        public async Task<IEnumerable<Clan>> GetAsync(CancellationToken cancellation = default) =>
            await (await _clans.FindAsync(_ => true, cancellationToken: cancellation)).ToListAsync(cancellation);

        public async Task<Clan> GetByClanIdAsync(string clanId, CancellationToken cancellation = default) =>
            await (await _clans.FindAsync(clan => clan.Id == clanId, cancellationToken: cancellation)).FirstOrDefaultAsync(cancellation);

        public async Task<Clan> CreateAsync(Clan clan, CancellationToken cancellation = default)
        {
            clan.Timestamp = _now;

            foreach (var leaderboardCompHistory in clan.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = _now;
            }

            await _clans.InsertOneAsync(clan, cancellationToken: cancellation);
            return clan;
        }

        public async Task UpdateByClanIdAsync(string clanId, Clan clanIn, CancellationToken cancellation = default)
        {
            clanIn.Timestamp = _now;

            foreach (var leaderboardCompHistory in clanIn.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = leaderboardCompHistory.Timestamp == default ? _now : leaderboardCompHistory.Timestamp;
            }

            await _clans.ReplaceOneAsync(clan => clan.ClanId == clanId, clanIn, cancellationToken: cancellation);
        }

        public async Task RemoveByClanIdAsync(Clan clanIn, CancellationToken cancellation = default) =>
            await _clans.DeleteOneAsync(clan => clan.ClanId == clanIn.ClanId, cancellationToken: cancellation);

        public async Task RemoveByClanIdAsync(string clanId, CancellationToken cancellation = default) =>
            await _clans.DeleteOneAsync(clan => clan.ClanId == clanId, cancellationToken: cancellation);
    }
}
