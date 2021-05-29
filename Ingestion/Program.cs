using DataAccess.Repository;
using Ingestion.Agents;
using Ingestion.Services;
using Polly;
using Serilog;
using Serilog.Events;
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

            var logFilePath = Environment.GetEnvironmentVariable("LOG_PATH");
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File($"{logFilePath}/Ingestion.txt", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();

            logger.Information($"Initalizing at {now}...");

            try
            {
                var sw = Stopwatch.StartNew();

                var connectionString = "";
                var seleniumChromeUrl = "";
                switch (Environment.GetEnvironmentVariable("ENVIRONMENT"))
                {
                    case "Production":
                        connectionString = "mongodb://root:example@mongo:27017";
                        seleniumChromeUrl = "http://selenium-chrome:4444/wd/hub";
                        break;
                    case "Test":
                        connectionString = "mongodb://root:example@mongo-test:27017";
                        seleniumChromeUrl = "http://selenium-chrome:4444/wd/hub";
                        break;
                    case "Development":
                        connectionString = "mongodb://root:example@localhost:27018";
                        seleniumChromeUrl = "http://localhost:4444/wd/hub";
                        break;
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
                    SeleniumChromeUrl: seleniumChromeUrl,
                    SeleniumConnectionRetries: 5,
                    SeleniumConnectionRetryPeriodSec: 2
                ), "https://blocktanks.io");

                var playerService = new PlayerService(playerRepository, blockTanksPlayerAPIAgent, scrapeBTPageService, logger.ForContext<PlayerService>());

                await FetchClanLeaderboardStats(clanService, blockTanksPlayerAPIAgent, logger);

                // TODO when a player is updated, all it's stats are overwritting.
                // This means that if a player is both tracked and is a member of a tracked clan, and we first fetch it as a member of a clan
                // And then as a tracked player. The 1e update will remain in the database and the player will be seen as a member of it's clan.
                // We prefer that so fetch clans first.
                await playerService.FetchClanMemberStats(new[] {
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
                });

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
                    "XXHyperGamerXX",
                    "GabGaming Pro38",
                    "sub-zero",
                    "JokiPau",
                    "fuinloce",
                    "HD Gamer",
                    "_RIOT_ fffffffy",
                    "9205672",
                    "-Xeno-",
                    "718",
                    "IHasZero",
                    "Colonial",
                    "xRIOTx",
                    "Revelide",
                    "Bbc",
                    "TheSniperDuo",
                    "Denooo",
                    "Arjun Pro ff 2",
                    "V God",
                    "ARISTHEOREO",
                    "xB-RADx_ALT",
                    "Milliana",
                    "Coronaries VN",
                    "Dream -SMP",
                    "ichi",
                    "Atom",
                    "-Pumpkin-",
                    "Kíra",
                    "Onii-Chan",
                    "Garga",
                    "Retro",
                    "nOtelite",
                    "slg jujudark",
                    "THE DARK TANK",
                    "le7",
                    "Zynox81",
                    "lopik",
                    "Yanshuo",
                    "BlueLeaf",
                    "Block Slayer",
                    "Harpro",
                    "bow and arrow",
                    "Eazy clap",
                    "Geyyyy",
                    "PoopyDoopy",
                    "Pepe Da Frog",
                    "jiggler",
                    "Cheams",
                    "pillow",
                    "78787878",
                    "Hendrik",
                    "abhays72",
                    "James edward",
                    "JCREX",
                    "Idol",
                    "gag123",
                    "SuperHugo",
                    "palches",
                    "Your final",
                    "Hanna",
                    "CampKing",
                    "Strike",
					"Power X",
					"ChAs81",
					"Keep Killing It",
					"Dev1ce",
                };

                await playerService.FetchTrackedPlayerStats(trackedPlayerNames);

                sw.Stop();
                logger.Information($"...Done fetching in {sw.Elapsed}. Exiting.");
            }
            catch (Exception e)
            {
                logger.Fatal($"...An exception occurred: ${e}");
                throw;
            }
        }

        private static async Task FetchClanLeaderboardStats(ClanService clanService, BlockTanksAPIAgent blockTanksPlayerAPIAgent, ILogger logger)
        {
            logger.Information($"Fetching clan leaderboard...");
            var clans = await blockTanksPlayerAPIAgent.FetchClanLeaderboard();
            foreach (var clan in clans)
            {
                await clanService.AddStatsForClanAsync(clan);
            }
        }
    }
}
