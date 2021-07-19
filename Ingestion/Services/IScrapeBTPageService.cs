using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public interface IScrapeBTPageService : IDisposable
    {
        Task<(IList<string>, bool)> GetClanMembersAsync(string clanTag, CancellationToken cancellation = default);
    }
}