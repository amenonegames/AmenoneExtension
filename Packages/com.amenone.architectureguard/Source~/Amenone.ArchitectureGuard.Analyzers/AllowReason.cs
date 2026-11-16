using System.Linq;

namespace Amenone.ArchitectureGuard.Analyzers
{
    internal static class AllowReason
    {
        public const int MinimumLength = 10;

        public const string Missing = "書かれていません";
        public const string NotConstant = "文字列定数ではありません";
        private const string Empty = "空です";
        private const string WhitespaceOnly = "空白だけです";

        public static string? FindProblem(string? reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return Empty;
            }

            var meaningfulLength = reason!.Count(character => !char.IsWhiteSpace(character));
            if (meaningfulLength == 0)
            {
                return WhitespaceOnly;
            }

            return meaningfulLength < MinimumLength ? TooShort(meaningfulLength) : null;
        }

        private static string TooShort(int meaningfulLength) => $"短すぎます（空白を除いて {meaningfulLength} 文字）";
    }
}
