using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public class CompletedGame
    {
        public string GameMode { get; set; }
        public string MapId { get; set; }
        public bool? S { get; set; }
        public int? HS { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Completed { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public Dictionary<string, int> Fired { get; set; }
        public Dictionary<string, int> KillsPerWeapon { get; set; }
    }
}
