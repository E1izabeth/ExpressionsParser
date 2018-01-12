using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using ParsingExpression.Automaton;
using ParsingExpression.RulesTree;
using System.Diagnostics;

namespace ParsingExpression
{
    class Program
    {
        //static void Main()
        //{   
        //    var g = new Grammar(
        //        new Rule("expr", Expr.Alternatives(Expr.RuleCall("sum"), Expr.RuleCall("product"), Expr.RuleCall("braces"), Expr.RuleCall("num"))),
        //        new Rule("num", Expr.Number(Expr.CharsRange('0','9'),1,int.MaxValue)),
        //        new Rule("braces", Expr.Sequence(Expr.Characters("("), Expr.RuleCall("expr") ,Expr.Characters(")"))),
        //        new Rule("parg", Expr.Alternatives(Expr.RuleCall("braces"), Expr.RuleCall("num"))),
        //        new Rule("product", Expr.Sequence(
        //                                Expr.RuleCall("parg"),
        //                                Expr.Number(Expr.Sequence(
        //                                        Expr.RuleCall("productOp"),
        //                                        Expr.RuleCall("parg")
        //                                ), 1, int.MaxValue)
        //                            )),
        //        new Rule("productOp", Expr.Alternatives(Expr.Characters("*"), Expr.Characters("/"))),
        //        new Rule("sarg", Expr.Alternatives(Expr.RuleCall("product"), Expr.RuleCall("braces"), Expr.RuleCall("num"))),
        //        new Rule("sum", Expr.Sequence(
        //                                Expr.RuleCall("sarg"),
        //                                Expr.Number(Expr.Sequence(
        //                                        Expr.RuleCall("sumOp"),
        //                                        Expr.RuleCall("sarg")
        //                                ), 1, int.MaxValue)
        //                            )),
        //        new Rule("sumOp", Expr.Alternatives(Expr.Characters("+"), Expr.Characters("-")))
        //    );

        //    var result = g.Parse("1+4+6*3");

        //    Console.WriteLine(result);
        //}

        static TimeSpan Measure(int count, Action act)
        {
            var sw = new Stopwatch();
            sw.Start();

            act();
            for (int i = 0; i < count; i++)
                act();

            sw.Stop();

            return new TimeSpan(sw.ElapsedTicks / count);
        }

        static void Main()
        {
            const int iterations = 100000;
            var p = new RegexParser();
            string regexp = @"a((b|c|[a-zA-Z])s*n|s*)b";
            //regexp = @"a(b|c|d)b";
            string text = "assssssb";
            p.TryParse(regexp, out Expr expr);


            Console.WriteLine(expr.CollectTree(
                e => e.GetItems(),
                e => e.GetType().Name + ": " + e.ToString()
            ));

            ExprTreeRunner treeRunner = new ExprTreeRunner(expr);
            var ans2 = treeRunner.IsMatch(text);

            var t2 = Measure(iterations, () => treeRunner.IsMatch(text));

            // treeRunner.LastState.SaveStatesLogToFile(@"c:\temp\treematch.dgml");

            //p.TryParse(regexp, out Grammar grammar);
            //bool IsMatched = false;
            //var st = ParsingState.MakeInitial(str, expr);
            //var result = expr.Match(st); //var result = grammar.Match(st);

            //if (result.LastMatchSuccessed == true && result.Pos == result.Text.Length)
            //    IsMatched = true;
            //Console.WriteLine("\tResult {0} for \"{1}\"", IsMatched, str);


            //var fsm = ExprFsmBuilder.BuildFsm(expr, null);
            //fsm.SaveGraphToFile(@"c:\temp\out.dgml");

            //var fsm2 = fsm.RemoveEmptyTransitions().Optimize();
            //fsm2.SaveGraphToFile(@"c:\temp\out2.dgml");

            //var testFsm = new Fsm(4) {
            //    { 0, 3, 'b' },
            //    { 3, 2 },
            //    { 0, 2 },
            //    { 0, 1, 'a' },
            //    { 3, 1, 'a' },
            //    { 1, 3 },
            //    { 2, 1, 'b' }
            //};
            //testFsm.States[3].SetFinal();
            //testFsm.SetInitialState(testFsm.States[0]);
            //testFsm.SaveGraphToFile(@"c:\temp\test.dgml");

            // var fsm3 = fsm2.MakeDFA();
            //fsm3.SaveGraphToFile(@"c:\temp\out3.dgml");

           

            var nfaRunner = Fsms.MakeFsmRunner(expr, FsmRunnerMode.NFA);
            var ans4 = nfaRunner.IsMatch(text);

            var t4 = Measure(iterations, () => nfaRunner.IsMatch(text));

            var dfaRunner = Fsms.MakeFsmRunner(expr, FsmRunnerMode.DFA);
            var ans3 = dfaRunner.IsMatch(text);

            var t3 = Measure(iterations,() => dfaRunner.IsMatch(text));
            //int position = 0;
            //var r = expr.Match(text, ref position);
            var ans1 = false;
            var t1 = Measure(iterations, () => {
                int position = 0;
                ans1 = expr.Match(text, ref position);
            });

            Console.WriteLine(t1 + "      " + ans1);
            Console.WriteLine(t2 + "      " + ans2 + "   " +  "tree");
            Console.WriteLine(t3 + "      " + ans3 + "   " + "DFA");
            Console.WriteLine(t4 + "      " + ans4 + "   " + "NFA");
            return;

            //if (expr.Match(text, ref position) & position == text.Length)
            //    Console.WriteLine("True");
            //else
            //    Console.WriteLine("False");
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

////@"^(?[\w\-]+\.)?
////(?<name>[\w\-]+)
////(\;((?<param>[\w\-]+)
////(\=(?<pvalue>(\"[^\"]*\")|([^\"\;\:\,]*))
////(\,(?<pvalue>(\"[^\"]*\")|([^\"\;\:\,]*)))?)?))*
////:(?<value>.*)";
