using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public class Player : Model
    {
        public string PlayerId { get; set; }
        public NameStatus NameStatus { get; set; }
        public IEnumerable<PlayerLeaderboardComp> LeaderboardCompHistory { get; set; }
        public IEnumerable<CompletedGame> CompletedGames { get; set; }
        public string DisplayName { get; set; }
        public string ClanTag { get; set; }
        public DateTime JoinDate { get; set; }
        public Community Community { get; set; }
        public XPInfo XPInfo { get; set; }
    }
}
