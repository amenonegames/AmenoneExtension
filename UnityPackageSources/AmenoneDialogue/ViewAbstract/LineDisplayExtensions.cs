using System.Collections.Generic;
using LitMotion;

namespace ViewAbstract
{
    public interface ILineViewContextProcessor
    {
        LineViewContext Process(LineViewContext context);
    }

    // 選択肢を出す前に、文と、出すかどうかを替える。index は Yarn の選択肢の番号のまま保つ
    public interface IOptionViewContextProcessor
    {
        OptionViewContext Process(OptionViewContext context);
    }

    // ライブラリの外から、台詞のタグの名前・文字の演出・表示前の加工を足す口。
    // 子のスコープの登録は親のスコープから見えないので、Root に 1 つ置き、外の組み立てが組み立ての後に足す
    public sealed class LineDisplayExtensions
    {
        private readonly HashSet<string> _markupNames = new();
        private readonly List<IMarkupCharShowAnimator> _charShowAnimators = new();
        private readonly List<ILineViewContextProcessor> _contextProcessors = new();
        private readonly List<IOptionViewContextProcessor> _optionContextProcessors = new();

        public IReadOnlyCollection<string> MarkupNames => _markupNames;
        public IReadOnlyList<IMarkupCharShowAnimator> CharShowAnimators => _charShowAnimators;
        public IReadOnlyList<ILineViewContextProcessor> ContextProcessors => _contextProcessors;
        // 無い時は LitMotion の既定のスケジューラーで動かす
        public IMotionScheduler MotionScheduler { get; private set; }
        // 無い時は、文字の速さの値を 1 文字の長さと次の文字までの間隔の両方に使う
        public LineCharTiming? CharTiming { get; private set; }

        public void UseMotionScheduler(IMotionScheduler scheduler) => MotionScheduler = scheduler;

        public void UseCharTiming(LineCharTiming timing) => CharTiming = timing;

        public MotionHandle Run(MotionSequenceBuilder sequence)
        {
            var scheduler = MotionScheduler;
            return scheduler == null ? sequence.Run() : sequence.Run(builder => builder.WithScheduler(scheduler));
        }

        public void AddMarkupName(string markupName) => _markupNames.Add(markupName);

        public void AddCharShowAnimator(IMarkupCharShowAnimator animator)
        {
            _charShowAnimators.Add(animator);
            AddMarkupName(animator.MarkupName);
        }

        public void AddContextProcessor(ILineViewContextProcessor processor) => _contextProcessors.Add(processor);

        public void AddOptionContextProcessor(IOptionViewContextProcessor processor) => _optionContextProcessors.Add(processor);

        public LineViewContext Process(LineViewContext context)
        {
            foreach (var processor in _contextProcessors) context = processor.Process(context);
            return context;
        }

        public OptionViewContext Process(OptionViewContext context)
        {
            foreach (var processor in _optionContextProcessors) context = processor.Process(context);
            return context;
        }
    }

    // 文字の速さ（既定 1）を掛ける前の、1 文字が出きるまでの長さと、次の文字が出始めるまでの間隔
    public readonly record struct LineCharTiming(float DurationSeconds, float IntervalSeconds);
}
