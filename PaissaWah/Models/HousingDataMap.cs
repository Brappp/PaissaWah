using CsvHelper.Configuration;
using PaissaWah.Handlers;

namespace PaissaWah.Models
{
    public sealed class HousingDataMap : ClassMap<HousingData>
    {
        public HousingDataMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.World).Name("world");
            Map(m => m.District).Name("district");
            Map(m => m.WardNumber).Name("ward_number");
            Map(m => m.PlotNumber).Name("plot_number");
            Map(m => m.HouseSize).Name("house_size");
            Map(m => m.LottoEntries).Name("lotto_entries");
            Map(m => m.Price).Name("price");
            Map(m => m.FirstSeenEpoch).Name("first_seen").TypeConverter<EpochTimeConverter>();
            Map(m => m.LastSeenEpoch).Name("last_seen").TypeConverter<EpochTimeConverter>();
            Map(m => m.LottoPhaseUntilEpoch).Name("lotto_phase_until").TypeConverter<EpochTimeConverter>().TypeConverterOption.NullValues(string.Empty);
            Map(m => m.IsOwned).Name("is_owned");
            Map(m => m.LottoPhase).Name("lotto_phase");
        }
    }
}
