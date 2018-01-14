using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    public interface IBinaryTreeActiveNode<T>
    {
        T Data { get; }

        IBinaryTreeActiveNode<T> GoUp();
        IBinaryTreeActiveNode<T> GoLeft();
        IBinaryTreeActiveNode<T> GoRight();

        IBinaryTreeActiveNode<T> CreateLeft(T data);
        IBinaryTreeActiveNode<T> CreateRight(T data);

        IBinaryTreeActiveNode<T> DropLeft();
        IBinaryTreeActiveNode<T> DropRight();
    }

    public static class ImmutableBinaryTree
    {
        public static IBinaryTreeActiveNode<T> Create<T>(T data)
        {
            throw new NotImplementedException("");
        }

        public static IBinaryTreeActiveNode<T> Add<T>(this IBinaryTreeActiveNode<T> root, T data)
            where T : IComparable<T>
        {
            throw new NotImplementedException("");
        }

        public static IEnumerable<T> Enumerate<T>(this IBinaryTreeActiveNode<T> root)
        {
            throw new NotImplementedException("");
        }
    }
}
