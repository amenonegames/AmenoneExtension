using System.Threading;
using Cysharp.Threading.Tasks;

namespace Presenter.Adapters
{
    public struct LocalizeLine
    {
        public string FullText { get; init; }
        public string[] Tags { get; init; }
        public LocalizeLine(string FullText, string[] tags)
        {
            this.FullText = FullText;
            Tags = tags;
        }
    }

    public interface ILineProvider
    {
        UniTask<LocalizeLine> GetDialogueLine(string lineId, string[] substitutions , CancellationToken token);
        UniTask RefreshLocale(CancellationToken token , string locale);
    }
}