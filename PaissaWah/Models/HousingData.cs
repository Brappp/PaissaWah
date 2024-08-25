using System;

namespace PaissaWah.Models
{
    public class HousingData
    {
        public int Id { get; set; }
        public string World { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public int WardNumber { get; set; }
        public int PlotNumber { get; set; }
        public string HouseSize { get; set; } = string.Empty;
        public string LottoEntries { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;

        public long FirstSeenEpoch { get; set; }
        public long LastSeenEpoch { get; set; }
        public long? LottoPhaseUntilEpoch { get; set; }

        public bool IsOwned { get; set; }
        public string LottoPhase { get; set; } = string.Empty;

        public DateTime FirstSeen => DateTimeOffset.FromUnixTimeSeconds(FirstSeenEpoch).DateTime;
        public DateTime LastSeen => DateTimeOffset.FromUnixTimeSeconds(LastSeenEpoch).DateTime;
        public DateTime? LottoPhaseUntil => LottoPhaseUntilEpoch.HasValue ? (DateTime?)DateTimeOffset.FromUnixTimeSeconds(LottoPhaseUntilEpoch.Value).DateTime : null;
    }
}
