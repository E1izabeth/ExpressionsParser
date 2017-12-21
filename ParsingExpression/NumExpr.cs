using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParsingExpression
{
    public interface INumExprVisitor<T>
    {
        T VisitConst(NumConstExpr numConstExpr);
        T VisitVar(NumVarExpr numVarExpr);
        T VisitBinExpr(NumBinExpr numBinExpr);
    }

    public abstract class NumExpr
    {
        public T Apply<T>(INumExprVisitor <T> visitor)
        {
            return this.ApplyImpl(visitor);
        }

        protected abstract T ApplyImpl<T>(INumExprVisitor<T> visitor);

        public static NumExpr Const(int value) { return new NumConstExpr(value); }
        public static NumExpr BinOp(NumExpr left, NumExpr right, NumOp op) { return new NumBinExpr(left, right, op); }
        public static NumExpr Variable(string name) { return new NumVarExpr(name); }
    }

    public enum NumOp
    {
        Sum,
        Sub,
        Mul,
        Div
    }

    public class NumConstExpr : NumExpr
    {
        public int Value { get; set; }

        public NumConstExpr(int value)
        {
            this.Value = value;
        }
        
        public NumConstExpr()
        {
        }

        protected override T ApplyImpl<T>(INumExprVisitor<T> visitor)
        {
            return visitor.VisitConst(this);
        }
    }

    public class NumVarExpr : NumExpr
    {
        public string Name { get; set; }

        public NumVarExpr(string name)
        {
            this.Name = name;
        }

        public NumVarExpr()
        {
        }

        protected override T ApplyImpl<T>(INumExprVisitor<T> visitor)
        {
            return visitor.VisitVar(this);
        }
    }

    public class NumBinExpr : NumExpr
    {
        public NumExpr Left { get; set; }
        public NumExpr Right { get; set; }
        public NumOp Kind { get; set; }
        //public NumBinExpr Rel { get; set; }


        public NumBinExpr(NumExpr left, NumExpr right, NumOp kind)
        {
            this.Left = left;
            this.Right = right;
            this.Kind = kind;
        }

        public NumBinExpr()
        {
        }

        protected override T ApplyImpl<T>(INumExprVisitor<T> visitor)
        {
            return visitor.VisitBinExpr(this);
        }
    }

    public static class NumExprParser
    {
        static Dictionary<char, NumOp> _opByChar = new Dictionary<char, NumOp>() {
            { '+', NumOp.Sum },
            { '-', NumOp.Sub },
            { '/', NumOp.Div },
            { '*', NumOp.Mul },
        };

        static Dictionary<char, int> _priorityByChar = new Dictionary<char, int>() {
            { '+', 1 },
            { '-', 1 },
            { '/', 2 },
            { '*', 2 },
        };


        public static bool BracketsCanBeDeleted(this string exp)
        {
            exp = exp.Remove(exp.Length - 1);
            exp = exp.Substring(1);
            var brackets = new Regex("[)(]").Matches(exp);
            int amount = 0;
            foreach (var br in brackets)
            {
                if (br.ToString() == ")")
                {
                    --amount;
                }
                else if (br.ToString() == "(")
                {
                    ++amount;
                }
                if (amount < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static string SpaceDelite(this string exp)
        {
            exp = exp.Replace(" ", "");
            return exp;
        }
        public static NumExpr ExpToTree(string exp)
        {
            exp = exp.SpaceDelite();
            NumBinExpr tree = new NumBinExpr();
            int BracketCounter = 0, minPrior = 20, indexMinPrior = -1;

            for (int i = 0; i < exp.Length; ++i)
            {

                if (exp[0] == '(' && exp[exp.Length - 1] == ')' && exp.BracketsCanBeDeleted())
                {
                    exp = exp.Remove(exp.Length - 1);
                    exp = exp.Substring(1);
                }
                if (exp[i] == '(')
                {
                    ++BracketCounter;
                }
                else if (exp[i] == ')')
                {
                    --BracketCounter;
                }
                else
                {
                    if (BracketCounter == 0 && !Char.IsLetterOrDigit(exp[i]))
                    {
                        int tmpOpPrior = _priorityByChar[exp[i]];
                        if (minPrior >= _priorityByChar[exp[i]])
                        {
                            minPrior = tmpOpPrior;
                            indexMinPrior = i;
                        }
                    }
                }
            }
            if (indexMinPrior != -1)
            {
                tree.Kind = _opByChar[exp[indexMinPrior]];
                tree.Left = ExpToTree(exp.Remove(indexMinPrior));
                tree.Right = ExpToTree(exp.Substring(indexMinPrior + 1));
            }
            else
            {
                for (int i = 0; i < exp.Length; i++)
                {
                    if (!Char.IsDigit(exp[i]))
                    {
                        return new NumVarExpr(exp);
                    }
                }
                return new NumConstExpr(Int32.Parse(exp));
            }
            return tree;
        }
    }

    class NumExprTreeCollector : INumExprVisitor<object>
    {
        readonly StringBuilder _sb = new StringBuilder();
        readonly string _indent = "  ";
        int _depth = 0;

        private NumExprTreeCollector()
        {

        }

        private void AppendLine(string str)
        {
            for (int i = 0; i < _depth; i++)
                _sb.Append(_indent);

            _sb.AppendLine(str);
        }
        
        private void Push()
        {
            _depth++;
        }

        private void Pop()
        {
            _depth--;
        }

        #region INumExprVisitor<object> implementation

        object INumExprVisitor<object>.VisitConst(NumConstExpr numConstExpr)
        {
            this.AppendLine(numConstExpr.Value.ToString());
            return null;
        }

        object INumExprVisitor<object>.VisitVar(NumVarExpr numVarExpr)
        {
            this.AppendLine(numVarExpr.Name);
            return null;
        }

        object INumExprVisitor<object>.VisitBinExpr(NumBinExpr numBinExpr)
        {
            this.AppendLine(numBinExpr.Kind.ToString());
            this.Push();
            numBinExpr.Left.Apply(this);
            numBinExpr.Right.Apply(this);
            this.Pop();
            return null;
        }

        #endregion

        public string Complete()
        {
            return _sb.ToString();
        }

        public static string CollectTree(NumExpr expr)
        {
            var collector = new NumExprTreeCollector();
            expr.Apply(collector);
            return collector.Complete();
        }
    }

    class NumExprNodeStringCollector : INumExprVisitor<string>
    {
        public static readonly NumExprNodeStringCollector Instance = new NumExprNodeStringCollector();

        string INumExprVisitor<string>.VisitBinExpr(NumBinExpr numBinExpr)
        {
            return numBinExpr.Kind.ToString();
        }

        string INumExprVisitor<string>.VisitConst(NumConstExpr numConstExpr)
        {
            return numConstExpr.Value.ToString();
        }

        string INumExprVisitor<string>.VisitVar(NumVarExpr numVarExpr)
        {
            return numVarExpr.Name;
        }
    }

    class NumExprStringCollector : INumExprVisitor<string>
    {
        public static readonly NumExprStringCollector Instance = new NumExprStringCollector();

        static readonly Dictionary<NumOp, int> _priorityByOp = new Dictionary<NumOp, int>()
        {
                { NumOp.Sum, 1 },
                { NumOp.Sub, 1 },
                { NumOp.Mul, 2 },
                { NumOp.Div, 2 },
            };

        static readonly Dictionary<NumOp, string> _strByOp = new Dictionary<NumOp, string>()
        {
                { NumOp.Sum, "+" },
                { NumOp.Sub, "-" },
                { NumOp.Div, "/" },
                { NumOp.Mul, "*" },
            };

        string GetChildStr(NumBinExpr curr, NumExpr child)
        {
            var bin = child as NumBinExpr;
            string result;

            if (bin != null && _priorityByOp[curr.Kind] > _priorityByOp[bin.Kind]) result = $"({child.Apply(this)})";
            else result = child.Apply(this);

            return result;
        }

        string INumExprVisitor<string>.VisitBinExpr(NumBinExpr curr)
        {
            return string.Join(" ", this.GetChildStr(curr, curr.Left), _strByOp[curr.Kind], this.GetChildStr(curr, curr.Right));
        }

        string INumExprVisitor<string>.VisitConst(NumConstExpr numConstExpr)
        {
            return numConstExpr.Value.ToString();
        }

        string INumExprVisitor<string>.VisitVar(NumVarExpr numVarExpr)
        {
            return numVarExpr.Name;
        }
    }
}
