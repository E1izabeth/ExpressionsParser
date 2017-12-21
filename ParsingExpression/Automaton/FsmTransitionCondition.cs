using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{

    class FsmTransitionCondition : IFsmTransitionCondition, IComparable<IFsmTransitionCondition>
    {
        public char? Character { get; private set; }

        public Func<char, bool> ClassTestOrNull { get; private set; }

        public IFsm CheckFsmOrNull { get; private set; }

        public bool CheckCondition { get; private set; }

        public bool IsSigmaTransition
        {
            get { return !this.Character.HasValue && this.ClassTestOrNull != null && this.CheckCondition == true; }
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
            return base.GetHashCode();
        }

        public int CompareTo(IFsmTransitionCondition other)
        {
            return (this.Character == other.Character && this.CheckCondition == other.CheckCondition && this.CheckFsmOrNull == other.CheckFsmOrNull && this.ClassTestOrNull == other.ClassTestOrNull)? 1: 0;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
