using System;

namespace DataAccess.Models
{
    public class PlayerLeaderboardComp
    {
        public DateTime Timestamp { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public double Xp { get; set; }
        public TimeSpan Time { get; set; }
        public int Bullets { get; set; }
    }
}