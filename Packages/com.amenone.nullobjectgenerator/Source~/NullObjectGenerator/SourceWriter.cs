using System.Text;

namespace Amenone.NullObjectGenerator
{
    internal sealed class SourceWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private int _depth;

        public void Line(string text) => _builder.Append(' ', _depth * 4).Append(text).Append('\n');

        public void BlankLine() => _builder.Append('\n');

        public void Open()
        {
            Line("{");
            _depth++;
        }

        public void Close()
        {
            _depth--;
            Line("}");
        }

        public void Indent() => _depth++;

        public void Unindent() => _depth--;

        public override string ToString() => _builder.ToString();
    }
}
