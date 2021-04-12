using DataAccess.Repository;
using Ingestion.Agents;
using Ingestion.Services;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ingestion
{
    class Program
    {
        static async Task Main()
        {
            var now = DateTime.UtcNow;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation($"Initalizing at {now}...");
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
            var playerRepository = new PlayerRepository(blockTanksStatsDatabaseSettings, now);
            var clanRepository = new ClanRepository(blockTanksStatsDatabaseSettings, now);

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

            var playerService = new PlayerService(playerRepository, blockTanksPlayerAPIAgent, scrapeBTPageService, loggerFactory.CreateLogger<PlayerService>());

            await FetchClanLeaderboardStats(clanService, blockTanksPlayerAPIAgent, logger);

            // TODO when a player is updated, all it's stats are overwritting.
            // This means that if a player is both tracked and is a member of a tracked clan, and we first fetch it as a tracked player
            // And then as a member of a tracked clan, the 2e update will remain in the database and the player will be seen as a member of it's clan.
            // We prefer that so fetch clans last.
            var trackedPlayerNames = new[]
            {
                "Jupiter",
                "Jupiter alt",
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
                "Tank tsunami666",
                "Lubiniio",
                "Cidar",
            };

            await playerService.FetchTrackedPlayerStats(trackedPlayerNames);

            await playerService.FetchClanMemberStats(new[] {
                "RIOT",
                "RIOT2",
                "ZR",
                "DRONE",
                "MERC",
                "KRYPTO",
                "FOLDIN",
            });

            sw.Stop();
            logger.LogInformation($"...Done fetching in {sw.Elapsed}. Exiting.");
        }

        private static async Task FetchClanLeaderboardStats(ClanService clanService, BlockTanksAPIAgent blockTanksPlayerAPIAgent, ILogger<Program> logger)
        {
            logger.LogInformation($"Fetching clan leaderboard...");
            var clans = await blockTanksPlayerAPIAgent.FetchClanLeaderboard();
            foreach (var clan in clans)
            {
                await clanService.AddStatsForClanAsync(clan);
            }
        }
    }
}
