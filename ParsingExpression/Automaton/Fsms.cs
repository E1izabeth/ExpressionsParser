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
        DFA,
        MDFA
    }

    static class Fsms
    {
        private static IFsm MakeNfa(Expr expr)
        {
            var fsm = ExprFsmBuilder.BuildFsm(expr, MakeNfa);
            fsm.SaveGraphToFile(@"c:\temp\outNFA.dgml");
            return fsm;
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
            fsm3.SaveGraphToFile(@"c:\temp\outDFA.dgml");
            return fsm3;
        }

        private static IFsmRunner MakeDfaRunner(IFsm fsm)
        {
            return new DfaFsmRunner(fsm, MakeDfaRunner);
        }

        private static IFsm MakeMDfa(Expr expr)
        {
            var fsm = ExprFsmBuilder.BuildFsm(expr, MakeMDfa);
            var fsm2 = fsm.RemoveEmptyTransitions();
            var fsm3 = fsm2.MakeDFA();
            var fsm4 = fsm3.MinimizeDFA();
            return fsm4;
        }

        private static IFsmRunner MakeMDfaRunner(IFsm fsm)
        {
            return new DfaFsmRunner(fsm, MakeMDfaRunner);
        }

        public static IFsmRunner MakeFsmRunner(Expr expr, FsmRunnerMode mode)
        {
            IFsmRunner runner;

            switch (mode)
            {
                case FsmRunnerMode.NFA: runner = MakeNfaRunner(MakeNfa(expr)); break;
                case FsmRunnerMode.DFA: runner = MakeDfaRunner(MakeDfa(expr)); break;
                case FsmRunnerMode.MDFA: runner = MakeMDfaRunner(MakeMDfa(expr)); break;
                default:
                    throw new NotImplementedException(mode.ToString());
            }

            return runner;
        }
    }
}
