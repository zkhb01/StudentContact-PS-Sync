using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace StudentContact_PS_Sync
{
    public class NullableDateTimeConverter : TypeConverter<DateTime?>
    {
        public override DateTime? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        public override string? ConvertToString(DateTime? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
                return "";
            return ((DateTime?)value).Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
