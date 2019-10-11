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
    class Program2
    {
        static IFsm MakeMDfa(Expr expr, int depth, bool debug)
        {
            var fsm = ExprFsmBuilder.BuildFsm(expr, e => MakeMDfa(e, depth + 1, debug));
            var fsm2 = fsm.RemoveEmptyTransitions();
            var fsm3 = fsm2.MakeDFA();
            var fsm4 = fsm3.MinimizeDFA();

            if (debug)
            {
                var fname = "regex" + depth;
                expr.SaveTreeToFile(fname + "_0_tree.dgml");
                fsm.SaveGraphToFile(fname + "_1_nfa.dgml");
                fsm2.SaveGraphToFile(fname + "_2_nfa_we.dgml");
                fsm3.SaveGraphToFile(fname + "_3_dfa.dgml");
                fsm4.SaveGraphToFile(fname + "_4_minimized.dgml");
            }

            return fsm4;
        }

        static IFsmRunner MakeMDfaRunner(IFsm fsm)
        {
            return new DfaFsmRunner(fsm, MakeMDfaRunner);
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine("regex.exe <regex> <text> [-debug]");
            }
            else
            {
                string regexp = args[0]; // @"b(ab)*bbc?";
                string text = args[1]; // "bbbc";

                var p = new RegexParser();
                if (p.TryParse(regexp, out Expr expr))
                {

                    var fsm = MakeMDfa(expr, 1, args.Length > 2 && args[2] == "-debug");
                    var runner = MakeMDfaRunner(fsm);

                    if (runner.IsMatch(text))
                    {
                        Console.WriteLine("OK");
                        Environment.ExitCode = 0;
                    }
                    else
                    {
                        Console.WriteLine("Fail");
                        Environment.ExitCode = -1;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid regex");
                    Environment.ExitCode = -2;
                }
            }
        }
    }

    class Program
    {
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

        static void Main2()
        {
            const int iterations = 100000;
            var p = new RegexParser();

            string regexp = @"b(ab)*bbc?";
            string text = "bbbc";

            p.TryParse(regexp, out Expr expr);
            Console.WriteLine(expr.CollectTree(
                e => e.GetItems(),
                e => e.GetType().Name + ": " + e.ToString()
            ));

            //var fsm = ExprFsmBuilder.BuildFsm(expr, null);
            //fsm.SaveGraphToFile(@"c:\temp\outFSM.dgml");
            //var fsmWithoutEmpty = fsm.RemoveEmptyTransitions().Optimize();
            //var fsmWithoutEmptyAndOptimized = fsm.RemoveEmptyTransitions().Optimize();
            //fsmWithoutEmpty.SaveGraphToFile(@"c:\temp\outFSMwithoutEmptyAndOptimized.dgml");
            //fsmWithoutEmptyAndOptimized.SaveGraphToFile(@"c:\temp\outFSMwithoutEmpty.dgml");
            //var fsmDFA = fsmWithoutEmptyAndOptimized.MakeDFA();
            //fsmDFA.SaveGraphToFile(@"c:\temp\outDFA.dgml");
            //var m2 = fsmDFA.MinimizeDFA();
            //m2.SaveGraphToFile(@"c:\temp\outDFA2.dgml");

            var nfaRunner = Fsms.MakeFsmRunner(expr, FsmRunnerMode.NFA);
            var nfaAns = nfaRunner.IsMatch(text);
            var nfaT = Measure(iterations, () => nfaRunner.IsMatch(text));

            var dfaRunner = Fsms.MakeFsmRunner(expr, FsmRunnerMode.DFA);
            var dfaAns = dfaRunner.IsMatch(text);
            var dfaT = Measure(iterations, () => dfaRunner.IsMatch(text));

            var mdfaRunner = Fsms.MakeFsmRunner(expr, FsmRunnerMode.DFA);
            var mdfaAns = mdfaRunner.IsMatch(text);
            var mdfaT = Measure(iterations, () => mdfaRunner.IsMatch(text));

            var exprAns = false;
            var exprT = Measure(iterations, () =>
            {
                int position = 0;
                exprAns = expr.Match(text, ref position) && position == text.Length;
            });

            ExprTreeRunner treeRunner = new ExprTreeRunner(expr);
            var treeAns = treeRunner.IsMatch(text);
            var treeT = Measure(iterations, () => treeRunner.IsMatch(text));
            treeRunner.LastState.SaveStatesLogToFile(@"c:\temp\treematch.dgml");

            Console.WriteLine("\tResult {0} for \"{1}\"", treeAns, text);
            Console.WriteLine(exprT + "\t" + exprAns + "\t recursive tree");  //just parsed expr
            Console.WriteLine(treeT + "\t" + treeAns + "\t stackless tree");
            Console.WriteLine(nfaT + "\t" + nfaAns + "\t NFA");
            Console.WriteLine(dfaT + "\t" + dfaAns + "\t DFA");
            Console.WriteLine(dfaT + "\t" + mdfaAns + "\t MDFA");
            return;
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