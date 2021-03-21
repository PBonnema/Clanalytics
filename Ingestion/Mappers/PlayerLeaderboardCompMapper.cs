using DataAccess.Models;
using Ingestion.Dtos;

namespace Ingestion.Mappers
{
    public static class PlayerLeaderboardCompMapper
    {
        public static PlayerLeaderboardComp MapToLeaderboardComp(this PlayerLeaderboardCompDto leaderboardCompDto) => new()
        {
            Bullets = leaderboardCompDto.Bullets,
            Deaths = leaderboardCompDto.Deaths,
            Kills = leaderboardCompDto.Kills,
            Time = leaderboardCompDto.Time,
            Xp = leaderboardCompDto.Xp,
        };
    }
}
