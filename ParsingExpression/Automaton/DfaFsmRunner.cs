using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class DfaFsmRunner : IFsmRunner
    {
        readonly IFsm _fsm;

        public DfaFsmRunner(IFsm fsm)
        {
            _fsm = fsm;
        }

        public bool IsMatch(string text)
        {
            throw new NotImplementedException("Dfa");
        }
    }
}
