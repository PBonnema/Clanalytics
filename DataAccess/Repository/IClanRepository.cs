using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IClanRepository
    {
        Task<Clan> CreateAsync(Clan clan, CancellationToken cancellation = default);
        Task<IEnumerable<Clan>> GetAsync(CancellationToken cancellation = default);
        Task<Clan> GetByClanIdAsync(string clanId, CancellationToken cancellation = default);
        Task RemoveByClanIdAsync(Clan clanIn, CancellationToken cancellation = default);
        Task RemoveByClanIdAsync(string clanId, CancellationToken cancellation = default);
        Task UpdateByClanIdAsync(string clanId, Clan clanIn, CancellationToken cancellation = default);
    }
}