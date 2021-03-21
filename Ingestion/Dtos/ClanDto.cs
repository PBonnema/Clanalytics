using System.Text.Json.Serialization;

namespace Ingestion.Dtos
{
    public class ClanDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        public ClanLeaderboardCompDto LeaderboardComp { get; set; }
        public string Tag { get; set; }
    }
}
