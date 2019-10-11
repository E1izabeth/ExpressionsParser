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
            return this.TryMatch(text, out var pos);
        }

        public bool TryMatch(string text, out int end)
        {
            var currState = _fsm.InitialState;
            var pos = 0;

            while (!currState.IsFinal && pos < text.Length)
            {
                var activeTransitions = currState.OutTransitions.Where(t => this.MatchEdge(text, pos, t)).ToArray();

                if (activeTransitions.Length == 0)
                {
                    break;
                }
                else if (activeTransitions.Length == 1)
                {
                    currState = activeTransitions.First().To;
                    pos++;
                }
                else
                {
                    throw new InvalidOperationException("Bad fsm");
                }
            }

            end = pos;
            return currState.IsFinal;
        }

        /*public bool IsMatch(string text)
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
                // Console.WriteLine(tmpState.Id);
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
        }*/
    }
}
