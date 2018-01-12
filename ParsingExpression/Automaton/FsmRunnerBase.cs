using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    abstract class FsmRunnerBase
    {
        readonly Func<IFsm, IFsmRunner> _checkRunnerFabric;

        protected FsmRunnerBase(Func<IFsm, IFsmRunner> checkRunnerFabric)
        {
            _checkRunnerFabric = checkRunnerFabric;
        }

        protected bool MatchEdge(string text, int pos, IFsmTransition transition)
        {
            // Console.WriteLine("Trying to match '{0}' character at {1} against {2}", text[pos], pos, transition.Condition);

            bool ok;

            if (transition.Condition.CheckFsmOrNull != null)
            {
                var runner = _checkRunnerFabric(transition.Fsm);
                ok = runner.IsMatch(text.Substring(pos)) == transition.Condition.CheckCondition;
            }
            else if (transition.Condition.ClassTestOrNull != null)
            {
                ok = transition.Condition.ClassTestOrNull(text[pos]);
            }
            else if (transition.Condition.Character.HasValue)
            {
                ok = transition.Condition.Character.Value == text[pos];
            }
            else
            {
                throw new NotSupportedException("Dfa cannot use sigma-transitions!");
            }

            return ok;
        }
    }
}
