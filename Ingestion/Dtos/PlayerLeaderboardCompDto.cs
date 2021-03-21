using Ingestion.JsonConverters;
using System;
using System.Text.Json.Serialization;

namespace Ingestion.Dtos
{
    public class PlayerLeaderboardCompDto
    {
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public double Xp { get; set; }
        [JsonConverter(typeof(TimeSpanMinutesConverter))]
        public TimeSpan Time { get; set; }
        public int Bullets { get; set; }
    }
}