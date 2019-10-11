using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.XmlGraph
{
    public class XmlGraph
    {
        Dictionary<string, XmlGraphNode> _nodes = new Dictionary<string, XmlGraphNode>();

        public XmlGraphNode this[string id] { get { return _nodes[id]; } }

        public XmlGraph()
        {
        }

        public XmlGraphNode CreateNode(string id = null)
        {
            var node = new XmlGraphNode(this, id ?? Guid.NewGuid().ToString());
            _nodes.Add(node.Id, node);
            return node;
        }

        public Dgml.DirectedGraph ToDgml()
        {
            return new Dgml.DirectedGraph() {
                Nodes = _nodes.Values.Select(n => new Dgml.DirectedGraphNode() {
                    Id = n.Id,
                    Label = n.Text,
                }).ToArray(),
                Links = _nodes.Values.SelectMany(
                    n => n.GetLinks()
                          .Select(l => new Dgml.DirectedGraphLink() {
                              Source = l.From.Id,
                              Target = l.To.Id,
                              Label = l.Text,
                              Stroke = "#555555",
                          })
                ).ToArray(),
            };
        }
    }

    public class XmlGraphConnection
    {
        public XmlGraphNode From { get; private set; }
        public XmlGraphNode To { get; private set; }
        public string Text { get; set; }

        public XmlGraphConnection(XmlGraphNode from, XmlGraphNode to)
        {
            this.From = from;
            this.To = to;
        }
    }

    public class XmlGraphNode : IComparable<XmlGraphNode>
    {
        XmlGraph _owner;

        // SortedSet<XmlGraphNode> _targets = new SortedSet<XmlGraphNode>();
        List<XmlGraphConnection> _links = new List<XmlGraphConnection>();

        public string Id { get; private set; }
        public string Text { get; set; }

        public XmlGraphNode(XmlGraph owner, string id)
        {
            _owner = owner;

            this.Id = id;
        }

        public XmlGraphConnection[] GetLinks()
        {
            return _links.ToArray();
        }

        public XmlGraphConnection ConnectTo(XmlGraphNode target)
        {
            if (target._owner != _owner)
                throw new InvalidOperationException();

            //if (!_targets.Add(target))
            //    throw new InvalidOperationException();

            var link = new XmlGraphConnection(this, target);
            _links.Add(link);
            return link;
        }

        public XmlGraphNode CreateNext(string id = null)
        {
            var node = _owner.CreateNode(id);
            this.ConnectTo(node);
            return node;
        }

        public XmlGraphNode CreatePrev(string id = null)
        {
            var node = _owner.CreateNode(id);
            node.ConnectTo(this);
            return node;
        }

        int IComparable<XmlGraphNode>.CompareTo(XmlGraphNode other)
        {
            if (other.Id == null)
                return 1;

            return this.Id.CompareTo(other.Id);
        }
    }
}
