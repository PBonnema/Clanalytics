using DataAccess.Models;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Repository
{
    public class ClanRepository : Repository<Clan>, IClanRepository
    {
        public ClanRepository(BlockTanksStatsDatabaseSettings settings, DateTime now)
            : base(settings, settings.ClansCollectionName, now)
        {
        }

        public async Task<Clan> GetByClanIdAsync(string clanId, CancellationToken cancellation = default) =>
            await (await _models.FindAsync(clan => clan.ClanId == clanId, cancellationToken: cancellation)).FirstOrDefaultAsync(cancellation);

        public virtual async Task<IEnumerable<Clan>> GetActiveClansAsync(CancellationToken cancellation = default) =>
            (await GetAsync(cancellation))
                .GroupBy(c => c.Tag)
                .Select(g =>
                {
                    var timestamp = g.Max(c2 => c2.Timestamp);
                    return g.First(c => c.Timestamp == timestamp);
                }).ToList();

        public override async Task<Clan> CreateAsync(Clan clan, CancellationToken cancellation = default)
        {
            foreach (var leaderboardCompHistory in clan.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = _now;
            }

            return await base.CreateAsync(clan, cancellation);
        }

        public async Task UpdateByClanIdAsync(string clanId, Clan clanIn, CancellationToken cancellation = default)
        {
            clanIn.Timestamp = clanIn.Timestamp == default ? _now : clanIn.Timestamp;

            foreach (var leaderboardCompHistory in clanIn.LeaderboardCompHistory)
            {
                leaderboardCompHistory.Timestamp = leaderboardCompHistory.Timestamp == default ? _now : leaderboardCompHistory.Timestamp;
            }

            await _models.ReplaceOneAsync(clan => clan.ClanId == clanId, clanIn, cancellationToken: cancellation);
        }
    }
}
