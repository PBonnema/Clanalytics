using Ingestion.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ingestion.Dtos
{
    public class PlayerDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        public NameStatusDto NameStatus { get; set; }
        public PlayerLeaderboardCompDto LeaderboardComp { get; set; }
        public IEnumerable<CompletedGameDto> CompletedGames { get; set; }
        public string DisplayName { get; set; }
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime JoinDate { get; set; }
        public CommunityDto Community { get; set; }
        public XPInfoDto XPInfo { get; set; }
    }
}
