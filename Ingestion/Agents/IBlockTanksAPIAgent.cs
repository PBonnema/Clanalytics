using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Agents
{
    public interface IBlockTanksAPIAgent : IDisposable
    {
        Task<IEnumerable<Clan>> FetchClanLeaderboard(CancellationToken cancellation = default);
        Task<Player> FetchPlayerAsync(string playerName, CancellationToken cancellation = default);
    }
}