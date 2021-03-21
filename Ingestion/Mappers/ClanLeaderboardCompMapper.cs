using DataAccess.Models;
using Ingestion.Dtos;

namespace Ingestion.Mappers
{
    public static class ClanLeaderboardCompMapper
    {
        public static ClanLeaderboardComp MapToLeaderboardComp(this ClanLeaderboardCompDto leaderboardCompDto) => new()
        {
            Bullets = leaderboardCompDto.Bullets,
            Deaths = leaderboardCompDto.Deaths,
            Kills = leaderboardCompDto.Kills,
            Time = leaderboardCompDto.Time,
            Xp = leaderboardCompDto.Xp,
            CompletedGames = leaderboardCompDto.CompletedGames,
        };
    }
}
