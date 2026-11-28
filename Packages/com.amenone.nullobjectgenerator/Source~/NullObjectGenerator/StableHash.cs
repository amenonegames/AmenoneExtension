using System.Globalization;

namespace Amenone.NullObjectGenerator
{
    internal static class StableHash
    {
        private const uint OffsetBasis = 2166136261;
        private const uint Prime = 16777619;

        public static string Of(string text)
        {
            var hash = OffsetBasis;
            foreach (var character in text)
            {
                hash = unchecked((hash ^ character) * Prime);
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }
}
