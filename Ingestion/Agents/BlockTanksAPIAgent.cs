using DataAccess.Models;
using Ingestion.Dtos;
using Ingestion.Mappers;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Agents
{
    public class BlockTanksAPIAgent : IBlockTanksAPIAgent
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _pollyPolicy;

        public BlockTanksAPIAgent(string blockTanksUrl, IAsyncPolicy pollyPolicy)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(blockTanksUrl),
            };

            _pollyPolicy = pollyPolicy;
        }

        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<Player> FetchPlayerAsync(string playerName, CancellationToken cancellation = default)
        {
            playerName = Uri.EscapeDataString(playerName.ToLower());
            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<PlayerDto>($"/user/stats/data/{playerName}", cancellation), cancellation))
                .MapToPlayer();
        }

        public async Task<IEnumerable<Clan>> FetchClanLeaderboard(CancellationToken cancellation = default)
        {
            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<IEnumerable<ClanDto>>("/stats/xp_clan.json", cancellation), cancellation))
                .Select(c => c.MapToClan());
        }
    }
}
