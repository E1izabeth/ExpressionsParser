using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{

    class FsmTransitionCondition : IFsmTransitionCondition, IComparable<IFsmTransitionCondition>
    {
        public static readonly FsmTransitionCondition EmptyCondition = new FsmTransitionCondition(null, null, null, false);

        public char? Character { get; private set; }

        public Func<char, bool> ClassTestOrNull { get; private set; }

        public IFsm CheckFsmOrNull { get; private set; }

        public bool CheckCondition { get; private set; }

        public bool IsSigma
        {
            get { return !this.Character.HasValue && this.ClassTestOrNull == null && this.CheckCondition != true; }
        }

        public FsmTransitionCondition(char? character, Func<char, bool> classTestOrNull, IFsm checkFsmOrNull, bool checkCondition)
        {
            this.Character = character;
            this.ClassTestOrNull = classTestOrNull;
            this.CheckFsmOrNull = checkFsmOrNull;
            this.CheckCondition = checkCondition;
        }

        public override int GetHashCode()
        {
            return this.Character.GetHashCode() ^ this.CheckCondition.GetHashCode() ^ (
                this.ClassTestOrNull == null ? 1 : this.ClassTestOrNull.GetHashCode()
            ) ^ (
                this.CheckFsmOrNull == null ? 1 : this.CheckFsmOrNull.GetHashCode()
            );
        }

        public int CompareTo(IFsmTransitionCondition other)
        {
            var result = (this.Character == other.Character && this.CheckCondition == other.CheckCondition && this.CheckFsmOrNull == other.CheckFsmOrNull && this.ClassTestOrNull == other.ClassTestOrNull) ? 0 : 1;
            return result;
        }

        public override bool Equals(object obj)
        {
            var other = obj as IFsmTransitionCondition;
            return other == null ? false : this.CompareTo(other) == 0;
        }

        public override string ToString()
        {
            string str;

            if (this.CheckFsmOrNull != null)
            {
                str = "<CheckExpr>";
            }
            else if (this.ClassTestOrNull != null)
            {
                str = "<CharClass>";
            }
            else if (this.Character.HasValue)
            {
                str = "<" + this.Character.Value + ">";
            }
            else
            {
                str = "<Sigma>";
            }

            return str;
        }
    }
}
