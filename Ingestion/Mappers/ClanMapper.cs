using DataAccess.Models;
using Ingestion.Dtos;
using System.Collections.Generic;

namespace Ingestion.Mappers
{
    public static class ClanMapper
    {
        public static Clan MapToClan(this ClanDto clanDto) => new()
        {
            ClanId = clanDto.Id,
            Tag = clanDto.Tag,
            LeaderboardCompHistory = new List<ClanLeaderboardComp> { clanDto.LeaderboardComp.MapToLeaderboardComp() },
        };
    }
}
