using DataAccess.Repository;
using MigrateDatabase.MigrationModels;
using System.Threading;
using System.Threading.Tasks;

namespace MigrateDatabase.Repositories
{
    public interface IPlayerMigrationsRepository : IRepository<PlayerMigrations>
    {
        Task UpdateByPlayerIdAsync(string playerId, PlayerMigrations playerIn, CancellationToken cancellation = default);
    }
}