using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    interface IFsmRunner
    {
        bool IsMatch(string text);
    }

    interface IFsm
    {
        IFsmState InitialState { get; }

        IReadOnlyList<IFsmState> States { get; }
        IReadOnlyList<IFsmTransition> Transitions { get; }

        IFsmState CreateState();
        IFsmTransition CreateTransition(IFsmState from, IFsmState to, IFsmTransitionCondition Condition);

        IFsm Optimize();

        void SetInitialState(IFsmState state);
    }

    interface IFsmState : IComparable<IFsmState>
    {
        IFsm Fsm { get; }

        int Id { get; }
        bool IsFinal { get; }
        

        IReadOnlyList<IFsmTransition> OutTransitions { get; }
        IReadOnlyList<IFsmTransition> InTransitions { get; }


        void SetFinal();
        void Delete();
    }

    interface IFsmTransition : IComparable<IFsmTransition>
    {
        IFsm Fsm { get; }

        IFsmState From { get; }
        IFsmState To { get; }
        //char? Character { get; }
        //Func<char, bool> ClassTest { get; }

        IFsmTransitionCondition Condition { get; }

        void Delete();
    }

    interface IFsmTransitionCondition : IComparable<IFsmTransitionCondition>
    {
        char? Character { get; }
        Func<char, bool> ClassTestOrNull { get; }

        IFsm CheckFsmOrNull { get; }
        bool CheckCondition { get; }

        bool IsSigma { get; }
    }
}
