using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class FsmFragment
    {
        public IFsmState From { get; private set; }
        public IFsmState To { get; private set; }

        public FsmFragment(IFsmState from, IFsmState to)
        {
            this.From = from;
            this.To = to;
        }
    }

    class ExprFsmBuilder : IExprVisitor<FsmFragment>
    {
        readonly Fsm _fsm = new Fsm();
        readonly Func<Expr, IFsm> _checkerFsmBuilder;

        private ExprFsmBuilder(Func<Expr, IFsm> checkerFsmBuilder)
        {
            _checkerFsmBuilder = checkerFsmBuilder;
        }

        #region IExprVisitor<FsmFragment> implementation

        FsmFragment IExprVisitor<FsmFragment>.VisitAlternative(AlternativesExpr alternativesExpr)
        {
            var from = _fsm.CreateState();
            var to = _fsm.CreateState();

            foreach (var elem in alternativesExpr.Items)
            {
                var fragment = elem.Apply(this);
                _fsm.CreateTransition(from, fragment.From, FsmTransitionCondition.EmptyCondition);
                _fsm.CreateTransition(fragment.To, to, FsmTransitionCondition.EmptyCondition);
            }

            return new FsmFragment(from, to);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitCharClass(CharClassExpr charClassExpr)
        {
            var from = _fsm.CreateState();
            var to = _fsm.CreateState();

            FsmTransitionCondition condition = new FsmTransitionCondition(null, charClassExpr.ClassTest, null, false);
            _fsm.CreateTransition(from, to, condition);

            return new FsmFragment(from, to);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitChars(CharsExpr charsExpr)
        {
            var from = _fsm.CreateState();
            var to = from;

            foreach (var ch in charsExpr.Chars)
            {
                var last = _fsm.CreateState();
                FsmTransitionCondition condition = new FsmTransitionCondition(ch, null, null, false);
                _fsm.CreateTransition(to, last, condition);
                to = last;
            }
            
            return new FsmFragment(from, to);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitCheck(Check check)
        {
            var childFsm = _checkerFsmBuilder(check.Child);

            var from = _fsm.CreateState();
            var to = _fsm.CreateState();

            _fsm.CreateTransition(from, to, new FsmTransitionCondition(null, null, childFsm, true));

            return new FsmFragment(from, to);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitCheckNot(CheckNot checkNot)
        {
            var childFsm = _checkerFsmBuilder(checkNot.Child);

            var from = _fsm.CreateState();
            var to = _fsm.CreateState();

            _fsm.CreateTransition(from, to, new FsmTransitionCondition(null, null, childFsm, false));

            return new FsmFragment(from, to);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitNum(NumberExpr numExpr)
        {
            if (numExpr.Max == 0)
                throw new ArgumentOutOfRangeException("");

            var numItem = numExpr.Child.Apply(this);
            var from = numItem.From;
            var prevTo = numItem.To;
            var last = numItem.To;

            if (numExpr.Min == 0)
            {
                _fsm.CreateTransition(from, last, FsmTransitionCondition.EmptyCondition);
                if(numExpr.Max != 1)
                    _fsm.CreateTransition(last, from, FsmTransitionCondition.EmptyCondition);
            }
            else
            {
                for (int i = 0; i < numExpr.Min-1; i++)
                {
                    numItem = numExpr.Child.Apply(this);
                    _fsm.CreateTransition(prevTo, numItem.From, FsmTransitionCondition.EmptyCondition);
                    prevTo = numItem.To;
                }
                var s = prevTo;
                last = _fsm.CreateState();
                if (numExpr.Max >= int.MaxValue)
                {
                    _fsm.CreateTransition(prevTo, last, FsmTransitionCondition.EmptyCondition);
                    _fsm.CreateTransition(prevTo, numItem.From, FsmTransitionCondition.EmptyCondition);
                }
                else
                {
                    for (int i = numExpr.Min; i < numExpr.Max; i++)
                    {
                        numItem = numExpr.Child.Apply(this);
                        _fsm.CreateTransition(prevTo, numItem.From, FsmTransitionCondition.EmptyCondition);
                        prevTo = numItem.To;
                        _fsm.CreateTransition(prevTo, last, FsmTransitionCondition.EmptyCondition);
                    }
                    _fsm.CreateTransition(s, last, FsmTransitionCondition.EmptyCondition);
                }
            }
            
            return new FsmFragment(from, last);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitSequence(SequenceExpr sequenceExpr)
        {
            var seqItem = sequenceExpr.Items[0].Apply(this);
            var from = seqItem.From;
            var prevTo = seqItem.To;

            for (int i = 1; i < sequenceExpr.Items.Count; i++)
            {
                seqItem = sequenceExpr.Items[i].Apply(this);
                _fsm.CreateTransition(prevTo, seqItem.From, FsmTransitionCondition.EmptyCondition);
                prevTo = seqItem.To;
            }

            return new FsmFragment(from, prevTo);
        }

        FsmFragment IExprVisitor<FsmFragment>.VisitRuleCall(RuleCallExpr ruleCallExpr)
        {
            throw new NotImplementedException();
        }

        #endregion

        public static IFsm BuildFsm(Expr expr, Func<Expr, IFsm> checkerFsmBuilder)
        {
            var builder = new ExprFsmBuilder(checkerFsmBuilder);
            var fsmFragment = expr.Apply(builder);
            var fsm = builder._fsm;

            fsm.SetInitialState(fsmFragment.From);
            fsmFragment.To.SetFinal();

            return fsm;
        }
        
    }
}
