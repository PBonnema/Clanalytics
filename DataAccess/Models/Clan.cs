using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public class Clan
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ClanId { get; set; }
        public string Tag { get; set; }
        public IEnumerable<ClanLeaderboardComp> LeaderboardCompHistory { get; set; }
    }
}
