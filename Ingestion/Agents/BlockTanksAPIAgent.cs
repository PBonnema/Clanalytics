using DataAccess.Models;
using Ingestion.Dtos;
using Ingestion.Mappers;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Agents
{
    public class BlockTanksAPIAgent : IBlockTanksAPIAgent
    {
        private readonly CookieContainer _cookieContainer;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _pollyPolicy;
        private readonly ILogger _logger;

        public BlockTanksAPIAgent(string blockTanksUrl, IAsyncPolicy pollyPolicy, ILogger logger)
        {
            _cookieContainer = new CookieContainer();
            var httpHandler = new HttpClientHandler { CookieContainer = _cookieContainer };
            _httpClient = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri(blockTanksUrl),
            };

            _pollyPolicy = pollyPolicy;
            _logger = logger;
        }

        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<Player> FetchPlayerAsync(string playerName, CancellationToken cancellation = default)
        {
            playerName = Uri.EscapeDataString(playerName.ToLower());

            _logger.Verbose($"GET {_httpClient.BaseAddress}user/stats/data/{playerName} cookies: {_cookieContainer.GetCookieHeader(new Uri(_httpClient.BaseAddress.ToString()))}");

            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<PlayerDto>($"/user/stats/data/{playerName}", cancellation), cancellation))
                .MapToPlayer();
        }
        
        public async Task<Player> FetchPlayerAsync(string playerName, string asPlayer, string authHash, CancellationToken cancellation = default)
        {
            _cookieContainer.Add(new Cookie("username", Uri.EscapeDataString(asPlayer.ToLower()), "/", _httpClient.BaseAddress.Host));
            _cookieContainer.Add(new Cookie("hash", authHash, "/", _httpClient.BaseAddress.Host));
            playerName = Uri.EscapeDataString(playerName.ToLower());

            _logger.Verbose($"GET {_httpClient.BaseAddress}user/stats/data/{playerName} cookies: {_cookieContainer.GetCookieHeader(new Uri(_httpClient.BaseAddress.ToString()))}");

            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<PlayerDto>($"/user/stats/data/{playerName}", cancellation), cancellation))
                .MapToPlayer();
        }

        public async Task<IEnumerable<Clan>> FetchClanLeaderboard(CancellationToken cancellation = default)
        {
            _logger.Verbose($"GET {_httpClient.BaseAddress}stats/xp_clan.json cookies: {_cookieContainer.GetCookieHeader(new Uri(_httpClient.BaseAddress.ToString()))}");

            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<IEnumerable<ClanDto>>("/stats/xp_clan.json", cancellation), cancellation))
                .Select(c => c.MapToClan());
        }
    }
}
