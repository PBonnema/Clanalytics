using System.Collections.Generic;

namespace Ingestion.Models
{
    public class OwnedClans
    {
        public IReadOnlyDictionary<string, (string asPlayerName, string authHash)> OwnedClanCredentials { get; set; }
    }
}
