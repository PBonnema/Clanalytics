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
            var now = DateTime.UtcNow;
            Console.WriteLine($"Initalizing at {now}...");
            var sw = Stopwatch.StartNew();

            var culture = CultureInfo.CreateSpecificCulture("nl-NL");
            // TODO bug. Onderstaande regel werkt niet.
            culture.NumberFormat.NumberDecimalDigits = 1;
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
                    statsPath = "../../../../SavedStats-test";
                    break;
            }

            var blockTanksStatsDatabaseSettings = new BlockTanksStatsDatabaseSettings(
                ConnectionString: connectionString,
                DatabaseName: "BlockTanksStats",
                PlayersCollectionName: "Players",
                ClansCollectionName: "Clans"
            );
            var _playerRepository = new PlayerRepository(blockTanksStatsDatabaseSettings, now);
            var _clanRepository = new ClanRepository(blockTanksStatsDatabaseSettings, now);
            var csvSep = ';';
            var days = 1;
            var periodLengthDays = int.Parse(Environment.GetEnvironmentVariable("PERIOD_LENGHT_DAYS"));

            if (Directory.Exists(statsPath))
            {
                Console.WriteLine("Emptying the stats directory...");
                foreach (var directory in Directory.EnumerateDirectories(statsPath))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }

            var clans = await _clanRepository.GetAsync();
            await SaveClanLeaderboardAsync(clans, days, periodLengthDays, culture, statsPath, csvSep, now);

            var players = await _playerRepository.GetAsync();

            foreach(var clanTag in new[] {
                "RIOT",
                "RIOT2",
                "RIOT3",
                "ZR",
                "DRONE",
                "MERC",
                "KRYPTO",
                "FOLDIN",
                "SPACE",
                "INQ",
                "CAVERA",
                "SENTRY",
                "PRO",
                "TD",
                "Tracked Player",
            })
            {
                await SaveClanDashboardsAsync(clanTag, players, days, periodLengthDays, culture, statsPath, clanDashboardsPath, csvSep, now);
            }

            await SavePlayerStatsAsync(players, culture, statsPath, csvSep);

            sw.Stop();
            Console.WriteLine($"...Done fetching in {sw.Elapsed}. Exiting.");
        }

        static async Task SaveClanDashboardsAsync(
            string clanTag,
            IEnumerable<Player> players,
            int days,
            int periodLengthDays,
            CultureInfo culture,
            string statsPath,
            string clanDashboardsPath,
            char csvSep,
            DateTime now)
        {
            await SaveKDRPerDaysDashboardAsync(clanTag, players, days, periodLengthDays, culture, statsPath, clanDashboardsPath, csvSep, now);
            await SaveXpPerDaysDashboardAsync(clanTag, players, days, periodLengthDays, culture, statsPath, clanDashboardsPath, csvSep, now);
            await SaveXpDashboardAsync(clanTag, players, culture, statsPath, clanDashboardsPath, csvSep, now);
            await SaveCumulativeXpDashboardAsync(clanTag, players, culture, statsPath, clanDashboardsPath, csvSep, now);
        }

        static async Task SaveClanLeaderboardAsync(
            IEnumerable<Clan> clans,
            int days,
            int periodLengthDays,
            CultureInfo culture,
            string statsPath,
            char csvSep,
            DateTime now)
        {
            Console.WriteLine("Saving clan leaderboard...");
            --periodLengthDays;

            var firstDate = now.Date.AddDays(-periodLengthDays);
            clans = clans.OrderByDescending(p =>
            {
                var first = p.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp.Date >= firstDate);
                if (first != null)
                {
                    var last = p.LeaderboardCompHistory.LastOrDefault();
                    return last.Xp - first.Xp;
                }
                else
                {
                    return 0.0;
                }
            }).ToList();

            var builder = new StringBuilder();
            builder.Append("Current Total XP");
            foreach (var clan in clans)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, clan.LeaderboardCompHistory.LastOrDefault()?.Xp ?? 0.0);
            }
            builder.AppendLine();

            builder.Append("Days until catchup");
            var riotClan = clans.Single(c => c.Tag == "RIOT");
            var firstRiotStats = riotClan.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp >= firstDate);
            var riotAvgXp = 0.0;
            // Skip today as this day isn't over yet.
            var riotClanStats = riotClan.LeaderboardCompHistory.LastOrDefault(l => l.Timestamp < now.Date);
            if (firstRiotStats != null)
            {
                riotAvgXp = (riotClanStats.Xp - firstRiotStats.Xp) / periodLengthDays;
            }

            foreach (var clan in clans)
            {
                var daysUntilCatchup = 0.0;
                var firstClanStats = clan.LeaderboardCompHistory.FirstOrDefault(l => l.Timestamp >= firstDate);
                bool print = false;
                if (firstClanStats != null)
                {
                    // Skip today as this day isn't over yet.
                    var clanStats = clan.LeaderboardCompHistory.LastOrDefault(l => l.Timestamp < now.Date);
                    if (clanStats != null)
                    {
                        var clanAvgXp = (clanStats.Xp - firstClanStats.Xp) / periodLengthDays;
                        daysUntilCatchup = (clanStats.Xp - riotClanStats.Xp) / (riotAvgXp - clanAvgXp);
                        print = riotAvgXp > clanAvgXp == clanStats.Xp > riotClanStats.Xp && clan.Tag != riotClan.Tag;
                    }
                }

                if (print)
                {
                    builder.AppendFormat(culture, "{0}{1}", csvSep, daysUntilCatchup);
                }
                else
                {
                    builder.AppendFormat(culture, "{0}-", csvSep);
                }
            }
            builder.AppendLine();

            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach (var clan in clans)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, clan.Tag);
            }
            builder.AppendLine();

            var statsEnumerators = new Dictionary<IEnumerator<ClanLeaderboardComp>, (bool, double)>(clans.Select(
                c =>
                {
                    var enumerator = c.LeaderboardCompHistory.GetEnumerator();
                    var done = false;

                    do
                    {
                        done = !enumerator.MoveNext();
                    } while (!done && enumerator.Current.Timestamp.Date < firstDate);
                    return KeyValuePair.Create(enumerator, (done, enumerator.Current?.Xp ?? 0.0));
                }));

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
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

            Directory.CreateDirectory(statsPath);
            await File.WriteAllTextAsync($"{statsPath}/ClanLeaderboardXpPerDay.csv", builder.ToString());
            Console.WriteLine($"Saved xp per day dashboard for clan leaderboard");
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

        static async Task SaveCumulativeXpDashboardAsync(string clanTag, IEnumerable<Player> players, CultureInfo culture, string statsPath, string clanDashboardsPath, char csvSep, DateTime now)
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

        static async Task SaveXpDashboardAsync(string clanTag, IEnumerable<Player> players, CultureInfo culture, string statsPath, string clanDashboardsPath, char csvSep, DateTime now)
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
            int periodLengthDays,
            CultureInfo culture,
            string statsPath,
            string clanDashboardsPath,
            char csvSep,
            DateTime now)
        {
            Console.WriteLine($"Saving xp dashboard for {clanTag}...");
            --periodLengthDays;

            players = players.Where(p => p.ClanTag == clanTag);

            var firstDate = now.Date.AddDays(-periodLengthDays);

            IEnumerable<(Player, IEnumerable<double?>)> columns = players.Select(player =>
            {
                IEnumerable<double?> cells = new List<double?>();
                using var statsEnumerator = player.LeaderboardCompHistory.GetEnumerator();
                var done = false;
                do
                {
                    done = !statsEnumerator.MoveNext();
                } while (!done && statsEnumerator.Current.Timestamp.Date < firstDate);

                PlayerLeaderboardComp last = statsEnumerator.Current ?? default;
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    if (!done && statsEnumerator.Current.Timestamp.Date <= currentDate)
                    {
                        var ignoreLast = (currentDate - last.Timestamp.Date).Days > days;
                        var notDone = true;
                        PlayerLeaderboardComp next = default;
                        var prev = ignoreLast ? statsEnumerator.Current : last;

                        do
                        {
                            next = statsEnumerator.Current;
                            notDone = statsEnumerator.MoveNext();
                        } while (notDone && statsEnumerator.Current.Timestamp.Date <= currentDate);
                        (done, last) = (!notDone, next);
                        cells = cells.Append(next.Xp - prev.Xp);
                    }
                    else
                    {
                        cells = cells.Append(null);
                    }
                }
                return (player, cells);
            });

            columns = columns.OrderByDescending(c => c.Item2.Sum()).ToList();

            var builder = new StringBuilder();
            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach (var (player, values) in columns)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, player.DisplayName);
            }
            builder.AppendLine();

            var columnsEnumerators = columns
                .Select(c => { var enumerator = c.Item2.GetEnumerator(); enumerator.MoveNext(); return enumerator; })
                .ToList();

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    builder.AppendFormat(culture, "{0}", currentDate);
                    foreach (var statsEnumerator in columnsEnumerators)
                    {
                        builder.AppendFormat(culture, "{0}{1}", csvSep, statsEnumerator.Current?.ToString(culture) ?? "-");
                        statsEnumerator.MoveNext();
                    }
                    builder.AppendLine();
                }
            }
            finally
            {
                foreach (var enumerator in columnsEnumerators)
                {
                    enumerator.Dispose();
                }
            }

            var path = $"{statsPath}/{clanTag}/{clanDashboardsPath}";
            Directory.CreateDirectory(path);
            await File.WriteAllTextAsync($"{path}/XpPerDay.csv", builder.ToString());
            Console.WriteLine($"Saved xp per day dashboard for clan {clanTag}");
        }

        static async Task SaveKDRPerDaysDashboardAsync(
            string clanTag,
            IEnumerable<Player> players,
            int days,
            int periodLengthDays,
            CultureInfo culture,
            string statsPath,
            string clanDashboardsPath,
            char csvSep,
            DateTime now)
        {
            Console.WriteLine($"Saving KDR dashboard for {clanTag}...");
            --periodLengthDays;

            players = players.Where(p => p.ClanTag == clanTag);

            var firstDate = now.Date.AddDays(-periodLengthDays);

            IEnumerable<(Player, (double?, double?), IEnumerable<(double, double)?>)> columns = players.Select(player =>
            {
                IEnumerable<(double, double)?> cells = new List<(double, double)?>();
                using var statsEnumerator = player.LeaderboardCompHistory.GetEnumerator();
                var done = false;
                do
                {
                    done = !statsEnumerator.MoveNext();
                } while (!done && statsEnumerator.Current.Timestamp.Date < firstDate);

                PlayerLeaderboardComp last = statsEnumerator.Current ?? default;
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    if (!done && statsEnumerator.Current.Timestamp.Date <= currentDate)
                    {
                        var ignoreLast = (currentDate - last.Timestamp.Date).Days > days;
                        var notDone = true;
                        PlayerLeaderboardComp next = default;
                        var prev = ignoreLast ? statsEnumerator.Current : last;

                        do
                        {
                            next = statsEnumerator.Current;
                            notDone = statsEnumerator.MoveNext();
                        } while (notDone && statsEnumerator.Current.Timestamp.Date <= currentDate);
                        (done, last) = (!notDone, next);
                        cells = cells.Append((next.Kills - prev.Kills, next.Deaths - prev.Deaths));
                    }
                    else
                    {
                        cells = cells.Append(null);
                    }
                }

                var kills = cells.Sum(cell => cell?.Item1);
                var deaths = cells.Sum(cell => cell?.Item2);
                return (player, (kills, deaths), cells);
            });

            columns = columns.OrderByDescending(column => column.Item2.Item1 / (column.Item2.Item2 == 0.0 ? 1.0 : column.Item2.Item2 ?? 1.0)).ToList();

            var builder = new StringBuilder();
            builder.Append("Total KD");
            foreach (var (player, (kills, deaths), values) in columns)
            {
                var kdr = kills / (deaths == 0.0 ? 1.0 : deaths ?? 1.0);
                builder.AppendFormat(culture, "{0}{1:N1} ({2}/{3})", csvSep, kdr, kills, deaths);
            }
            builder.AppendLine();

            builder.AppendFormat(culture, "{0}", nameof(PlayerLeaderboardComp.Timestamp));
            foreach (var (player, (kills, deaths), values) in columns)
            {
                builder.AppendFormat(culture, "{0}{1}", csvSep, player.DisplayName);
            }
            builder.AppendLine();

            var columnsEnumerators = columns
                .Select(c => { var enumerator = c.Item3.GetEnumerator(); enumerator.MoveNext(); return enumerator; })
                .ToList();

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    builder.AppendFormat(culture, "{0}", currentDate);
                    foreach (var statsEnumerator in columnsEnumerators)
                    {
                        var kdr = statsEnumerator.Current?.Item1 / (statsEnumerator.Current?.Item2 == 0.0 ? 1.0 : statsEnumerator.Current?.Item2);
                        builder.AppendFormat(culture, "{0}{1}({2}/{3})", csvSep, kdr.HasValue ? string.Format(culture, "{0:N1}", kdr) : "-", statsEnumerator.Current?.Item1, statsEnumerator.Current?.Item2);
                        statsEnumerator.MoveNext();
                    }
                    builder.AppendLine();
                }
            }
            finally
            {
                foreach (var enumerator in columnsEnumerators)
                {
                    enumerator.Dispose();
                }
            }

            var path = $"{statsPath}/{clanTag}/{clanDashboardsPath}";
            Directory.CreateDirectory(path);
            await File.WriteAllTextAsync($"{path}/KDRPerDay.csv", builder.ToString());
            Console.WriteLine($"Saved KDR per day dashboard for clan {clanTag}");
        }
    }
}