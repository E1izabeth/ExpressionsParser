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
        ParsingState _mainState;

        public ExprTreeRunner(Expr expr)
        {
            _expr = expr;
        }

        public bool IsMatch(string text)
        {
            _mainState = ParsingState.MakeInitial(text, _expr);
            _currState = _mainState;

            do
            {
                _currState = _currState.CurrentExpr.Apply(this);
            }
            while (!(_currState.CurrentExpr == _mainState.CurrentExpr && _currState.SeqIndex == _mainState.CurrentExpr.GetItems().Count()));

            return _currState.LastMatchSuccessed;
        }

        #region IExprVisitor<ParsingState> implementation

        ParsingState IExprVisitor<ParsingState>.VisitAlternative(AlternativesExpr alternativesExpr)
        {
            int startPos = _currState.Pos;
            ParsingState nextState;

            if(_currState.PrevChildIndex == -1)
            {
                nextState = _currState.ExprMatchContinue(alternativesExpr.Items[_currState.PrevChildIndex + 1]);
            }
            else if (_currState.PrevChildIndex < 1)
            {
                if (_currState.LastMatchSuccessed)
                    nextState = _currState.ExprMatchSuccess(_currState.Pos - startPos);
                else
                    nextState = _currState.ExprMatchContinue(alternativesExpr.Items[_currState.PrevChildIndex + 1]);
            }
            else
            {
                if (_currState.LastMatchSuccessed)
                    nextState = _currState.ExprMatchSuccess(_currState.Pos - startPos);
                else
                    nextState = _currState.ExprMatchFail(_currState.Pos - startPos);
            }

            return nextState;
        }

        ParsingState IExprVisitor<ParsingState>.VisitCharClass(CharClassExpr charClassExpr)
        {
            if (_currState.Pos >= _currState.Text.Length)
                return _currState.ExprMatchFail(0);

            if (charClassExpr.ClassTest(_currState.Text[_currState.Pos]))
            {
                return _currState.ExprMatchSuccess(1);
            }
            return _currState.ExprMatchFail(0);
        }

        ParsingState IExprVisitor<ParsingState>.VisitChars(CharsExpr charsExpr)
        {
            int startPos = _currState.Pos;
            if (_currState.Pos + charsExpr.Chars.Length > _currState.Text.Length)
                return _currState.ExprMatchFail(0);

            int j = _currState.Pos;

            for (int i = 0; i < charsExpr.Chars.Length; i++)
            {
                if (_currState.Text[j] != charsExpr.Chars[i])
                    return _currState.ExprMatchFail(0);

                ++j;
            }

            return _currState.ExprMatchSuccess(j - startPos);
        }

        ParsingState IExprVisitor<ParsingState>.VisitCheck(Check check)
        {
            ParsingState nextState;
            int startPos = _currState.Pos;

            if (_currState.PrevChildIndex == -1)
            {
                nextState = _currState.ExprMatchContinue(check.Child);
            }
            else
            {
                if (_currState.LastMatchSuccessed)
                    nextState = _currState.ExprMatchSuccess(_currState.Pos - startPos);
                else
                    nextState = _currState.ExprMatchFail(_currState.Pos - startPos);
            }
            return nextState;
        }

        ParsingState IExprVisitor<ParsingState>.VisitCheckNot(CheckNot checkNot)
        {
            throw new NotImplementedException();
        }

        ParsingState IExprVisitor<ParsingState>.VisitNum(NumberExpr numberExpr)
        {
            throw new NotImplementedException();
        }

        ParsingState IExprVisitor<ParsingState>.VisitRuleCall(IExprVisitor<ParsingState> visitor)
        {
            throw new NotImplementedException();
        }

        ParsingState IExprVisitor<ParsingState>.VisitSequence(SequenceExpr sequenceExpr)
        {
            ParsingState nextState;
            if (_currState.CurrentExpr == _mainState.CurrentExpr)
            {
                _mainState.SeqIndex += 1;
                nextState = _currState.ExprMatchSeqContinue(sequenceExpr.Items[_mainState.SeqIndex]);
            }
            else
                nextState = _currState.ExprMatchSeqContinue(sequenceExpr.Items[_currState.PrevChildIndex + 1]);
            return nextState;
        }

        #endregion

    }
}
