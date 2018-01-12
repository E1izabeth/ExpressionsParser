using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParsingExpression.Automaton;

namespace ParsingExpression.RulesTree
{
    public class ExprTreeRunner : IFsmRunner, IExprVisitor<ParsingState>
    {
        readonly Expr _expr;

        ParsingState _currState;

        public ParsingState LastState { get { return _currState; } }

        public ExprTreeRunner(Expr expr)
        {
            _expr = expr;
        }

        public bool IsMatch(string text)
        {
            _currState = ParsingState.MakeInitial(text, _expr);
            do
            {
                _currState = _currState.CurrentExpr.Apply(this);
            }
            while (_currState.Parent != null);

            return _currState.LastMatchSuccessed == null || _currState.LastMatchSuccessed == false ? false : true;
        }

        #region IExprVisitor<ParsingState> implementation

        ParsingState IExprVisitor<ParsingState>.VisitAlternative(AlternativesExpr alternativesExpr)
        {
            int startPos = _currState.Pos;

            ParsingState nextState;
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

        ParsingState IExprVisitor<ParsingState>.VisitCharClass(CharClassExpr charClassExpr)
        {
            if (_currState.Pos >= _currState.Text.Length)
                return _currState.ExitChild(false);

            if (charClassExpr.ClassTest(_currState.Text[_currState.Pos]))
            {
                return _currState.ExitChild(true, 1);
            }
            return _currState.ExitChild(false);
        }

        ParsingState IExprVisitor<ParsingState>.VisitChars(CharsExpr charsExpr)
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

        ParsingState IExprVisitor<ParsingState>.VisitCheck(Check check)
        {
            ParsingState nextState;
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

        ParsingState IExprVisitor<ParsingState>.VisitCheckNot(CheckNot checkNot)
        {
            ParsingState nextState;
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

        ParsingState IExprVisitor<ParsingState>.VisitNum(NumberExpr numberExpr)
        {
            int startPos = _currState.Pos;
            ParsingState nextState;

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

        ParsingState IExprVisitor<ParsingState>.VisitRuleCall(RuleCallExpr callExpr)
        {
            throw new NotImplementedException();
        }

        ParsingState IExprVisitor<ParsingState>.VisitSequence(SequenceExpr sequenceExpr)
        {
            int startPos = _currState.Pos;

            ParsingState nextState;
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

