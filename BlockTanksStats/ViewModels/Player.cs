﻿using System.Collections.Generic;
using System.Linq;

namespace BlockTanksStats.ViewModels
{
    public class Player
    {
        public string Name { get; set; }
        public double CurrentTotalXp { get; set; }
        public IEnumerable<double?> XP { get; set; } = Enumerable.Empty<double?>();
        public int CurrentTotalGames { get; set; }
        public IEnumerable<int?> Games { get; set; } = Enumerable.Empty<int?>();
        public double? AverageKDRNumber { get; set; }
        public string AverageKDR { get; set; }
        public IEnumerable<string> KDR { get; set; } = Enumerable.Empty<string>();
    }
}
