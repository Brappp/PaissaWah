using System.Collections.Generic;

namespace PaissaWah.Data
{
    public static class WardMapper
    {
        private static readonly Dictionary<int, string> WardTypeMap = new Dictionary<int, string>
        {
            { 1, "FC" }, { 2, "FC" }, { 3, "FC" }, { 4, "FC" }, { 5, "FC" }, { 6, "FC" },
            { 7, "Personal & FC" }, { 8, "Personal & FC" }, { 9, "Personal & FC" }, { 10, "Personal & FC" }, { 11, "Personal & FC" }, { 12, "Personal & FC" },
            { 13, "Personal & FC" }, { 14, "Personal & FC" }, { 15, "Personal & FC" }, { 16, "Personal & FC" }, { 17, "Personal & FC" }, { 18, "Personal & FC" },
            { 19, "Personal & FC" }, { 20, "Personal & FC" },
            { 21, "Personal" }, { 22, "Personal" }, { 23, "Personal" }, { 24, "Personal" },
            { 25, "FC" },
            { 26, "Personal & FC" }, { 27, "Personal & FC" }, { 28, "Personal & FC" }, { 29, "Personal & FC" },
            { 30, "Personal" }
        };

        public static string GetWardType(int wardNumber)
        {
            return WardTypeMap.TryGetValue(wardNumber, out var wardType) ? wardType : "Unknown";
        }
    }
}
