using DataAccess.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IClanRepository : IRepository<Clan>
    {
        Task<Clan> GetByClanIdAsync(string clanId, CancellationToken cancellation = default);
        Task UpdateByClanIdAsync(string clanId, Clan clanIn, CancellationToken cancellation = default);
    }
}