using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.RulesTree
{
    public interface IGrammar
    {
        // IReadOnlyCollection<Rule> Rules { get; }
        // Rule RootRule { get; }

        IParsingResult Parse(string text);
    }

    public interface IParsingResult
    {
        bool Success { get; }

    }

    public class StringTreeNode
    {
        public StringFragment Fragment { get; private set; }
        public ReadOnlyCollection<StringTreeNode> Children { get; private set; }

        public StringTreeNode(StringFragment fragment, params StringTreeNode[] children)
        {
            this.Fragment = fragment;
            this.Children = new ReadOnlyCollection<StringTreeNode>(children);
        }

        public override string ToString()
        {
            return string.Format("Node[{0}]", this.Fragment);
        }
    }

    public struct StringFragment
    {
        private readonly int _start, _length;
        private readonly string _text;

        private string _content;

        public string Content { get { return _text == null ? null : _content ?? (_content = _text.Substring(_start, _length)); } }

        public StringFragment(string text, int start, int length)
        {
            _start = start;
            _length = length;
            _text = text;
            _content = null;
        }

        public override string ToString()
        {
            return this.Content;
        }
    }

}
