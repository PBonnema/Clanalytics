using DataAccess.Models;
using DataAccess.Repository;
using Ingestion.Agents;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IBlockTanksAPIAgent _blockTanksPlayerAPIAgent;
        private readonly IScrapeBTPageService _scrapeBTPageService;
        private readonly ILogger _logger;

        public PlayerService(IPlayerRepository playerRepository, IBlockTanksAPIAgent blockTanksPlayerAPIAgent, IScrapeBTPageService scrapeBTPageService, ILogger logger)
        {
            _playerRepository = playerRepository;
            _blockTanksPlayerAPIAgent = blockTanksPlayerAPIAgent;
            _scrapeBTPageService = scrapeBTPageService;
            _logger = logger;
        }

        public async Task AddStatsForPlayerAsync(Player player, CancellationToken cancellation = default)
        {
            var storedPlayer = await _playerRepository.GetByPlayerIdAsync(player.PlayerId, cancellation);
            if (storedPlayer == null)
            {
                await _playerRepository.CreateAsync(player, cancellation);
                _logger.Debug($"Added stats for new player {player.DisplayName}");
            }
            else
            {
                player.LeaderboardCompHistory = storedPlayer.LeaderboardCompHistory.Concat(player.LeaderboardCompHistory);
                player.Id = storedPlayer.Id;
                await _playerRepository.UpdateByPlayerIdAsync(storedPlayer.PlayerId, player, cancellation);
                _logger.Debug($"Updated stats for player {player.DisplayName}");
            }
        }

        public async Task FetchClanMemberStats(IEnumerable<string> clanTags, CancellationToken cancellation = default)
        {
            foreach (var clanTag in clanTags)
            {
                _logger.Information($"Fetching {clanTag} members...");
                var playerNames = await _scrapeBTPageService.GetClanMembersAsync(clanTag, cancellation);
                await FetchPlayersStats(playerNames, clanTag, cancellation);
            }
        }

        public async Task FetchTrackedPlayerStats(IEnumerable<string> trackedPlayerNames, CancellationToken cancellation = default)
        {
            _logger.Information($"Fetching stats of tracked players...");
            await FetchPlayersStats(trackedPlayerNames, "Tracked Player", cancellation);
        }

        private async Task FetchPlayersStats(IEnumerable<string> playerNames, string clanTag, CancellationToken cancellation = default)
        {
            // Prevent players from being fetched twice if they are both tracked separately and in a tracked clan
            playerNames = await _playerRepository.FilterPlayersNotInClanAsync(playerNames, cancellation);

            _logger.Information($"Fetching stats of {playerNames.Count()} players...");
            foreach (var playerName in playerNames)
            {
                if (!await _scrapeBTPageService.ArePlayerStatsHiddenAsync(playerName, cancellation))
                {
                    var player = await _blockTanksPlayerAPIAgent.FetchPlayerAsync(playerName, cancellation);
                    player.ClanTag = clanTag;
                    await AddStatsForPlayerAsync(player, cancellation);
                }
                else
                {
                    _logger.Warning($"{playerName} has their stats hidden or isn't found.");
                }
            }
        }
    }
}
