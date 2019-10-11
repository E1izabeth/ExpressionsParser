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

        //TODO:
        // delayed.Push(Tuple.Create(tmpState, pos));
        public bool IsMatch(string text)
        {
            var currState = _fsm.InitialState;
            int pos = 0;
            return IsMatch(text, ref pos, ref currState);
        }

        public bool TryMatch(string text, out int pos)
        {
            var currState = _fsm.InitialState;
            pos = 0;
            return IsMatch(text, ref pos, ref currState);
        }

        //    //int nextEdge = 0;
        //    var delayed = new Stack<Tuple<IFsmState, int>>();
        //    while (currState.IsFinal == false || pos == text.Length)
        //    {
        //        var visitedTransitions = new SortedSet<string>();

        //        var transitions = currState.Flatten(s => s.OutTransitions.Where(nt => nt.Condition.IsSigma).Select(x => x.To), tt => visitedTransitions.Add(tt.ToString()))
        //            .SelectMany(ct => ct.OutTransitions.Where(nt => !nt.Condition.IsSigma));

        //        delayed.Push(Tuple.Create(currState, pos));

        //        foreach (var transition in transitions)
        //        {
        //            bool edge = MatchEdge(text, pos, transition);
        //            if (edge)
        //            {
        //                currState = transition.To;
        //                pos++;
        //                break;
        //            }
        //            else
        //            {
        //                if (delayed.Count > 0)
        //                {
        //                    var retState = delayed.Pop();
        //                    currState = retState.Item1;
        //                    pos = retState.Item2;
        //                    //nextEdge = retState.Item3;
        //                }
        //            }
        //        }
        //    }
        //    return currState.IsFinal;
        //}

        private bool IsMatch(string text, ref int pos, ref IFsmState tmpState)
        {
            if (tmpState.IsFinal)
                return true;

            var delayed = new Stack<(IFsmState fallbackState, int fallbackPos)>();
            var backStates = new List<(int pos, int fallbackLimit, int fallbackCount)>();

            //сохранять outtransitions, pos с которого откат и считать откаты.
            // если кол-во откатов больше кол-ва outTransitions, false
            while ((tmpState.IsFinal == false || pos != text.Length) && pos < text.Length)
            {
                if (tmpState.OutTransitions.Any(t => t.Condition.IsSigma))
                    delayed.Push((tmpState, pos));

                var visitedTransitions = new SortedSet<string>();
                var transitions = tmpState.Flatten(
                    s => s.OutTransitions.Where(nt => nt.Condition.IsSigma).Select(x => x.To),
                    tt => visitedTransitions.Add(tt.ToString())
                ).SelectMany(ct => ct.OutTransitions.Where(nt => !nt.Condition.IsSigma));

                bool succ = false;

                foreach (var transition in transitions)
                {
                    bool edge = this.MatchEdge(text, pos, transition);
                    if (edge)
                    {
                        tmpState = transition.To;
                        pos++;
                        succ = true;
                        break;
                    }
                }

                if (!succ)
                {
                    if (backStates.Count == 0 || backStates.Last().pos != pos)
                    {
                        backStates.Add((pos, tmpState.OutTransitions.Count, 1));
                    }
                    else
                    {
                        var last = backStates.Last();
                        if (last.fallbackLimit < last.fallbackCount)
                        {
                            break;
                        }
                        else
                        {
                            var count = last.fallbackCount;
                            count++;
                            backStates.Remove(backStates.Last());
                            backStates.Add((last.pos, last.fallbackLimit, count));
                        }
                    }

                    var backState = delayed.Pop();
                    tmpState = backState.fallbackState;
                    pos = backState.fallbackPos;
                }
            }
            return tmpState.IsFinal;
        }
    }
}
