namespace ParsingExpression.RulesTree
{
    public class Rule
    {
        public string Name { get; private set; }
        public Expr Expr { get; private set; }

        public Rule(string name, Expr expr)
        {
            this.Name = name;
            this.Expr = expr;
        }

        //TODO: Rule.Match
        public ParsingState Match(ParsingState st)
        {
            return st;
        }
    }

    public class ParsingState
    {
        public string Text { get; private set; }
        public int Pos { get; private set; }
        //public Rule Rule { get; private set; }
        public ParsingState PrevState { get; private set; }
        public bool LastMatchSuccessed { get; private set; }

        private ParsingState(string text, int pos, ParsingState prevState, bool lastMatchSuccessed)
        {
            this.Text = text;
            this.Pos = pos;
            this.PrevState = prevState;
            this.LastMatchSuccessed = lastMatchSuccessed;
        }

        public ParsingState ExprMatchSuccess(int dst)
        {
            return new ParsingState(this.Text, this.Pos + dst, this, true);
        }

        public ParsingState ExprMatchFail(int dst)
        {
            return new ParsingState(this.Text, this.Pos - dst, this.PrevState, false);
        }

        public static ParsingState MakeInitial(string text)
        {
            return new ParsingState(text, 0, null, false);
        }
    }

}