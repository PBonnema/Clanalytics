using DataAccess.Models;
using MigrateDatabase.JsonConverters;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace MigrateDatabase.Models
{
    public class Player : DataAccess.Models.Player, ISupportInitialize
    {
        private static readonly IEnumerable<Action<Player>> migrations = new Action<Player>[]
        {
            (Player p) =>
            {
                if (p.ExtraElements.TryGetValue("LeaderBoardCompHistory", out object leaderBoardCompHistoryValue))
                {
                    var leaderBoardCompHistory = (List<object>)leaderBoardCompHistoryValue;

                    // Remove the element so that it doesn't get persisted back to the database
                    p.ExtraElements.Remove("LeaderBoardCompHistory");

                    string json = JsonSerializer.Serialize(leaderBoardCompHistory);
                    p.LeaderboardCompHistory = JsonSerializer.Deserialize<IEnumerable<PlayerLeaderboardComp>>(json, jsonSerializerOptions);
                }
            },
            (Player p) =>
            {
                if (p.ExtraElements.TryGetValue("ClanName", out object clanNameValue))
                {
                    var clanName = (string)clanNameValue;

                    // remove the Name element so that it doesn't get persisted back to the database
                    p.ExtraElements.Remove("ClanName");

                    p.ClanTag = clanName;
                }
            },
        };

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            Converters =
            {
                new TimeSpanParseConverter(),
            },
        };

        [BsonExtraElements]
        public IDictionary<string, object> ExtraElements { get; set; }

        public void BeginInit()
        {
            // Nothing to do before deserialisation
        }

        public void EndInit()
        {
            ExtraElements ??= new Dictionary<string, object> { };
            foreach (var migration in migrations)
            {
                migration(this);
            }
        }
    }
}
