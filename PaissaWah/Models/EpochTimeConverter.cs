using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PaissaWah.Models
{
    public class EpochTimeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (text.Contains('.'))
            {
                text = text.Split('.')[0];
            }

            if (long.TryParse(text, out long epochTime))
            {
                return epochTime;
            }

            throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unable to convert '{text}' to {typeof(long)}.");
        }
    }
}
