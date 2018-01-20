using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    static class Program
    {
        static IEnumerable<IParsingTreeNode<T>> GetChildren<T>(this IParsingTreeNode<T> node)
        {
            var child = node.Child;
            while (child != null)
            {
                yield return child;
                child = child.NextChild;
            }
        }

        static void Main()
        {
            var g = new Grammar(
                new Rule("expr", Expr.Alternatives(Expr.RuleCall("sum"), Expr.RuleCall("product"), Expr.RuleCall("braces"), Expr.RuleCall("num"))),
                //new Rule("spaces", Expr.Number(Expr.WhitespaceChar(), 0, int.MaxValue)),
                // new Rule("num", Expr.Sequence(Expr.RuleCall("spaces"), Expr.Number(Expr.CharsRange('0', '9'), 1, int.MaxValue), Expr.RuleCall("spaces"))),
                new Rule("num", Expr.Number(Expr.CharsRange('0', '9'), 1, int.MaxValue)),
                new Rule("braces", Expr.Sequence(Expr.Characters("("), Expr.RuleCall("expr"), Expr.Characters(")"))),
                new Rule("parg", Expr.Alternatives(Expr.RuleCall("braces"), Expr.RuleCall("num"))),
                new Rule("product", Expr.Sequence(
                                        Expr.RuleCall("parg"),
                                        Expr.Number(Expr.Sequence(
                                                Expr.RuleCall("productOp"),
                                                Expr.RuleCall("parg")
                                        ), 1, int.MaxValue)
                                    )),
                new Rule("productOp", Expr.Alternatives(Expr.Characters("*"), Expr.Characters("/"))),
                new Rule("sarg", Expr.Alternatives(Expr.RuleCall("product"), Expr.RuleCall("braces"), Expr.RuleCall("num"))),
                new Rule("sum", Expr.Sequence(
                                        Expr.RuleCall("sarg"),
                                        Expr.Number(Expr.Sequence(
                                                Expr.RuleCall("sumOp"),
                                                Expr.RuleCall("sarg")
                                        ), 1, int.MaxValue)
                                    )),
                new Rule("sumOp", Expr.Alternatives(Expr.Characters("+"), Expr.Characters("-")))
            );

            var text = "4341+54+6*3";
            //var result = g.Parse(text);

            var runner = new GrammarRunner(g);
            var result = runner.Match(text);

            var str = result.CollectTree(n => n.GetChildren(), n => n.Info.ToString());
            Console.WriteLine(str);
        }



        static void Main2(string[] args)
        {
            var expr = Expr.Sequence(
                Expr.Characters("k"),
                Expr.Number(
                    Expr.Alternatives(
                        Expr.Characters("b"),
                        Expr.Characters("c")
                    ),
                    0,
                    int.MaxValue
                ),
                Expr.Characters("ad")
            );

            //var initState = ParsingState.MakeInitial("kbbad", expr);
            //var r = expr.Match(initState);
            //Console.WriteLine(r == null? false : true);
        }
    }
}
