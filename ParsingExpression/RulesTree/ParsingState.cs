namespace ParsingExpression.RulesTree
{
    public class ParsingState
    {
        public string Text { get; private set; }
        public int Pos { get; private set; }
        public int InvocationCount { get; set; }
        public Expr CurrentExpr { get; private set; }
        public bool? LastMatchSuccessed { get; private set; }
        public ParsingState PrevState { get; private set; }
        public ParsingState Parent { get; private set; }

        private ParsingState(string text, ParsingState prevState, int pos, int invocationCount, Expr currentExpr, bool? lastMatchSuccessed, ParsingState parent)
        {
            this.Text = text;
            this.Pos = pos;
            this.InvocationCount = invocationCount;
            this.CurrentExpr = currentExpr;
            this.LastMatchSuccessed = lastMatchSuccessed;
            this.PrevState = prevState;
            this.Parent = parent;
        }

        public static ParsingState MakeInitial(string text, Expr expr)
        {
            return new ParsingState(text, null, 0, 0, Expr.Sequence(expr), null, null);
        }

        public ParsingState EnterChild(Expr expr)
        {
            return new ParsingState(this.Text, this, this.Pos, 0, expr, null, this);
        }

        public ParsingState ExitChild(bool success, int advance = 0)
        {
            return new ParsingState(this.Text, this, this.Pos + advance, this.Parent.InvocationCount + 1, this.Parent.CurrentExpr, success, this.Parent.Parent);
        }
    }

}