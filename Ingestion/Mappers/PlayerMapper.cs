using DataAccess.Models;
using Ingestion.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Ingestion.Mappers
{
    public static class PlayerMapper
    {
        public static Player MapToPlayer(this PlayerDto playerDto) => new()
        {
            PlayerId = playerDto.Id,
            Community = playerDto.Community.MapToCommunity(),
            CompletedGames =  playerDto.CompletedGames.Select(cg => cg.MapToCompletedGame()),
            DisplayName = playerDto.DisplayName,
            JoinDate = playerDto.JoinDate,
            LeaderboardCompHistory = new List<PlayerLeaderboardComp> { playerDto.LeaderboardComp.MapToLeaderboardComp() },
            NameStatus = playerDto.NameStatus.MapToNameStatus(),
            XPInfo = playerDto.XPInfo.MapToXPInfo(),
        };
    }
}
