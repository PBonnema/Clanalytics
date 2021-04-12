using DataAccess.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MigrateDatabase
{
    public class PlayerLeaderboardCompComparer : IEqualityComparer<PlayerLeaderboardComp>
    {
        public bool Equals(PlayerLeaderboardComp x, PlayerLeaderboardComp y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x == y || (x.Timestamp == y.Timestamp
                && x.Xp == y.Xp
                && x.Kills == y.Kills
                && x.Deaths == y.Deaths
                && x.Bullets == y.Bullets);
        }

        public int GetHashCode([DisallowNull] PlayerLeaderboardComp obj)
        {
            return obj.Timestamp.GetHashCode()
                ^ obj.Xp.GetHashCode()
                ^ obj.Kills.GetHashCode()
                ^ obj.Deaths.GetHashCode()
                ^ obj.Bullets.GetHashCode();
        }
    }
}
