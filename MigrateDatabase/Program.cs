using DataAccess.Repository;
using System;
using System.Threading.Tasks;

namespace MigrateDatabase
{
    class Program
    {
        // See http://mongodb.github.io/mongo-csharp-driver/2.12/reference/bson/mapping/schema_changes/ for guidance

        static async Task Main(string[] args)
        {
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
            var playerRepository = new PlayerMigrationsRepository(blockTanksStatsDatabaseSettings, DateTime.UtcNow);

            foreach (var player in await playerRepository.GetAsync())
            {
                await playerRepository.UpdateByPlayerIdAsync(player.PlayerId, player);
            }
        }
    }
}
