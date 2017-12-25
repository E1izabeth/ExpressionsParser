using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.RulesTree
{
    interface IGrammar
    {
        IReadOnlyCollection<Rule> Rules { get; }
        Rule RootRule { get; }
    }
}
