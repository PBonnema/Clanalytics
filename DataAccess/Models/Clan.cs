using System.Collections.Generic;

namespace DataAccess.Models
{
    public class Clan : Model
    {
        public string ClanId { get; set; }
        public string Tag { get; set; }
        public IEnumerable<ClanLeaderboardComp> LeaderboardCompHistory { get; set; }
    }
}
