﻿using DataAccess.Models;
using DataAccess.Repository;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlockTanksStats.ViewModels
{
    public class ClanDashboardViewModel : ViewModel
    {
        private readonly string _clanTag;
        private readonly ILogger _logger;

        public IEnumerable<Player> Players { get; private set; }
        public IEnumerable<Player> PlayersXP { get => Players; }
        public IEnumerable<Player> PlayersKDR { get => Players; }
        public IEnumerable<Player> PlayersGames { get => Players; }
        public IEnumerable<DateTime> DatesXP { get; private set; } = Enumerable.Empty<DateTime>();
        public IEnumerable<DateTime> DatesKDR { get; private set; } = Enumerable.Empty<DateTime>();
        public IEnumerable<DateTime> DatesGames { get; private set; } = Enumerable.Empty<DateTime>();

        public ClanDashboardViewModel(
            IClanRepository clanRepository,
            IPlayerRepository playerRepository,
            string clanTag,
            string templateFile,
            ILogger logger)
                : base(clanRepository, playerRepository, templateFile)
        {
            _clanTag = clanTag;
            _logger = logger;
        }

        private void AddXP(
            IEnumerable<Player> playerViewModels,
            IEnumerable<DataAccess.Models.Player> players,
            int days,
            int periodLengthDays,
            DateTime now)
        {
            --periodLengthDays; // We are going to ignore today so the number of days considered is actually 1 less
            var firstDate = now.Date.AddDays(-periodLengthDays);

            IEnumerable<IEnumerable<double?>> rows = players.Select(player =>
            {
                var cells = new List<double?>();
                using var statsEnumerator = player.LeaderboardCompHistory.GetEnumerator();

                // Move the enumerator to the first LeaderboardComp on firstDate or after
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
                        var notDone = true;
                        PlayerLeaderboardComp next = default;
                        // If there are more than 1 'days' between the current and last, then ignore the last so that the XP of currentDate will be just 0
                        var ignoreLast = (currentDate - last.Timestamp.Date).Days > days;
                        var prev = ignoreLast ? statsEnumerator.Current : last;

                        do
                        {
                            next = statsEnumerator.Current;
                            notDone = statsEnumerator.MoveNext();
                        } while (notDone && statsEnumerator.Current.Timestamp.Date <= currentDate);
                        (done, last) = (!notDone, next);
                        cells.Add(next.Xp - prev.Xp);
                    }
                    else
                    {
                        cells.Add(null);
                    }
                }
                return cells;
            });

            var rowsEnumerators = rows
                .Select(c => {
                    var enumerator = c.GetEnumerator();
                    enumerator.MoveNext();
                    return enumerator;
                })
                .Zip(playerViewModels)
                .ToList();

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    DatesXP = DatesXP.Append(currentDate);
                    foreach (var (statsEnumerator, player) in rowsEnumerators)
                    {
                        player.XP = player.XP.Append(statsEnumerator.Current);
                        statsEnumerator.MoveNext();
                    }
                }
            }
            finally
            {
                foreach (var (enumerator, _) in rowsEnumerators)
                {
                    enumerator.Dispose();
                }
            }

            DatesXP = DatesXP.ToList();
            foreach (var playerViewModel in playerViewModels)
            {
                playerViewModel.XP = playerViewModel.XP.ToList();
            }
        }

        public void AddKDR(
            IEnumerable<Player> playerViewModels,
            IEnumerable<DataAccess.Models.Player> players,
            int days,
            int periodLengthDays,
            DateTime now)
        {
            --periodLengthDays; // We are going to ignore today so the number of days considered is actually 1 less

            var firstDate = now.Date.AddDays(-periodLengthDays);

            IEnumerable<(Player, (int?, int?), IEnumerable<(int, int)?>)> rows = players
                .Zip(playerViewModels)
                .Select(p =>
            {
                var player = p.First;

                var cells = new List<(int, int)?>();
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
                        cells.Add((next.Kills - prev.Kills, next.Deaths - prev.Deaths));
                    }
                    else
                    {
                        cells.Add(null);
                    }
                }

                var kills = cells.Sum(cell => cell?.Item1);
                var deaths = cells.Sum(cell => cell?.Item2);
                return (p.Second, (kills, deaths), cells as IEnumerable<(int, int)?>);
            });

            foreach (var (playerViewModel, (kills, deaths), values) in rows)
            {
                var kdr = kills / (deaths == 0.0 ? 1.0 : deaths ?? 1.0);
                playerViewModel.AverageKDRNumber = kdr;
                playerViewModel.AverageKDR = $"{kdr:N1} ({kills}/{deaths})";
            }

            var rowsEnumerators = rows
                .Select(r => {
                    var enumerator = r.Item3.GetEnumerator();
                    enumerator.MoveNext();
                    return (enumerator, r.Item1);
                })
                .ToList();

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    DatesKDR = DatesKDR.Append(currentDate);
                    foreach (var (statsEnumerator, playerViewModel) in rowsEnumerators)
                    {
                        var kdr = statsEnumerator.Current?.Item1 / (statsEnumerator.Current?.Item2 == 0.0 ? 1.0 : statsEnumerator.Current?.Item2);
                        var kdrString = kdr.HasValue ? $"{kdr:N1}" : "-";
                        var kills = statsEnumerator.Current?.Item1;
                        var deaths = statsEnumerator.Current?.Item2;
                        playerViewModel.KDR = playerViewModel.KDR.Append($"{kdrString} ({kills}/{deaths})");
                        statsEnumerator.MoveNext();
                    }
                }
            }
            finally
            {
                foreach (var (enumerator, _) in rowsEnumerators)
                {
                    enumerator.Dispose();
                }
            }

            DatesKDR = DatesKDR.ToList();
            foreach (var playerViewModel in playerViewModels)
            {
                playerViewModel.KDR = playerViewModel.KDR.ToList();
            }
        }

        private void AddGames(
            IEnumerable<Player> playerViewModels,
            IEnumerable<DataAccess.Models.Player> players,
            int days,
            int periodLengthDays,
            DateTime now)
        {
            --periodLengthDays; // We are going to ignore today so the number of days considered is actually 1 less
            var firstDate = now.Date.AddDays(-periodLengthDays);

            IEnumerable<IEnumerable<int?>> rows = players.Select(player =>
            {
                var cells = new List<int?>();
                using var gamesEnumerator = player.CompletedGames.GetEnumerator();

                // Move the enumerator to the first CompletedGame on firstDate or after
                var done = false;
                do
                {
                    done = !gamesEnumerator.MoveNext();
                } while (!done && gamesEnumerator.Current.StartTime.Date < firstDate);

                CompletedGame last = gamesEnumerator.Current ?? default;
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    if (!done && gamesEnumerator.Current.StartTime.Date <= currentDate)
                    {
                        var counter = 0;
                        do
                        {
                            if (gamesEnumerator.Current.StartTime.Date < currentDate)
                            {
                                _logger.Error("That's weird");
                            }
                            ++counter;
                        } while (!(done = !gamesEnumerator.MoveNext()) && gamesEnumerator.Current.StartTime.Date <= currentDate);

                        cells.Add(counter);
                    }
                    else
                    {
                        cells.Add(null);
                    }
                }
                return cells;
            });

            var rowsEnumerators = rows
                .Select(c => {
                    var enumerator = c.GetEnumerator();
                    enumerator.MoveNext();
                    return enumerator;
                })
                .Zip(playerViewModels)
                .ToList();

            try
            {
                for (var currentDate = firstDate; currentDate <= now.Date; currentDate = currentDate.AddDays(days))
                {
                    DatesGames = DatesGames.Append(currentDate);
                    foreach (var (statsEnumerator, player) in rowsEnumerators)
                    {
                        player.Games = player.Games.Append(statsEnumerator.Current);
                        statsEnumerator.MoveNext();
                    }
                }
            }
            finally
            {
                foreach (var (enumerator, _) in rowsEnumerators)
                {
                    enumerator.Dispose();
                }
            }

            DatesGames = DatesGames.ToList();
            foreach (var playerViewModel in playerViewModels)
            {
                playerViewModel.Games = playerViewModel.Games.ToList();
            }
        }

        public override async Task OnGenerateAsync(
            DateTime now,
            int days,
            int periodLengthDays,
            CancellationToken cancellation)
        {
            _logger.Debug("Fetching players from the database...");
            var players = await PlayerRepository.GetByClanAsync(_clanTag, cancellation);

            if (!players.Any())
            {
                _logger.Warning($"clan {_clanTag} doesn't have any players. Is the tag correct?");
            }

            Players = players
                .Select(p => new Player
                {
                    Name = p.DisplayName,
                    CurrentTotalXp = p.LeaderboardCompHistory.LastOrDefault()?.Xp ?? 0.0,
                    CurrentTotalGames = p.CompletedGames.Count(),
                })
                .ToList();

            AddXP(Players, players, days, periodLengthDays, now);
            AddKDR(Players, players, days, periodLengthDays, now);
            AddGames(Players, players, days, periodLengthDays, now);

            // TODO: refactor. The XP and KDR lists are now sharing this same Players list. This only works because the XP dashboard does it's own sorting.
            // This will break when adding a new dashboard that also relies on sorting.
            Players = Players
                .OrderByDescending(p => p.AverageKDRNumber)
                .ToList();
        }
    }
}
