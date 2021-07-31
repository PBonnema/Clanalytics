using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<IEnumerable<string>> FilterPlayersNotInClanAsync(IEnumerable<string> playerNames, CancellationToken cancellation = default);
        Task<IEnumerable<Player>> GetAllByNamesAsync(IEnumerable<string> playerNames, CancellationToken cancellation = default);
        Task<IEnumerable<Player>> GetByClanAsync(string clanTag, CancellationToken cancellation = default);
        Task<Player> GetByPlayerIdAsync(string playerId, CancellationToken cancellation = default);
        Task UpdateByPlayerIdAsync(string playerId, Player playerIn, CancellationToken cancellation = default);
    }
}