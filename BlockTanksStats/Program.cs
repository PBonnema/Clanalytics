﻿using BlockTanksStats.ViewModels;
using DataAccess.Repository;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Discord.Rest.Extensions;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BlockTanksStats
{
    class Program
    {
        static async Task Main()
        {
            var now = DateTime.UtcNow;

            var logFilePath = Environment.GetEnvironmentVariable("LOG_PATH");
            try
            {
                using var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File($"{logFilePath}/Analytics.txt", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(LogEventLevel.Debug)
                    .CreateLogger();

                logger.Information($"Initalizing at {now}...");

                try
                {
                    var config = new ConfigurationBuilder()
                        .AddUserSecrets<Program>(true)
                        .AddKeyPerFile("/run/secrets/", true)
                        .Build();

                    var sw = Stopwatch.StartNew();

                    var culture = CultureInfo.CreateSpecificCulture("nl-NL");
                    // TODO bug. Onderstaande regel werkt niet.
                    culture.NumberFormat.NumberDecimalDigits = 1;
                    CultureInfo.CurrentCulture = culture;

                    var connectionString = "";
                    var dashboardsPath = "";
                    var templatesPath = "./Templates";
                    switch (Environment.GetEnvironmentVariable("ENVIRONMENT"))
                    {
                        case "Production":
                            connectionString = "mongodb://root:example@mongo:27017";
                            dashboardsPath = "/app/Dashboards";
                            break;
                        case "Test":
                            connectionString = "mongodb://root:example@mongo-test:27017";
                            dashboardsPath = "/app/Dashboards";
                            break;
                        case "Development":
                            connectionString = "mongodb://root:example@localhost:27018"; // Port of the test DB
                            dashboardsPath = "../../../../Dashboards-test";
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
                    var days = 1;
                    var periodLengthDays = int.Parse(Environment.GetEnvironmentVariable("PERIOD_LENGHT_DAYS"));
                    var clanLeaderBoardRelativeToClanTag = "RIOT"; // TODO change to an ID

                    if (Directory.Exists(dashboardsPath))
                    {
                        logger.Information("Emptying the dashboard directory...");
                        foreach (var item in Directory.GetDirectories(dashboardsPath))
                        {
                            Directory.Delete(item, recursive: true);
                        }

                        foreach (var item in Directory.GetFiles(dashboardsPath))
                        {
                            File.Delete(item);
                        }
                    }

                    var clanLeaderBoardViewModel = new ClanLeaderBoardViewModel(
                        clanRepository,
                        playerRepository,
                        clanLeaderBoardRelativeToClanTag,
                        $"{templatesPath}/ClanLeaderBoardTemplate.xlsx",
                        logger);
                    await DashboardGenerator.GenerateAsync(clanLeaderBoardViewModel, $"{dashboardsPath}/Clan Leaderboard.xlsx", now, days, periodLengthDays, logger);

                    var tasks = new List<Task>();
                    // TODO change to IDs
                    foreach (var clanTag in new[] {
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
                        "Tracked Player",
                    })
                    {
                        var clanDashboardViewModel = new ClanDashboardViewModel(clanRepository, playerRepository, clanTag, $"{templatesPath}/ClanDashboardTemplate.xlsx", logger);
                        tasks.Add(DashboardGenerator.GenerateAsync(clanDashboardViewModel, $"{dashboardsPath}/{clanTag}.xlsx", now, days, periodLengthDays, logger));
                    }

                    await Task.WhenAll(tasks);

                    logger.Information("Posting dashboards in Discord...");
                    // Get this webhookid and token by creating a webhook in Discord and open it's link. That URL will contain them.
                    var webhookId = new Snowflake(871070885665177630ul);
                    var serviceCollection = new ServiceCollection()
                        .AddDiscordRest(_ => config["botToken"])
                        .AddSingleton(sp => new DashboardUploader(
                            sp.GetRequiredService<IDiscordRestWebhookAPI>(),
                            sp.GetRequiredService<IDiscordRestChannelAPI>(),
                            webhookId,
                            config["webhookToken"],
                            logger))
                        .BuildServiceProvider();
                    var dashboardUploader = serviceCollection.GetRequiredService<DashboardUploader>();
                    await dashboardUploader.UploadAsync(dashboardsPath, new[] {
                        "Clan Leaderboard.xlsx", "RIOT.xlsx", "RIOT2.xlsx", "Tracked Player.xlsx",
                    });

                    sw.Stop();
                    logger.Information($"...Done generating in {sw.Elapsed}. Exiting.");
                }
                catch (Exception e)
                {
                    logger.Fatal($"Oh oh...: ${e}");
                    throw;
                }
            }
            catch (Exception e)
            {
                static string a(Exception b) => $"{b}\n{(b.InnerException == null ? "\n" : a(b.InnerException))}";
                File.AppendAllText($"{logFilePath}/AnalyticsError{DateTimeOffset.UtcNow:yyyymmdd}.txt", a(e));
                throw;
            }
        }
    }
}
