using System.Collections.Generic;
using System.Linq;

namespace BlockTanksStats.ViewModels
{
    public class Clan
    {
        public string Tag { get; set; }
        public double? DaysUntilCatchup { get; set; }
        public double CurrentTotalXp { get; set; }
        public IEnumerable<double> XP { get; set; } = Enumerable.Empty<double>();
    }
}
