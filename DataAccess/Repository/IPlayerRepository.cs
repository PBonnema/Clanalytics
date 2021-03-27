using DataAccess.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Player> GetByPlayerIdAsync(string playerId, CancellationToken cancellation = default);
        Task UpdateByPlayerIdAsync(string playerId, Player playerIn, CancellationToken cancellation = default);
    }
}