using DataAccess.Repository;
using Ingestion.Agents;
using Ingestion.Services;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ingestion
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine($"Initalizing at {DateTimeOffset.Now}...");
            var sw = Stopwatch.StartNew();

            string connectionString = "";
            switch (Environment.GetEnvironmentVariable("ENVIRONMENT"))
            {
                case "Production": connectionString = "mongodb://root:example@mongo:27017"; break;
                case "Test": connectionString = "mongodb://root:example@mongo-test:27017"; break;
                case "Development": connectionString = "mongodb://root:example@localhost:27018"; break;
            }

            var blockTanksStatsDatabaseSettings = new BlockTanksStatsDatabaseSettings(
                ConnectionString: connectionString,
                DatabaseName: "BlockTanksStats",
                PlayersCollectionName: "Players",
                ClansCollectionName: "Clans"
            );
            var now = DateTime.UtcNow;
            var playerRepository = new PlayerRepository(blockTanksStatsDatabaseSettings, now);
            var clanRepository = new ClanRepository(blockTanksStatsDatabaseSettings, now);

            var playerService = new PlayerService(playerRepository);
            var clanService = new ClanService(clanRepository);

            var httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
              .WaitAndRetryAsync(10, r => TimeSpan.FromSeconds(60));
            var taskCanceledExceptionPolicy = Policy.Handle<TaskCanceledException>()
              .WaitAndRetryAsync(10, r => TimeSpan.FromSeconds(60));
            var pollyPolicy = httpRequestExceptionPolicy.WrapAsync(taskCanceledExceptionPolicy);

            using var blockTanksPlayerAPIAgent = new BlockTanksAPIAgent("https://blocktanks.io", pollyPolicy);
            using var scrapeBTPageService = new ScrapeBTPageService(new ScrapeBTPageService.SeleniumConfig(
                UseRemoteSeleniumChrome: Environment.GetEnvironmentVariable("ENVIRONMENT") != "Development",
                SeleniumChromeUrl: "http://selenium-chrome:4444/wd/hub",
                SeleniumConnectionRetries: 5,
                SeleniumConnectionRetryPeriodSec: 2
            ), "https://blocktanks.io");

            await FetchClanLeaderboardStats(clanService, blockTanksPlayerAPIAgent);

            await FetchClanMemberStats(playerService, blockTanksPlayerAPIAgent, scrapeBTPageService, new[] {
                "RIOT",
                "SWIFT",
                "ZR",
                "DRONE",
            });

            await FetchTrackedPlayerStats(playerService, blockTanksPlayerAPIAgent, scrapeBTPageService, new[]
            {
                "Howie",
                "Tankking",
                "Magpie",
                "Alaska",
                "LORDEVIL",
                "RICO",
                "boriin",
                "m157",
                "VAATHI",
                "fethi",
                "atuka",
                "Ruben123",
                "otsosi",
                "cube",
                "mg123ok",
                "magic_exe",
                "yrene",
                "temp",
                "Luinlanthir",
            });

            sw.Stop();
            Console.WriteLine($"...Done fetching in {sw.Elapsed}. Exiting.");
        }

        private static async Task FetchClanLeaderboardStats(ClanService clanService, BlockTanksAPIAgent blockTanksPlayerAPIAgent)
        {
            Console.WriteLine($"Fetching clan leaderboard...");
            var clans = await blockTanksPlayerAPIAgent.FetchClanLeaderboard();
            foreach (var clan in clans)
            {
                await clanService.AddStatsForClanAsync(clan);
            }
        }

        private static async Task FetchClanMemberStats(PlayerService playerService, BlockTanksAPIAgent blockTanksPlayerAPIAgent, ScrapeBTPageService scrapeBTPageService, IEnumerable<string> clanTags)
        {
            foreach (var clanTag in clanTags)
            {
                Console.WriteLine($"Fetching {clanTag} members...");
                var playerNames = await scrapeBTPageService.GetClanMembersAsync(clanTag);

                Console.WriteLine($"Fetching player stats of {playerNames.Count} players...");
                foreach (var playerName in playerNames)
                {
                    if (!await scrapeBTPageService.ArePlayerStatsHidden(playerName))
                    {
                        var player = await blockTanksPlayerAPIAgent.FetchPlayerAsync(playerName);
                        player.ClanTag = clanTag;
                        await playerService.AddStatsForPlayerAsync(player);
                        Console.WriteLine($"Saved stats for player {playerName}");
                    }
                }
            }
        }

        private static async Task FetchTrackedPlayerStats(PlayerService playerService, BlockTanksAPIAgent blockTanksPlayerAPIAgent, ScrapeBTPageService scrapeBTPageService, IEnumerable<string> trackedPlayerNames)
        {
            Console.WriteLine($"Fetching player stats of tracked players...");
            foreach (var playerName in trackedPlayerNames)
            {
                if (!await scrapeBTPageService.ArePlayerStatsHidden(playerName))
                {
                    var player = await blockTanksPlayerAPIAgent.FetchPlayerAsync(playerName);
                    player.ClanTag = "Tracked Player";
                    await playerService.AddStatsForPlayerAsync(player);
                    Console.WriteLine($"Saved stats for player {playerName}");
                }
            }
        }
    }
}
