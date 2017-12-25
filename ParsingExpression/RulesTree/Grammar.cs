﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.RulesTree
{
    class Grammar : IGrammar
    {
        readonly Rule _rootRule;

        public Rule RootRule { get { return _rootRule; } }

        readonly List<Rule> _rules;
        public IReadOnlyCollection<Rule> Rules { get; private set; }

        public Grammar(params Rule[] rules)
        {
            _rootRule = rules.First();
            _rules = rules.ToList();
            this.Rules = new ReadOnlyCollection<Rule>(_rules);
        }      
    }
}