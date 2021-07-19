using DataAccess.Repository;
using Ingestion.Agents;
using Ingestion.Models;
using Ingestion.Services;
using Polly;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ingestion
{
    class Program
    {
        static async Task Main()
        {
            var now = DateTime.UtcNow;
            var useRemoteSeleniumForDev = true;

            var logFilePath = Environment.GetEnvironmentVariable("LOG_PATH");
            try
            {
                using var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File($"{logFilePath}/Ingestion.txt", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(LogEventLevel.Verbose)
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

                    var clanService = new ClanService(clanRepository, logger.ForContext<ClanService>());

                    var ioExceptionPolicy = Policy.Handle<IOException>()
                      .WaitAndRetryAsync(10, r => TimeSpan.FromSeconds(r * r * 0.5));
                    var httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
                      .WaitAndRetryAsync(10, r => TimeSpan.FromSeconds(r * r * 0.5));
                    var taskCanceledExceptionPolicy = Policy.Handle<TaskCanceledException>()
                      .WaitAndRetryAsync(10, r => TimeSpan.FromSeconds(r * r * 0.5));
                    var pollyPolicy = ioExceptionPolicy.WrapAsync(httpRequestExceptionPolicy);
                    pollyPolicy = pollyPolicy.WrapAsync(taskCanceledExceptionPolicy);

                    using var blockTanksPlayerAPIAgent = new BlockTanksAPIAgent("https://blocktanks.io", pollyPolicy, logger.ForContext<BlockTanksAPIAgent>());
                    using var scrapeBTPageService = new ScrapeBTPageService(new ScrapeBTPageService.SeleniumConfig(
                        UseRemoteSeleniumChrome: useRemoteSeleniumForDev || Environment.GetEnvironmentVariable("ENVIRONMENT") != "Development",
                        SeleniumChromeUrl: seleniumChromeUrl,
                        SeleniumConnectionRetries: 5,
                        SeleniumConnectionRetryPeriodSec: 2
                    ), "https://blocktanks.io");

                    var ownedClans = new OwnedClans { OwnedClanCredentials = new Dictionary<string, (string, string)> {
                        //{ "RIOT", ( "Jupiter", "$2a$08$VMc9J5EvpnHFlVgXm5oaDuA.a4MUZ49Bf49p6P8iFi4GE/YmRjQ5K") },
                        { "RIOT2", ( "xRIOTx", "$2a$08$mjkrpe4CgwCwl8Lq5pup1epImuCfHmS8RK4DPb8LkgCnR9jPBDJ7e") },
                        { "RIOT3", ( "Jupiter alt", "$2a$08$63S3JMJzOawaOPuyWmu3aepHsZjymDNunjbFzCNmdI/feFKHkS1D6") },
                    } };
                    var playerService = new PlayerService(playerRepository, blockTanksPlayerAPIAgent, scrapeBTPageService, logger.ForContext<PlayerService>(), ownedClans);

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
                        "SENTRY",
                        "PRO",
                        "TD",
                        "SPEEDY",
                        "E8",
                        "TS12",
                        "RIES",
                    });

                    await FetchClanLeaderboardStats(clanService, blockTanksPlayerAPIAgent, logger);

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
                        "Yangshuo",
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
                        "Jhalmarpro",
                        "drdynamic333",
                        "Every1",
                        "Zero 2",
                        "Fleur",
                        "Live Life",
                        "Foreverkitty",
                        "Zeeto",
                        "xDNevioxD",
                        "Mucahit",
                        "Benjamin1124",
                        "MATHIASCRACK",
                        "Mister D",
                        "Io pro",
                        "Doctor Surviv",
                        "Catkid",
                        "TwinX22",
                        "Jingles",
                        "TKSS1216",
                        "Surfer",
                        "Khu1",
                        "EPiccc",
                        "Jayden_funeez",
                        "lionclaw",
                        "oof boi",
						"Izzz",
						"Jason12345",
                    };

                    var groupSize = 20;
                    for(var i = 0; i < trackedPlayerNames.Length; i+= groupSize)
                    {
                        var endIndex = Math.Min(trackedPlayerNames.Length - 1, i + groupSize);
                        await playerService.FetchTrackedPlayerStats(trackedPlayerNames[i..endIndex]);
                    }

                    sw.Stop();
                    logger.Information($"...Done fetching in {sw.Elapsed}. Exiting.");
                }
                catch (Exception e)
                {
                    logger.Fatal($"Oh oh...: ${e}");
                    throw;
                }
            }
            catch (Exception e)
            {
                File.AppendAllText($"{DateTimeOffset.UtcNow} {logFilePath}/Error.txt", e.ToString());
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
