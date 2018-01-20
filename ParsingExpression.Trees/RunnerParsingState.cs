using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    public class ParsingTreeNodeInfo
    {
        public Rule Rule { get; private set; }
        public Expr Expr { get; private set; }
        public int Position { get; private set; }

        public ParsingTreeNodeInfo(Rule rule, Expr expr, int position)
        {
            this.Rule = rule;
            this.Expr = expr;
            this.Position = position;
        }

        public override string ToString()
        {
            return string.Format("@{0} {1}: {2}", this.Position, this.Rule.Name, this.Expr);
        }
    }

    public class RunnerParsingState
    {
        public string Text { get; private set; }
        public Grammar Grammar { get; private set; }

        public int Pos { get; private set; }
        public int InvocationCount { get; set; }
        public Expr CurrentExpr { get; private set; }
        public bool? LastMatchSuccessed { get; private set; }
        public RunnerParsingState PrevState { get; private set; }
        public RunnerParsingState Parent { get; private set; }

        readonly IParsingTreeActiveNode<ParsingTreeNodeInfo> _node;

        private RunnerParsingState(string text, RunnerParsingState prevState,
                                   int pos, int invocationCount,
                                   Expr currentExpr, bool? lastMatchSuccessed,
                                   RunnerParsingState parent, Grammar grammar,
                                   IParsingTreeActiveNode<ParsingTreeNodeInfo> node)
        {
            this.Text = text;
            this.Pos = pos;
            this.InvocationCount = invocationCount;
            this.CurrentExpr = currentExpr;
            this.LastMatchSuccessed = lastMatchSuccessed;
            this.PrevState = prevState;
            this.Parent = parent;

            this.Grammar = grammar;
            _node = node;
        }

        public static RunnerParsingState MakeInitial(string text, Grammar grammar)
        {
            var expr = Expr.Sequence(grammar.RootRule.Expr);
            var node = ParsingTree<ParsingTreeNodeInfo>.CreateNew(new ParsingTreeNodeInfo(grammar.RootRule, expr, 0));
            return new RunnerParsingState(text, null, 0, 0, expr, null, null, grammar, node);
        }

        public RunnerParsingState EnterRule(Rule rule)
        {
            var newNode = _node.CreateChild(new ParsingTreeNodeInfo(rule, rule.Expr, this.Pos));
            return new RunnerParsingState(this.Text, this, this.Pos, 0, rule.Expr, null, this, this.Grammar, newNode);
        }

        public RunnerParsingState EnterChild(Expr expr)
        {
            var newNode = _node.CreateChild(new ParsingTreeNodeInfo(_node.Info.Rule, expr, this.Pos));
            return new RunnerParsingState(this.Text, this, this.Pos, 0, expr, null, this, this.Grammar, newNode);
        }

        public RunnerParsingState ExitChild(bool success, int advance = 0)
        {
            var newNode = _node.ExitChild(success);
            return new RunnerParsingState(this.Text, this, this.Pos + advance, this.Parent.InvocationCount + 1, this.Parent.CurrentExpr, success, this.Parent.Parent, this.Grammar, newNode);
        }

        public IParsingTreeNode<ParsingTreeNodeInfo> GetTree()
        {
            return _node.Complete();
        }
    }

}
