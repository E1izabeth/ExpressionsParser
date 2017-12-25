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
        private static IFsm MakeNfa(Expr expr)
        {
            return ExprFsmBuilder.BuildFsm(expr, MakeNfa);
        }

        private static IFsmRunner MakeNfaRunner(IFsm fsm)
        {
            return new NfaFsmRunner(fsm, MakeNfaRunner);
        }

        private static IFsm MakeDfa(Expr expr)
        {
            var fsm = ExprFsmBuilder.BuildFsm(expr, MakeDfa);
            var fsm2 = fsm.RemoveEmptyTransitions();
            var fsm3 = fsm2.MakeDFA();
            return fsm3;
        }

        private static IFsmRunner MakeDfaRunner(IFsm fsm)
        {
            return new DfaFsmRunner(fsm, MakeDfaRunner);
        }

        public static IFsmRunner MakeFsmRunner(Expr expr, FsmRunnerMode mode)
        {
            IFsmRunner runner;

            switch (mode)
            {
                case FsmRunnerMode.NFA: runner = MakeNfaRunner(MakeNfa(expr)); break;
                case FsmRunnerMode.DFA: runner = MakeDfaRunner(MakeDfa(expr)); break;
                default:
                    throw new NotImplementedException(mode.ToString());
            }

            return runner;
        }
    }
}
