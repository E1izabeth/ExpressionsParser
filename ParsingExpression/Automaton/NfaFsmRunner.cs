using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class NfaFsmRunner : IFsmRunner
    {
        readonly IFsm _fsm;

        public NfaFsmRunner(IFsm fsm)
        {
            _fsm = fsm;
        }

        public bool IsMatch(string text)
        {
            throw new NotImplementedException("Nfa");
        }
    }
}
