using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using ParsingExpression.Automaton;

namespace ParsingExpression
{
    class Program
    {
        static void Main()
        {
            // regex: ababab
            // text: ab

            var p = new RegexParser();
            string regexp = @"a&(b)(d)";
            // string regexp = @"a*";
            //string regexp = @"(b|(bxy)|(bxy)|c)*n(k|l)+b?";
            p.TryParse(regexp, out Expr expr);

            Console.WriteLine(expr.CollectTree(
                e => e.GetItems(),
                e => e.GetType().Name + ": " +  e.ToString()
            ));

            var fsm = ExprFsmBuilder.BuildFsm(expr, null);
            fsm.SaveGraphToFile(@"c:\temp\out.dgml");

            var fsm2 = fsm.RemoveEmptyTransitions().Optimize();
            fsm2.SaveGraphToFile(@"c:\temp\out2.dgml");

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

            var fsm3 = fsm2.MakeDFA();
            fsm3.SaveGraphToFile(@"c:\temp\out3.dgml");

            string text = "aaaaaaaaaaaaaaaaaaaaaab";
            int position = 0;
            var r = expr.Match(text, ref position);
            Console.WriteLine(r);
            Console.WriteLine("Regular Expression:   " + regexp);
            Console.WriteLine("Text:                 " + text + "\n");
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
