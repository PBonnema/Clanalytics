using DataAccess.Models;
using DataAccess.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockTanksStats
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine($"Initalizing at {DateTimeOffset.Now}...");
            var sw = Stopwatch.StartNew();

            var culture = CultureInfo.CreateSpecificCulture("nl-NL");
            var clanDashboardsPath = "Dashboards";

            string connectionString = "";
            string statsPath = "";
            switch (Environment.GetEnvironmentVariable("ENVIRONMENT"))
            {
                case "Production":
                    connectionString = "mongodb://root:example@mongo:27017";
                    statsPath = "/app/SavedStats";
                    break;
                case "Test":
                    connectionString = "mongodb://root:example@mongo-test:27017";
                    statsPath = "/app/SavedStats";
                    break;
                case "Development":
                    connectionString = "mongodb://root:example@localhost:27018";
                    statsPath = "../../../../SavedStats";
                    break;
            }

            var blockTanksStatsDatabaseSettings = new BlockTanksStatsDatabaseSettings(
                ConnectionString: connectionString,
                DatabaseName: "BlockTanksStats",
                PlayersCollectionName: "Players",
                ClansCollectionName: "Clans"
            );
            var now = DateTime.UtcNow;
            var _playerRepository = new PlayerRepository(blockTanksStatsDatabaseSettings, now);

            if (Directory.Exists(statsPath))
            {
                Console.WriteLine("Emptying the stats directory...");
                foreach (var directory in Directory.EnumerateDirectories(statsPath))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }

            var players = await _playerRepository.GetAsync();

            var csvSep = ';';
            await SavePlayerStatsAsync(players, culture, statsPath, csvSep);
            await SaveXpPerDaysDashboardAsync("RIOT", players, 1, 14, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveXpDashboardAsync("RIOT", players, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveCumulativeXpDashboardAsync("RIOT", players, culture, statsPath, clanDashboardsPath, csvSep);

            await SaveXpPerDaysDashboardAsync("SWIFT", players, 1, 14, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveXpDashboardAsync("SWIFT", players, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveCumulativeXpDashboardAsync("SWIFT", players, culture, statsPath, clanDashboardsPath, csvSep);

            await SaveXpPerDaysDashboardAsync("Tracked Player", players, 1, 14, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveXpDashboardAsync("Tracked Player", players, culture, statsPath, clanDashboardsPath, csvSep);
            await SaveCumulativeXpDashboardAsync("Tracked Player", players, culture, statsPath, clanDashboardsPath, csvSep);

            sw.Stop();
            Console.WriteLine($"...Done fetching in {sw.Elapsed}. Exiting.");
        }

        static async Task SavePlayerStatsAsync(IEnumerable<Player> players, CultureInfo culture, string statsPath, char csvSep)
        {
            Console.WriteLine("Saving player stats...");
            foreach (var player in players)
            {
                var builder = new StringBuilder();
                builder.AppendFormat(culture, "{1}{0}{2}{0}{3}{0}{4}",
                    csvSep, nameof(PlayerLeaderboardComp.Timestamp), nameof(PlayerLeaderboardComp.Xp), nameof(PlayerLeaderboardComp.Kills), nameof(PlayerLeaderboardComp.Deaths));
                builder.AppendLine();

                foreach (var leaderboardComp in player.LeaderboardCompHistory)
                {
                    builder.AppendFormat(culture, "{1}{0}{2}{0}{3}{0}{4}",
                        csvSep, leaderboardComp.Timestamp.ToLocalTime(), leaderboardComp.Xp, leaderboardComp.Kills, leaderboardComp.Deaths);
                    builder.AppendLine();
                }

                var clanPath = $"{statsPath}/{player.ClanTag}";
                Directory.CreateDirectory(clanPath);
                await File.WriteAllTextAsync($"{clanPath}/{player.DisplayName}.csv", builder.ToString());
                Console.WriteLine($"Saved stats for player {player.DisplayName}");
            }
        }

        static async Task SaveCumulativeXpDashboardAsync(string clanTag, IEnumerable<Player> players, CultureInfo culture, string statsPath, string clanDashboardsPath, char csvSep)
        {
            Console.WriteLine($"Saving cumulative xp dashboard for {clanTag}...");

            players = players.Where(p => p.ClanTag == clanTag);

            var builder = new StringBuilder();
            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach(var player in players)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, player.DisplayName);
            }
            builder.AppendLine();

            var timestamps = players.SelectMany(p => p.LeaderboardCompHistory.Select(stats => stats.Timestamp));
            timestamps = timestamps.OrderBy(t => t).Distinct().ToList();

            var statsEnumerators = new Dictionary<IEnumerator<PlayerLeaderboardComp>, (bool, double)>(players.Select(
                p => {
                    var enumerator = p.LeaderboardCompHistory.GetEnumerator();
                    var done = !enumerator.MoveNext();
                    return KeyValuePair.Create(enumerator, (done, 0.0));
                }));
            
            try
            {
                foreach (var timestamp in timestamps)
                {
                    builder.AppendFormat(culture, "{0}", timestamp.ToLocalTime());
                    foreach (var (statsEnumerator, (done, lastValue)) in statsEnumerators)
                    {
                        if (!done && statsEnumerator.Current.Timestamp <= timestamp)
                        {
                            var nextValue = statsEnumerator.Current.Xp;
                            statsEnumerators[statsEnumerator] = (!statsEnumerator.MoveNext(), nextValue);
                            builder.AppendFormat(culture, "{0}{1}", csvSep, nextValue);
                        } else
                        {
                            builder.AppendFormat(culture, "{0}{1}", csvSep, lastValue);
                        }
                    }
                    builder.AppendLine();
                }
            } finally
            {
                foreach (var enumerator in statsEnumerators)
                {
                    enumerator.Key.Dispose();
                }
            }

            var path = $"{statsPath}/{clanTag}/{clanDashboardsPath}";
            Directory.CreateDirectory(path);
            await File.WriteAllTextAsync($"{path}/CumulativeXp.csv", builder.ToString());
            Console.WriteLine($"Saved cumulative xp dashboard for clan {clanTag}");
        }

        static async Task SaveXpDashboardAsync(string clanTag, IEnumerable<Player> players, CultureInfo culture, string statsPath, string clanDashboardsPath, char csvSep)
        {
            Console.WriteLine($"Saving xp dashboard for {clanTag}...");

            players = players.Where(p => p.ClanTag == clanTag);

            var builder = new StringBuilder();
            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach (var player in players)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, player.DisplayName);
            }
            builder.AppendLine();

            var timestamps = players.SelectMany(p => p.LeaderboardCompHistory.Select(stats => stats.Timestamp));
            timestamps = timestamps.OrderBy(t => t).Distinct().ToList();

            var statsEnumerators = new Dictionary<IEnumerator<PlayerLeaderboardComp>, (bool, double)>(players.Select(
                p =>
                {
                    var enumerator = p.LeaderboardCompHistory.GetEnumerator();
                    var done = !enumerator.MoveNext();
                    return KeyValuePair.Create(enumerator, (done, enumerator.Current.Xp));
                }));

            try
            {
                foreach (var timestamp in timestamps)
                {
                    builder.AppendFormat(culture, "{0}", timestamp.ToLocalTime());
                    foreach (var (statsEnumerator, (done, lastValue)) in statsEnumerators)
                    {
                        if (!done && statsEnumerator.Current.Timestamp <= timestamp)
                        {
                            var nextValue = statsEnumerator.Current.Xp;
                            statsEnumerators[statsEnumerator] = (!statsEnumerator.MoveNext(), nextValue);
                            builder.AppendFormat(culture, "{0}{1}", csvSep, nextValue - lastValue);
                        }
                        else
                        {
                            builder.AppendFormat(culture, "{0}{1}", csvSep, 0.0);
                        }
                    }
                    builder.AppendLine();
                }
            }
            finally
            {
                foreach (var enumerator in statsEnumerators)
                {
                    enumerator.Key.Dispose();
                }
            }

            var path = $"{statsPath}/{clanTag}/{clanDashboardsPath}";
            Directory.CreateDirectory(path);
            await File.WriteAllTextAsync($"{path}/Xp.csv", builder.ToString());
            Console.WriteLine($"Saved xp dashboard for clan {clanTag}");
        }

        static async Task SaveXpPerDaysDashboardAsync(
            string clanTag,
            IEnumerable<Player> players,
            int days,
            int periodDays,
            CultureInfo culture,
            string statsPath,
            string clanDashboardsPath,
            char csvSep)
        {
            Console.WriteLine($"Saving xp dashboard for {clanTag}...");
            --periodDays;

            players = players.Where(p => p.ClanTag == clanTag);

            var firstDate = DateTime.Today.AddDays(-periodDays);
            players = players.OrderByDescending(p =>
            {
                var first = p.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp.Date >= firstDate);
                if (first != null)
                {
                    var last = p.LeaderboardCompHistory.LastOrDefault();
                    return last.Xp - first.Xp;
                } else
                {
                    return 0.0;
                }
            }).ToList();

            var builder = new StringBuilder();
            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach (var player in players)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, player.DisplayName);
            }
            builder.AppendLine();

            var statsEnumerators = new Dictionary<IEnumerator<PlayerLeaderboardComp>, (bool, double)>(players.Select(
                p =>
                {
                    var enumerator = p.LeaderboardCompHistory.GetEnumerator();
                    var done = false;

                    do
                    {
                        done = !enumerator.MoveNext();
                    } while (!done && enumerator.Current.Timestamp.Date < firstDate);
                    return KeyValuePair.Create(enumerator, (done, enumerator.Current?.Xp ?? 0.0));
                }));

            try
            {
                for (var currentDate = firstDate; currentDate <= DateTime.Today; currentDate = currentDate.AddDays(days))
                {
                    builder.AppendFormat(culture, "{0}", currentDate);
                    foreach (var (statsEnumerator, (done, lastValue)) in statsEnumerators)
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
                            statsEnumerators[statsEnumerator] = (!notDone, nextValue);
                            builder.AppendFormat(culture, "{0}{1}", csvSep, nextValue - lastValue);
                        }
                        else
                        {
                            builder.AppendFormat(culture, "{0}{1}", csvSep, 0.0);
                        }
                    }
                    builder.AppendLine();
                }
            }
            finally
            {
                foreach (var enumerator in statsEnumerators)
                {
                    enumerator.Key.Dispose();
                }
            }

            var path = $"{statsPath}/{clanTag}/{clanDashboardsPath}";
            Directory.CreateDirectory(path);
            await File.WriteAllTextAsync($"{path}/XpPerDay.csv", builder.ToString());
            Console.WriteLine($"Saved xp per day dashboard for clan {clanTag}");
        }
    }
}