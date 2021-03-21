using MigrateDatabase.Models;
using MongoDB.Driver;
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
                case "Development": connectionString = "mongodb://root:example@localhost:27017"; break;
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("BlockTanksStats");

            var players = database.GetCollection<Player>("Players");

            foreach (var player in await (await players.FindAsync(_ => true)).ToListAsync())
            {
                await players.ReplaceOneAsync(p => p.PlayerId == player.PlayerId, player);
            }
        }
    }
}
