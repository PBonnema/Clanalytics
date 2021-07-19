using DataAccess.Models;
using Ingestion.Dtos;
using Ingestion.Exceptions;
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
        private static readonly string STATS_HIDDEN_CONTENT = "This user has their community stats hidden.";
        private static readonly string USER_NOT_FOUND_CONTENT = "The user could not be found.";
        private static readonly string USER_DATA_ENDPOINT = "user/stats/data";
        private static readonly string CLAN_DATA_ENDPOINT = "stats/xp_clan.json";

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

            _logger.Verbose($"GET {_httpClient.BaseAddress}{USER_DATA_ENDPOINT}/{playerName} cookies: {_cookieContainer.GetCookieHeader(new Uri(_httpClient.BaseAddress.ToString()))}");

            return await _pollyPolicy.ExecuteAsync(async (cancellation) =>
            {
                var response = await _httpClient.GetAsync($"/{USER_DATA_ENDPOINT}/{playerName}", cancellation);
                _logger.Verbose($"{playerName} response: {response.StatusCode}");
                var asdf = await response.Content.ReadAsStringAsync(cancellation);

                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadFromJsonAsync<PlayerDto>(cancellationToken: cancellation)).MapToPlayer();
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest
                    && (await response.Content.ReadAsStringAsync(cancellation)) == STATS_HIDDEN_CONTENT)
                {
                    return null;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest
                  && (await response.Content.ReadAsStringAsync(cancellation)) == USER_NOT_FOUND_CONTENT)
                {
                    throw new UserNotFoundException();
                }
                else
                {
                    throw new HttpRequestException(null, null, response.StatusCode);
                }
            }, cancellation);
        }
        
        public async Task<Player> FetchPlayerAsync(string playerName, string asPlayer, string authHash, CancellationToken cancellation = default)
        {
            _cookieContainer.Add(new Cookie("username", Uri.EscapeDataString(asPlayer.ToLower()), "/", _httpClient.BaseAddress.Host));
            _cookieContainer.Add(new Cookie("hash", authHash, "/", _httpClient.BaseAddress.Host));

            return await FetchPlayerAsync(playerName, cancellation);
        }

        public async Task<IEnumerable<Clan>> FetchClanLeaderBoard(CancellationToken cancellation = default)
        {
            _logger.Verbose($"GET {_httpClient.BaseAddress}{CLAN_DATA_ENDPOINT} cookies: {_cookieContainer.GetCookieHeader(new Uri(_httpClient.BaseAddress.ToString()))}");

            return (await _pollyPolicy.ExecuteAsync(async (cancellation) =>
                await _httpClient.GetFromJsonAsync<IEnumerable<ClanDto>>($"/{CLAN_DATA_ENDPOINT}", cancellation), cancellation))
                .Select(c => c.MapToClan());
        }
    }
}
