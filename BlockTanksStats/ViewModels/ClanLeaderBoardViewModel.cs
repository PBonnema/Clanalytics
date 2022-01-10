using DataAccess.Models;
using DataAccess.Repository;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlockTanksStats.ViewModels
{
    public class ClanLeaderBoardViewModel : ViewModel
    {
        private readonly string _relativeToClanTag;
        private readonly ILogger _logger;

        public IEnumerable<Clan> Clans { get; set; }
        public IEnumerable<DateTime> Dates { get; set; } = Enumerable.Empty<DateTime>();

        public ClanLeaderBoardViewModel(
            IClanRepository clanRepository,
            IPlayerRepository playerRepository,
            string relativeToClanTag,
            string templateFile,
            ILogger logger)
                : base(clanRepository, playerRepository, templateFile)
        {
            _relativeToClanTag = relativeToClanTag;
            _logger = logger;
        }

        private static double? CalcDaysUntilCatchup(
            DataAccess.Models.Clan clan,
            DataAccess.Models.Clan relativeToClan,
            DateTime now,
            int periodLengthDays)
        {
            --periodLengthDays; // We are going to ignore today so the number of days considered is actually 1 less
            var firstDate = now.Date.AddDays(-periodLengthDays);
            var firstRiotStats = relativeToClan.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp >= firstDate);
            var riotAvgXp = 0.0;
            // Skip today as this day isn't over yet.
            var riotClanStats = relativeToClan.LeaderboardCompHistory.LastOrDefault(l => l.Timestamp < now.Date);
            if (firstRiotStats != null)
            {
                riotAvgXp = (riotClanStats.Xp - firstRiotStats.Xp) / periodLengthDays;
            }

            var firstClanStats = clan.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp >= firstDate);
            if (firstClanStats != null)
            {
                // Skip today as this day isn't over yet.
                var clanStats = clan.LeaderboardCompHistory.LastOrDefault(l => l.Timestamp < now.Date);
                if (clanStats != null)
                {
                    var clanAvgXp = (clanStats.Xp - firstClanStats.Xp) / periodLengthDays;
                    if (riotAvgXp > clanAvgXp == clanStats.Xp > riotClanStats.Xp && clan.Tag != relativeToClan.Tag)
                    {
                        return (clanStats.Xp - riotClanStats.Xp) / (riotAvgXp - clanAvgXp);
                    }
                }
            }

            return null;
        }

        private void AddXP(
            IEnumerable<Clan> clanViewModels,
            IEnumerable<DataAccess.Models.Clan> clans,
            DateTime now,
            int days,
            int periodLengthDays)
        {
            --periodLengthDays; // We are going to ignore today so the number of days considered is actually 1 less
            var firstDate = now.Date.AddDays(-periodLengthDays);

            var statsEnumerators = new Dictionary<IEnumerator<ClanLeaderboardComp>, (bool, (double, Clan))>(
                clans.Zip(clanViewModels).Select(
                    x =>
                    {
                        var enumerator = x.First.LeaderboardCompHistory.GetEnumerator();
                        var done = false;

                        do
                        {
                            done = !enumerator.MoveNext();
                        } while (!done && enumerator.Current.Timestamp.Date < firstDate);
                        return KeyValuePair.Create(enumerator, (done, (enumerator.Current?.Xp ?? 0.0, x.Second)));
                    })
                );

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    Dates = Dates.Append(currentDate);
                    foreach (var (statsEnumerator, (done, (lastValue, clanViewModel))) in statsEnumerators)
                    {
                        if (!done && statsEnumerator.Current.Timestamp.Date <= currentDate)
                        {
                            var notDone = true;
                            var nextValue = 0.0;
                            do
                            {
                                nextValue = statsEnumerator.Current.Xp;
                                notDone = statsEnumerator.MoveNext();
                            } while (notDone && statsEnumerator.Current.Timestamp.Date <= currentDate);

                            statsEnumerators[statsEnumerator] = (!notDone, (nextValue, clanViewModel));
                            clanViewModel.XP = clanViewModel.XP.Append(nextValue - lastValue);
                        }
                        else
                        {
                            clanViewModel.XP = clanViewModel.XP.Append(0.0);
                        }
                    }
                }
            }
            finally
            {
                foreach (var enumerator in statsEnumerators)
                {
                    enumerator.Key.Dispose();
                }
            }

            Dates = Dates.ToList();
            foreach (var clanViewModel in clanViewModels)
            {
                clanViewModel.XP = clanViewModel.XP.ToList();
            }
        }

        public override async Task OnGenerateAsync(
            DateTime now,
            int days,
            int periodLengthDays,
            CancellationToken cancellation)
        {
            _logger.Debug("Fetching clans from the database...");
            var clans = await ClanRepository.GetActiveClansAsync(cancellation);

            var relativeToClan = clans.Single(c => c.Tag == _relativeToClanTag);
            Clans = clans
                .Select(c => new Clan {
                    Tag = c.Tag,
                    CurrentTotalXp = c.LeaderboardCompHistory.LastOrDefault()?.Xp ?? 0.0,
                    DaysUntilCatchup = CalcDaysUntilCatchup(
                        clans.Single(c2 => c2.Tag == c.Tag), relativeToClan, now, periodLengthDays),
                })
                .ToList();

            AddXP(Clans, clans, now, days, periodLengthDays);
        }
    }
}
