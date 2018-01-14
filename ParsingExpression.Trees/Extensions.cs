using ParsingExpression.XmlGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Graph = ParsingExpression.XmlGraph.XmlGraph;

namespace ParsingExpression.Trees
{
    public static class Extensions
    {
        public static void SaveStatesLogToFile(this ParsingState state, string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                stream.SetLength(0);
                new XmlSerializer(typeof(Dgml.DirectedGraph)).Serialize(stream, state.BuildGraph().ToDgml());
            }
        }

        private static Graph BuildGraph(this ParsingState state)
        {
            var g = new Graph();

            var nodes = new Dictionary<ParsingState, XmlGraphNode>();
            var states = new Stack<ParsingState>();

            for (var s = state; s != null; s = s.PrevState)
                states.Push(s);

            var index = 0;
            while (states.Count > 0)
            {
                var s = states.Pop();
                var node = g.CreateNode(index.ToString());
                node.Text = new {
                    index,
                    // expr = s.CurrentExpr == null ? "<NULL>" : (s.CurrentExpr.GetType().Name + ": " + s.CurrentExpr.ToString()),
                    // s.InvocationCount,
                    // s.LastMatchSuccessed,
                    ruleName = s.Rule.Name,
                    capturedText = s.PrevState == null ? string.Empty : s.Text.Substring(s.PrevState.Pos, s.Pos - s.PrevState.Pos),
                    currChildren = "[" + string.Join(", ",  s.CurrChildren.Select(cn => cn.Rule.Name)) +"]",
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
