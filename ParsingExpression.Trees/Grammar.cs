using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    public class ParsingResult
    {
        public string Text { get; private set; }
        public StringTreeNode Root { get; private set; }

        public ParsingResult(string text, StringTreeNode root)
        {
            this.Text = text;
            this.Root = root;
        }
    }

    public class Grammar
    {
        public Rule RootRule { get; private set; }
        public IReadOnlyCollection<Rule> CollectionOfRules { get; private set; }

        readonly Dictionary<string, Rule> _rules;

        public Grammar(Rule rootRule, params Rule[] collectionOfRules)
        {
            this.RootRule = rootRule;
            this.CollectionOfRules = collectionOfRules;

            _rules = collectionOfRules.ToDictionary(r => r.Name);
            _rules.Add(rootRule.Name, rootRule);
        }

        public ParsingResult Parse(string text)
        {
            var state = this.RootRule.TryParse(ParsingState.MakeInitial(text, this));

            state.SaveStatesLogToFile(@"c:\temp\tg1.dgml");

            var t = state.CurrChildren.First().CollectTree(s => s.Children, s => $"{s.Rule.Name}: {s.Fragment}");
            Console.WriteLine(t);
            throw new NotImplementedException("");
        }

        public Rule GetRule(string ruleName)
        {
            return _rules[ruleName];
        }
    }
}
