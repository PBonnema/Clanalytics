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
            var useRemoteSeleniumForDev = false;

            var logFilePath = Environment.GetEnvironmentVariable("LOG_PATH");
            try
            {
                using var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File($"{logFilePath}/Ingestion.txt", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(LogEventLevel.Debug)
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
                        "MERC",
                        "FOLDIN",
                        "SPEEDY",
                        "E8",
                        "SKILL",
                        "ZEMFR",
                        "RP2",
                        "AIE",
                        "SIEGE",
                        "GQ",
                        "DID",
                    });

                    await FetchClanLeaderboardStats(clanService, blockTanksPlayerAPIAgent, logger);

                    var trackedPlayerNames = new[]
                    {
                        "Jupiter",
                        //"Jupiter alt",
                        //"Eazy clap",
                        //"Izzz",
                        //"fethi",
                        //"Bomihu",
                        //"otsosi",
                        //"Catkid",
                        //"palches",
                        //"Hendrik",
                        //"RICO",
                        //"nOtelite",
                        //"TheSniperDuo",
                        //"oof boi",
                        //"JCREX",
                        //"Keep Killing It",
                        //"James edward",
                        //"lopik",
                        //"Denooo",
                        //"le7",
                        //"Idol",
                        //"EPiccc",
                        //"Garga",
                        //"Howie",
                        //"Tankking",
                        //"Alaska",
                        //"mg123ok",
                        //"Luinlanthir",
                        //"Cidar",
                        //"fuinloce",
                        //"_RIOT_ fffffffy",
                        //"THE DARK TANK",
                        //"78787878",
                        //"ChAs81",
                        //"Dev1ce",
                        //"Fleur",
                        //"Zeeto",
                        //"Every1",
                        //"Jason12345",
                        //"Xeno Hype",
                        //"Howling crusher",
                        //"bluff",
                        //"PeterDanielYT",
                        //"Lyasson",
                        //"ProVibes",
                        //"GoodMorning9000",
                        //"duck",
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
                File.AppendAllText($"{DateTimeOffset.UtcNow} {logFilePath}/IngestionError.txt", e.ToString());
                throw;
            }
        }

        private static async Task FetchClanLeaderboardStats(ClanService clanService, BlockTanksAPIAgent blockTanksPlayerAPIAgent, ILogger logger)
        {
            logger.Information($"Fetching clan leaderboard...");
            var clans = await blockTanksPlayerAPIAgent.FetchClanLeaderBoard();
            foreach (var clan in clans)
            {
                try
                {
                    await clanService.AddStatsForClanAsync(clan);
                }
                catch (Exception e)
                {
                    logger.Error($"Adding stats of clan {clan.Tag} for the clan leaderboard failed. Continuing...: ${e}");
                }
            }
        }
    }
}
