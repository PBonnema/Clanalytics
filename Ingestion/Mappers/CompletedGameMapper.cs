using DataAccess.Models;
using Ingestion.Dtos;

namespace Ingestion.Mappers
{
    public static class CompletedGameMapper
    {
        public static CompletedGame MapToCompletedGame(this CompletedGameDto completedGameMapper) => new()
        {
            Completed = completedGameMapper.Completed,
            Deaths = completedGameMapper.Deaths,
            EndTime = completedGameMapper.EndTime,
            Fired = completedGameMapper.Fired,
            GameMode = completedGameMapper.GameMode,
            HS = completedGameMapper.HS,
            Kills = completedGameMapper.Kills,
            KillsPerWeapon = completedGameMapper.KillsPerWeapon,
            MapId = completedGameMapper.MapId,
            S = completedGameMapper.S,
            StartTime = completedGameMapper.StartTime,
        };
    }
}
