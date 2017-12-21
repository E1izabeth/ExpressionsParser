using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    enum FsmRunnerMode
    {
        NFA,
        DFA
    }

    static class Fsms
    {
        public static IFsmRunner MakeFsmRunner(Expr expr, FsmRunnerMode mode)
        {
            IFsmRunner runner;

            switch (mode)
            {
                case FsmRunnerMode.NFA:
                    {
                        var fsm = ExprFsmBuilder.BuildFsm(expr, null); // TODO: replace null
                        runner = new NfaFsmRunner(fsm);
                    }
                    break;
                case FsmRunnerMode.DFA:
                    {
                        var fsm = ExprFsmBuilder.BuildFsm(expr, null); // TODO: replace null
                        var fsm2 = fsm.RemoveEmptyTransitions();
                        var fsm3 = fsm2.MakeDFA();
                        runner = new DfaFsmRunner(fsm3);
                    }
                    break;
                default:
                    throw new NotImplementedException(mode.ToString());
            }

            return runner;
        }
    }
}
