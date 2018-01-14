using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
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

        public ParsingState TryParse(ParsingState st)
        {
            var enterState = st.EnterRule(this);

            var exitState = this.Expr.Match(enterState);

            return exitState == null ? null : exitState.ExitRule();
        }
    }
}
