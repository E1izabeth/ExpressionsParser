using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Trees
{
    public class GrammarRunner : IExprVisitor<RunnerParsingState>
    {
        readonly Grammar _grammar;

        RunnerParsingState _currState;

        public RunnerParsingState LastState { get { return _currState; } }

        public GrammarRunner(Grammar g)
        {
            _grammar = g;
        }

        public IParsingTreeNode<ParsingTreeNodeInfo> Match(string text)
        {
            _currState = RunnerParsingState.MakeInitial(text, _grammar);
            do
            {
                _currState = _currState.CurrentExpr.Apply(this);
            }
            while (_currState.Parent != null);

            return _currState.GetTree();
        }

        #region IExprVisitor<RunnerParsingState> implementation

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitAlternative(AlternativesExpr alternativesExpr)
        {
            int startPos = _currState.Pos;

            RunnerParsingState nextState;
            if (_currState.InvocationCount < alternativesExpr.Items.Count && _currState.LastMatchSuccessed != true)
            {
                nextState = _currState.EnterChild(alternativesExpr.Items[_currState.InvocationCount]);
            }
            else
            {
                nextState = _currState.ExitChild(_currState.LastMatchSuccessed.Value, _currState.Pos - startPos);
            }

            return nextState;
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitCharClass(CharClassExpr charClassExpr)
        {
            if (_currState.Pos >= _currState.Text.Length)
                return _currState.ExitChild(false);

            if (charClassExpr.ClassTest(_currState.Text[_currState.Pos]))
            {
                return _currState.ExitChild(true, 1);
            }
            return _currState.ExitChild(false);
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitChars(CharsExpr charsExpr)
        {
            int startPos = _currState.Pos;
            if (_currState.Pos + charsExpr.Chars.Length > _currState.Text.Length)
                return _currState.ExitChild(false);

            int j = _currState.Pos;

            for (int i = 0; i < charsExpr.Chars.Length; i++)
            {
                if (_currState.Text[j] != charsExpr.Chars[i])
                    return _currState.ExitChild(false);

                ++j;
            }

            return _currState.ExitChild(true, j - startPos);
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitCheck(Check check)
        {
            RunnerParsingState nextState;
            if (_currState.InvocationCount == 0)
            {
                nextState = _currState.EnterChild(check.Child);
            }
            else
            {
                nextState = _currState.ExitChild(_currState.LastMatchSuccessed.Value, _currState.Parent.Pos - _currState.Pos);
            }

            return nextState;
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitCheckNot(CheckNot checkNot)
        {
            RunnerParsingState nextState;
            if (_currState.InvocationCount == 0)
            {
                nextState = _currState.EnterChild(checkNot.Child);
            }
            else
            {
                nextState = _currState.ExitChild(!_currState.LastMatchSuccessed.Value, _currState.Parent.Pos - _currState.Pos);
            }

            return nextState;
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitNum(NumberExpr numberExpr)
        {
            int startPos = _currState.Pos;
            RunnerParsingState nextState;

            if (_currState.InvocationCount < numberExpr.Max)
            {
                if (_currState.LastMatchSuccessed == false)
                {
                    nextState = _currState.ExitChild(_currState.InvocationCount >= numberExpr.Min);
                }
                else
                {
                    nextState = _currState.EnterChild(numberExpr.Child);
                }
            }
            else
            {
                nextState = _currState.ExitChild(true);
            }

            return nextState;
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitRuleCall(RuleCallExpr callExpr)
        {
            RunnerParsingState nextState;

            if (_currState.InvocationCount ==0)
            {
                var targetRule = _grammar.GetRule(callExpr.RuleName);
                nextState = _currState.EnterRule(targetRule);
            }
            else
            {
                nextState = _currState.ExitChild(_currState.LastMatchSuccessed.Value);
            }

            return nextState;
        }

        RunnerParsingState IExprVisitor<RunnerParsingState>.VisitSequence(SequenceExpr sequenceExpr)
        {
            int startPos = _currState.Pos;

            RunnerParsingState nextState;
            if (_currState.InvocationCount < sequenceExpr.Items.Count)
            {
                if (_currState.LastMatchSuccessed == false)
                {
                    nextState = _currState.ExitChild(false);
                }
                else
                {
                    nextState = _currState.EnterChild(sequenceExpr.Items[_currState.InvocationCount]);
                }
            }
            else
            {
                nextState = _currState.ExitChild(_currState.LastMatchSuccessed.Value);
            }

            return nextState;
        }

        #endregion
    }
}
