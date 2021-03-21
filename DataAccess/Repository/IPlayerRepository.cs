using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IPlayerRepository
    {
        Task<Player> CreateAsync(Player player, CancellationToken cancellation = default);
        Task<IEnumerable<Player>> GetAsync(CancellationToken cancellation = default);
        Task<Player> GetByPlayerIdAsync(string playerId, CancellationToken cancellation = default);
        Task RemoveByPlayerIdAsync(Player playerIn, CancellationToken cancellation = default);
        Task RemoveByPlayerIdAsync(string playerId, CancellationToken cancellation = default);
        Task UpdateByPlayerIdAsync(string playerId, Player playerIn, CancellationToken cancellation = default);
    }
}