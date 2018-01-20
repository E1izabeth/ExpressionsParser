using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    public interface IParsingTreeNode<T>
    {
        T Info { get; }

        IParsingTreeNode<T> Child { get; }
        IParsingTreeNode<T> NextChild { get; }
    }

    public interface IParsingTreeActiveNode<T>
    {
        T Info { get; }

        bool HasParent { get; }

        IParsingTreeActiveNode<T> CreateChild(T infoition);
        IParsingTreeActiveNode<T> ExitChild(bool keep);

        IParsingTreeNode<T> Complete();
    }

    public class ParsingTree<T>
    {
        public static IParsingTreeActiveNode<T> CreateNew(T info)
        {
            return new ActiveNode(info, null, null);
        }

        abstract class Node
        {
            public T Info { get; private set; }

            public Node(T info)
            {
                this.Info = info;
            }
        }

        class ActiveParent : Node
        {
            public ActiveParent Parent { get; private set; }
            public ActiveChild LastChild { get; private set; }

            public ActiveParent(T info, ActiveParent parent, ActiveChild lastChild)
                : base(info)
            {
                this.Parent = parent;
                this.LastChild = lastChild;
            }
        }

        class ActiveNode : Node, IParsingTreeActiveNode<T>
        {
            public bool HasParent { get { return this.Parent != null; } }

            public ActiveParent Parent { get; private set; }
            public ActiveChild LastChild { get; private set; }

            public ActiveNode(T info, ActiveParent parent, ActiveChild lastChild)
                : base(info)
            {
                this.Parent = parent;
                this.LastChild = lastChild;
            }

            public ActiveParent RecreateAsActiveParent()
            {
                return new ActiveParent(this.Info, this.Parent, this.LastChild);
            }

            public IParsingTreeActiveNode<T> CreateChild(T info)
            {
                return new ActiveNode(info, this.RecreateAsActiveParent(), null);
            }

            public IParsingTreeActiveNode<T> ExitChild(bool keep)
            {
                if (keep)
                {
                    var recreatedCurrent = new ActiveChild(this.Info, this.RecreateChildrenAsFinal(), this.Parent.LastChild);
                    return new ActiveNode(this.Parent.Info, this.Parent.Parent, recreatedCurrent);
                }
                else
                {
                    return new ActiveNode(this.Parent.Info, this.Parent.Parent, this.Parent.LastChild);
                }
            }

            public FinalNode RecreateChildrenAsFinal()
            {
                ActiveChild curr = this.LastChild;
                FinalNode newCurr = null;

                while (curr != null)
                {
                    newCurr = new FinalNode(curr.Info, curr.Child, newCurr);
                    curr = curr.PrevChild;
                }

                return newCurr;
            }

            public IParsingTreeNode<T> Complete()
            {
                return new FinalNode(this.Info, this.RecreateChildrenAsFinal(), null);
            }
        }

        class ActiveChild : Node
        {
            public FinalNode Child { get; private set; }
            public ActiveChild PrevChild { get; private set; }

            public ActiveChild(T info, FinalNode child, ActiveChild prevChild)
                : base(info)
            {
                this.Child = child;
                this.PrevChild = prevChild;
            }
        }

        class FinalNode : Node, IParsingTreeNode<T>
        {
            public IParsingTreeNode<T> Child { get; private set; }
            public IParsingTreeNode<T> NextChild { get; private set; }

            public FinalNode(T info, FinalNode child, FinalNode nextChild)
                : base(info)
            {
                this.Child = child;
                this.NextChild = nextChild;
            }
        }

    }
}
