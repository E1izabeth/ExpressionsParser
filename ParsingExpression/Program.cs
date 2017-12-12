using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ParsingExpression
{


    class Program
    {
        //enum Quandors
        //{
        //    Qwestion = '?',
        //    Alternative = '|',
        //    Exclamation = '!',
        //    Star = '*',
        //    Plus = '+',
        //    StartInterval = '{',
        //    EndInterval = '}',
        //    Comma = ',',
        //    OpenSeq = '(',
        //    CloseSeq = ')'
        //}


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

        static void Main()
        {
            var p = new RegexParser();
            Expr expr;
            string regexp = @"kt\dk";
            p.TryParse(regexp, out expr);
            string text = "kt:digit:k";
            int position = 0;
            //expr.Match(text, ref position);
            Console.WriteLine("Regular Expression:   " + regexp);
            Console.WriteLine("Text:                 " + text + "\n");
            if (position == text.Length && expr.Match(text, ref position))
                Console.WriteLine("True");
            else
                Console.WriteLine("False");

            Console.ReadKey();
        }

        static void Main3()
        {
            //string exp = "(5+73+a2)*8";
            //var tree = NumExprParser.ExpToTree(exp);

            var generator = new ExprGenerator();
            var exprStrings = Enumerable.Range(0, 100).Select(n => generator.Generate(3, 5).Apply(NumExprStringCollector.Instance)).ToArray();

            foreach (var str in exprStrings)
            {
                Console.WriteLine(str);

                var tree = NumExprParser.ExpToTree(str);

                var s = tree.CollectTree(
                    e => e is NumBinExpr ? new NumExpr[] { (e as NumBinExpr).Left, (e as NumBinExpr).Right } : new NumExpr[0],
                    e => e.Apply(NumExprNodeStringCollector.Instance)
                );
                Console.WriteLine(s);
            }

            //Console.WriteLine(NumExprTreeCollector.CollectTree(tree));
            

            //var expr = Expr.Sequence(
            //    Expr.Characters("k"),
            //    Expr.Number(
            //        Expr.Alternatives(
            //            Expr.Characters("b"),
            //            Expr.Characters("c")
            //        ),
            //        0,
            //        int.MaxValue
            //    ),
            //    Expr.Characters("adabra")
            //);

            //var s = expr.ToString();

            //const string Exp = "k(b|c)*adabra";
            //int i = 0;
            //var e2 = ExpToSeq(Exp, ref (i));
            //var s2 = e2.ToString();

            //Console.WriteLine(s);
            //Console.WriteLine(s2);
            //Console.ReadKey();
        }
    }
}

//        public static SequenceExpr ExpToSeq(string exp, ref int i)
//        {
//            var sequenceList = new List<Expr>();

//            while (i < exp.Length)
//            {


//                if (exp[i] == '(')
//                {
//                    ++i;
//                    sequenceList.Add(ExpToSeq(exp, ref (i)));
//                }
//                else if (exp[i] == '+')
//                {
//                    NumberExpr num = new NumberExpr(sequenceList.Last(), 1, -1);
//                    sequenceList.RemoveAt(sequenceList.Count);
//                    sequenceList.Add(num);
//                    ++i;
//                }
//                else if (exp[i] == '*')
//                {
//                    NumberExpr num = new NumberExpr(sequenceList.Last(), 0, -1);
//                    sequenceList.Remove(sequenceList.Last());
//                    sequenceList.Add(num);
//                    ++i;
//                }
//                else if (exp[i] == '?')
//                {
//                    NumberExpr num = new NumberExpr(sequenceList.Last(), 0, 1);
//                    sequenceList.RemoveAt(sequenceList.Count);
//                    sequenceList.Add(num);
//                    ++i;
//                }

//                else if (exp[i] == '{')
//                {
//                    int min = 0, max = 0;

//                    while (exp[i] != ',')
//                    {
//                        min += min * 10 + Int32.Parse(exp[i].ToString());
//                        ++i;
//                    }
//                    ++i;
//                    while (exp[i] != '}')
//                    {
//                        max += max * 10 + Int32.Parse(exp[i].ToString());
//                        ++i;
//                    }

//                    NumberExpr num = new NumberExpr(sequenceList.Last(), min, max);
//                    sequenceList.RemoveAt(sequenceList.Count);
//                    sequenceList.Add(num);
//                }
//                else if (exp[i] == '|')
//                {
//                    ++i;
//                    AlternativesExpr alternatives = new AlternativesExpr(sequenceList.Last(), ExpToSeq(exp, ref (i)));
//                    sequenceList.Remove(sequenceList.Last());
//                }
//                else if (exp[i] != ')')
//                {
//                    CharsExpr ch = new CharsExpr(exp[i].ToString());
//                    ++i;
//                    sequenceList.Add(ch);
//                }
//                else
//                {
//                    ++i;
//                    break;
//                }

//            }

//            return new SequenceExpr(sequenceList.ToArray());
//        }


//        //public bool IsMatch(string input)
//        //{
//        //if (input == null)
//        //{
//        //    return false;
//        //}

//        //int state = 0;
//        //char ch;
//        //int index = 0;

//        //while (index <= input.Length && state != -1)
//        //{
//        //    if (index == input.Length)
//        //    {
//        //        ch = '\0';
//        //    }
//        //    else
//        //    {
//        //        ch = input[index];
//        //        if (ch == '\0')
//        //        {
//        //            return false;
//        //        }
//        //    }

//        //    switch (state)
//        //    {

//        //        case 0:
//        //            {
//        //                if ()
//        //                {
//        //                    state = 1;
//        //                    break;
//        //                }
//        //                state = -1;
//        //                break;
//        //            }
//        //    }
//        //    index++;
//        //}
//        //if (state == 2)
//        //    return true;
//        //else
//        //    return false;

//        //}
//    }
//}


////@"^(?[\w\-]+\.)?
////(?<name>[\w\-]+)
////(\;((?<param>[\w\-]+)
////(\=(?<pvalue>(\"[^\"]*\")|([^\"\;\:\,]*))
////(\,(?<pvalue>(\"[^\"]*\")|([^\"\;\:\,]*)))?)?))*
////:(?<value>.*)";
