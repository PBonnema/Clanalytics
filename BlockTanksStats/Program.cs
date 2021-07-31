using BlockTanksStats.ViewModels;
using DataAccess.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            CultureInfo.CurrentCulture = culture;

            string connectionString = "";
            string dashboardsPath = "";
            string templatesPath = "./Templates";
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
                    connectionString = "mongodb://root:example@localhost:27018";
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
            var clanLeaderBoardRelativeToClanTag = "RIOT";

            if (Directory.Exists(dashboardsPath))
            {
                Console.WriteLine("Emptying the dashboard directory...");
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
                $"{templatesPath}/ClanLeaderBoardTemplate.xlsx");
            await DashboardGenerator.GenerateAsync(clanLeaderBoardViewModel, $"{dashboardsPath}/Clan Leaderboard.xlsx", now, days, periodLengthDays);

            var tasks = new List<Task>();
            foreach (var clanTag in new[] {
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
                "SPEEDY",
                "E8",
                "TS12",
                "RIES",
                "Tracked Player",
            })
            {
                var clanDashboardViewModel = new ClanDashboardViewModel(clanRepository, playerRepository, clanTag, $"{templatesPath}/ClanDashboardTemplate.xlsx");
                tasks.Add(DashboardGenerator.GenerateAsync(clanDashboardViewModel, $"{dashboardsPath}/{clanTag}.xlsx", now, days, periodLengthDays));
            }

            await Task.WhenAll(tasks);

            sw.Stop();
            Console.WriteLine($"...Done generating in {sw.Elapsed}. Exiting.");
        }
    }
}