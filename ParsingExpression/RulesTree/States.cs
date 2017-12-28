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
        public int PrevChildIndex { get; private set; }
        public int SeqIndex { get; set; }
        public Expr CurrentExpr { get; private set; }
        public ParsingState PrevState { get; private set; }
        public bool LastMatchSuccessed { get; private set; }

        private ParsingState(string text, int pos, int prevChildIndex, int SeqIndex, Expr expr, ParsingState prevState, bool lastMatchSuccessed)
        {
            this.Text = text;
            this.Pos = pos;
            this.PrevChildIndex = prevChildIndex;
            this.CurrentExpr = expr;
            this.SeqIndex = SeqIndex;
            this.PrevState = prevState;
            this.LastMatchSuccessed = lastMatchSuccessed;
        }

        public ParsingState ExprMatchSuccess(int dst)
        {
            //System.Console.WriteLine("Success pos {0}, prevInd {1}, expr {2}", this.Pos + dst, this.PrevChildIndex + 1, this.PrevState.CurrentExpr);
            return new ParsingState(this.Text, this.Pos + dst, this.PrevChildIndex + 1, this.SeqIndex, this.PrevState.CurrentExpr, this, true);
            //return new ParsingState(this.Text, this.Pos + dst, this.PrevChildIndex + 1, this.PrevState.CurrentExpr, this, true);
        }

        public ParsingState ExprMatchContinue(Expr expr)
        {
            //System.Console.WriteLine("Continue pos {0}, prevInd {1}, expr {2}", this.Pos, this.PrevChildIndex, expr);
            return new ParsingState(this.Text, this.Pos, this.PrevChildIndex + 1, this.SeqIndex, expr, this, this.LastMatchSuccessed);
        }
         
        public ParsingState ExprMatchSeqContinue(Expr expr)
        {
            //System.Console.WriteLine("Continue pos {0}, prevInd {1}, expr {2}", this.Pos, this.PrevChildIndex, expr);
            return new ParsingState(this.Text, this.Pos, -1, this.SeqIndex, expr, this, this.LastMatchSuccessed);
        }

        public ParsingState ExprMatchFail(int dst)
        {
            //System.Console.WriteLine("Fail pos {0}, prevInd {1}, expr {2}", this.Pos - dst, this.PrevChildIndex + 1, this.PrevState.CurrentExpr);
            return new ParsingState(this.Text, this.Pos - dst, this.PrevState.PrevChildIndex + 1, this.SeqIndex, this.PrevState.CurrentExpr, this.PrevState.PrevState, false);
        }

        public static ParsingState MakeInitial(string text, Expr expr)
        {
            return new ParsingState(text, 0, -1, -1, expr, null, false);
        }
    }

}