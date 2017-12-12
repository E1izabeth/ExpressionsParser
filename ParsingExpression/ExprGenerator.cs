using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression
{
    class ExprGenerator
    {
        string[] _varNames = new[] { "x", "y", "a", "b", "c" };
        Random _rnd = new Random();

        public NumExpr Generate(int depthFrom, int maxDepth)
        {
            return this.GenerateImpl(depthFrom, maxDepth, 0);
        }

        NumExpr GenerateImpl(int depthFrom, int maxDepth, int currDepth)
        {
            NumExpr expr;

            var caseNum = _rnd.Next(1, 4);
            while ((currDepth < depthFrom && caseNum < 3)
                || (currDepth >= maxDepth && caseNum > 2))
                caseNum = _rnd.Next(1, 4);

            switch (caseNum)
            {
                case 1: expr = NumExpr.Const(_rnd.Next(1, 100)); break;
                case 2: expr = NumExpr.Variable(_varNames[_rnd.Next(0, _varNames.Length)] + _rnd.Next(0, 10)); break;
                case 3:
                    {
                        expr = NumExpr.BinOp(
                          this.GenerateImpl(depthFrom, maxDepth, currDepth + 1),
                          this.GenerateImpl(depthFrom, maxDepth, currDepth + 1),
                          (NumOp)_rnd.Next(0, 3)
                        );
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return expr;
        }
    }
}
