using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public interface IScrapeBTPageService : IDisposable
    {
        Task<bool> ArePlayerStatsHiddenAsync(string playerName, CancellationToken cancellation = default);
        Task<IList<string>> GetClanMembersAsync(string clanTag, CancellationToken cancellation = default);
    }
}