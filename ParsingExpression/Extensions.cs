using ParsingExpression.Automaton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ParsingExpression.XmlGraph;
using Graph = ParsingExpression.XmlGraph.XmlGraph;

namespace ParsingExpression
{
    static class Extensions
    {
        public static void SaveTreeToFile(this Expr expr, string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                stream.SetLength(0);
                new XmlSerializer(typeof(Dgml.DirectedGraph)).Serialize(stream, expr.BuildGraph().ToDgml());
            }
        }

        public static void SaveGraphToFile(this IFsm fsm, string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                stream.SetLength(0);
                new XmlSerializer(typeof(Dgml.DirectedGraph)).Serialize(stream, fsm.BuildGraph().ToDgml());
            }
        }

        public static void SaveStatesLogToFile(this RulesTree.ParsingState state, string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                stream.SetLength(0);
                new XmlSerializer(typeof(Dgml.DirectedGraph)).Serialize(stream, state.BuildGraph().ToDgml());
            }
        }

        class ExprGraphBuildingVisitor : IExprVisitor<XmlGraphNode>
        {
            public Graph Graph { get; private set; }

            public ExprGraphBuildingVisitor()
            {
                this.Graph = new Graph();
            }

            public XmlGraphNode AppendNode(Expr expr)
            {
                var node = expr.Apply(this);
                node.Text = expr.GetType().Name + ": " + expr.ToString();
                return node;
            }

            private XmlGraphNode AppendChilds(ItemsExpr expr)
            {
                var node = this.Graph.CreateNode();
                expr.Items.ForEach(e => node.ConnectTo(this.AppendNode(e)));
                return node;
            }

            private XmlGraphNode AppendChild(ItemExpr expr)
            {
                var node = this.Graph.CreateNode();
                node.ConnectTo(this.AppendNode(expr.Child));
                return node;
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitAlternative(AlternativesExpr alternativesExpr)
            {
                return this.AppendChilds(alternativesExpr);
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitCharClass(CharClassExpr charClassExpr)
            {
                return this.Graph.CreateNode();
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitChars(CharsExpr charsExpr)
            {
                return this.Graph.CreateNode();
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitCheck(Check check)
            {
                return this.AppendChild(check);
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitCheckNot(CheckNot checkNot)
            {
                return this.AppendChild(checkNot);
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitNum(NumberExpr numberExpr)
            {
                return this.AppendChild(numberExpr);
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitRuleCall(RuleCallExpr ruleCallExpr)
            {
                throw new NotImplementedException();
            }

            XmlGraphNode IExprVisitor<XmlGraphNode>.VisitSequence(SequenceExpr sequenceExpr)
            {
                return this.AppendChilds(sequenceExpr);
            }
        }

        private static Graph BuildGraph(this Expr expr)
        {
            var visitor = new ExprGraphBuildingVisitor();
            visitor.AppendNode(expr);
            return visitor.Graph;
        }

        private static Graph BuildGraph(this RulesTree.ParsingState state)
        {
            var g = new Graph();

            var nodes = new Dictionary<RulesTree.ParsingState, XmlGraphNode>();
            var states = new Stack<RulesTree.ParsingState>();

            for (var s = state; s != null; s = s.PrevState)
                states.Push(s);

            var index = 0;
            while (states.Count > 0)
            {
                var s = states.Pop();
                var node = g.CreateNode(index.ToString());
                node.Text = new
                {
                    index,
                    expr = s.CurrentExpr == null ? "<NULL>" : (s.CurrentExpr.GetType().Name + ": " + s.CurrentExpr.ToString()),
                    s.InvocationCount,
                    s.LastMatchSuccessed,
                    s.Pos,
                    currChar = s.Pos < s.Text.Length ? s.Text[s.Pos].ToString() : "<OOT>",
                    prev = s.PrevState == null ? "<NULL>" : nodes[s.PrevState].Id,
                    parent = s.Parent == null ? "<NULL>" : nodes[s.Parent].Id,
                }.ToString();

                nodes.Add(s, node);

                if (s.PrevState != null)
                    node.ConnectTo(nodes[s.PrevState]);

                if (s.Parent != null)
                    node.ConnectTo(nodes[s.Parent]);

                index++;
            }

            return g;
        }
    }
}
