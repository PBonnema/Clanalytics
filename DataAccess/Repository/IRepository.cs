using DataAccess.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface IRepository<T> where T : Model
    {
        Task<T> CreateAsync(T model, CancellationToken cancellation = default);
        Task<IEnumerable<T>> GetAsync(CancellationToken cancellation = default);
        Task RemoveByIdAsync(string id, CancellationToken cancellation = default);
        Task RemoveAsync(T modelIn, CancellationToken cancellation = default);
        Task UpdateAsync(string id, T modelIn, CancellationToken cancellation = default);
    }
}