using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class NfaFsmRunner : FsmRunnerBase, IFsmRunner
    {
        readonly IFsm _fsm;

        public NfaFsmRunner(IFsm fsm, Func<IFsm, IFsmRunner> checkRunnerFabric)
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
                var visitedTransitions = new SortedSet<string>();

                var transitions = tmpState.Flatten(s => s.OutTransitions.Where(nt => nt.Condition.IsSigma).Select(x => x.To), tt => visitedTransitions.Add(tt.ToString()))
                    .SelectMany(ct => ct.OutTransitions.Where(nt => !nt.Condition.IsSigma));

                foreach (var transition in transitions)
                {
                    if (transition.To.Id == tmpState.Id)
                        continue;
                    bool edge = MatchEdge(text, pos, tmpState, transition);
                    if (edge)
                    {
                        tmpState = transition.To;
                        pos++;
                    }

                    if (edge == true && tmpState.IsFinal && pos == text.Length)
                        return true;
                    if (edge == true && tmpState.Id != transition.From.Id)
                        IsMatch(text, ref pos, ref tmpState);
                    if (tmpState.IsFinal && pos == text.Length)
                        return true;
                }
            }
            return false;
        }
    }
}
