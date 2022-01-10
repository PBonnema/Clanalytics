using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IClanRepository : IRepository<Clan>
    {
        Task<IEnumerable<Clan>> GetActiveClansAsync(CancellationToken cancellation = default);
        Task<Clan> GetByClanIdAsync(string clanId, CancellationToken cancellation = default);
        Task UpdateByClanIdAsync(string clanId, Clan clanIn, CancellationToken cancellation = default);
    }
}