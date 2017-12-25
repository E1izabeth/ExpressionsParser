using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class DfaFsmRunner : FsmRunnerBase, IFsmRunner
    {
        readonly IFsm _fsm;

        public DfaFsmRunner(IFsm fsm, Func<IFsm, IFsmRunner> checkRunnerFabric)
            : base(checkRunnerFabric)
        {
            _fsm = fsm;
        }

        public bool IsMatch(string text)
        {
            var tmpState = _fsm.InitialState;
            int pos = 0;
            return IsMatch(text, ref pos, ref tmpState);
        }

        private bool IsMatch(string text, ref int pos, ref IFsmState tmpState)
        {
            if (tmpState.IsFinal)
                return true;

            while (!tmpState.IsFinal || pos == text.Length)
            {
                Console.WriteLine(tmpState.Id);
                foreach (var transition in tmpState.OutTransitions)
                {
                    if (MatchEdge(text, pos, tmpState, transition))
                    {
                        tmpState = transition.To;
                        ++pos;
                    }


                    if (tmpState.IsFinal && pos == text.Length)
                        return true;

                    if (tmpState.Id != transition.From.Id)
                        IsMatch(text, ref pos, ref tmpState);

                    if (tmpState.IsFinal && pos == text.Length)
                        return true;
                }
            }

            return false;
        }
    }
}
