using DataAccess.Repository;
using MigrateDatabase.MigrationModels;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MigrateDatabase
{
    public class PlayerMigrationsRepository : Repository<PlayerMigrations>
    {
        public PlayerMigrationsRepository(BlockTanksStatsDatabaseSettings settings, DateTime now)
            : base(settings, settings.PlayersCollectionName, now)
        {
        }

        public async Task UpdateByPlayerIdAsync(string playerId, PlayerMigrations playerIn, CancellationToken cancellation = default)
        {
            var a = await _models.ReplaceOneAsync(player => player.PlayerId == playerId, playerIn, cancellationToken: cancellation);
        }
    }
}
