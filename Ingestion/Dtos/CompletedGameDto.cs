using Ingestion.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ingestion.Dtos
{
    public class CompletedGameDto
    {
        [JsonPropertyName("m")]
        public string GameMode { get; set; }
        [JsonPropertyName("map")]
        public string MapId { get; set; }
        public bool? S { get; set; }
        public int? HS { get; set; }
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime StartTime { get; set; }
        public bool Completed { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public Dictionary<string, int> Fired { get; set; }
        [JsonPropertyName("kB")]
        public Dictionary<string, int> KillsPerWeapon { get; set; }
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime EndTime { get; set; }
    }
}