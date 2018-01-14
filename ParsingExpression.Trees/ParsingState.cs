using System.Linq;
using System.Collections.Generic;

namespace ParsingExpression.Trees
{
    public class ParsingState
    {
        public string Text { get; private set; }
        public int Pos { get; private set; }
        public ParsingState PrevState { get; private set; }
        public ParsingState Parent { get; private set; }
        public Grammar Grammar { get; private set; }
        public Rule Rule { get; private set; }

        public IReadOnlyCollection<StringTreeNode> CurrChildren { get;private set; } 

        private ParsingState(string text, ParsingState prevState, int pos, ParsingState parent, Grammar g, Rule rule, IReadOnlyCollection<StringTreeNode> currChildren)
        {
            this.Text = text;
            this.Pos = pos;
            this.PrevState = prevState;
            this.Parent = parent;
            this.Grammar = g;
            this.Rule = rule;
            this.CurrChildren = currChildren;
        }

        public static ParsingState MakeInitial(string text, Grammar g)
        {
            return new ParsingState(text, null, 0, null, g, g.RootRule, new StringTreeNode[0]);
        }
        
        public ParsingState EnterRule(Rule rule)
        {
            return new ParsingState(this.Text, this, this.Pos, this, this.Grammar, rule, new StringTreeNode[0]);
        }

        public ParsingState ExitRule()
        {
            var node = new StringTreeNode(this.Rule, new StringFragment(this.Text, this.Parent.Pos, this.Pos - this.Parent.Pos), this.CurrChildren.ToArray());
            var parentChildren = this.Parent.CurrChildren.Concat(new[] { node }).ToArray();
            return new ParsingState(this.Text, this, this.Pos, this.Parent.Parent, this.Grammar, this.Parent.Rule, parentChildren);
        }

        public ParsingState Advance(int advance)
        {
            return new ParsingState(this.Text, this, this.Pos + advance, this.Parent, this.Grammar, this.Rule, this.CurrChildren);
        }
    }
}