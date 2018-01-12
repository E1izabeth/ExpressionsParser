using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingExpression.Automaton
{
    class Fsm : IFsm, IEnumerable<IFsmTransition>
    {
        class FsmState : IFsmState
        {
            readonly Fsm _owner;

            public IFsm Fsm { get { return _owner; } }

            public int Id { get; private set; }
            public bool IsFinal { get; private set; }
            public bool IsDeleted { get; private set; }
            

            readonly List<FsmTransition> _outTransitions = new List<FsmTransition>();
            readonly List<FsmTransition> _inTransitions = new List<FsmTransition>();

            public IReadOnlyList<IFsmTransition> OutTransitions { get; private set; }
            public IReadOnlyList<IFsmTransition> InTransitions { get; private set; }

            public FsmState(Fsm owner, int id)
            {
                _owner = owner;
                this.Id = id;
                this.IsDeleted = false;
                
                this.InTransitions = new ReadOnlyCollection<FsmTransition>(_inTransitions);
                this.OutTransitions = new ReadOnlyCollection<FsmTransition>(_outTransitions);
                this.IsFinal = false;
            }

            public void SetFinal()
            {
                this.IsFinal = true;
            }

            public void RegisterInTransition(FsmTransition t)
            {
                if (t.To != this || t.IsDeleted)
                    throw new ArgumentException();

                _inTransitions.Add(t);
            }

            public void RegisterOutTransition(FsmTransition t)
            {
                if (t.From != this || t.IsDeleted)
                    throw new ArgumentException();

                _outTransitions.Add(t);
            }

            internal void UnregisterOutTransition(FsmTransition fsmTransition)
            {
                _outTransitions.Remove(fsmTransition);
            }

            internal void UnregisterInTransition(FsmTransition fsmTransition)
            {
                _inTransitions.Remove(fsmTransition);
            }

            public void Delete()
            {
                if (this.IsDeleted)
                    throw new InvalidOperationException();

                _inTransitions.ToArray().Where(t => !t.IsDeleted).ForEach(t => t.Delete());
                _outTransitions.ToArray().Where(t => !t.IsDeleted).ForEach(t => t.Delete());

                this.IsDeleted = true;
            }

            public override string ToString()
            {
                return (this.IsDeleted ? "X$" : "$") + this.Id;
            }

            public int CompareTo(IFsmState other)
            {
                return this.Id.CompareTo(other.Id);
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var other = obj as IFsmState;
                return other != null && (other.Fsm == this.Fsm && this.Id == other.Id);
            }
        }

        class FsmTransition : IFsmTransition
        {
            readonly Fsm _owner;

            public IFsm Fsm { get { return _owner; } }

            public FsmState FromState { get; private set; }
            public FsmState ToState { get; private set; }

            public IFsmState From { get { return this.FromState; } }
            public IFsmState To { get { return this.ToState; } }
            //public char? Character { get; private set; }
            //public Func<char, bool> ClassTest { get; private set; }
            public bool IsDeleted { get; private set; }

            public IFsmTransitionCondition Condition { get; private set; }

            public FsmTransition(Fsm owner, FsmState from, FsmState to, IFsmTransitionCondition Condition)//char? character, Func<char, bool> classTest,
            {
                _owner = owner;
                this.FromState = from;
                this.ToState = to;
                //this.Character = character;
                //this.IsDeleted = false;
                //this.ClassTest = classTest;
                this.Condition = Condition;
            }

            public void Delete()
            {
                _owner.DeleteTransitionImpl(this);
            }

            public void MarkAsDeleted()
            {
                if (this.IsDeleted)
                    throw new InvalidOperationException();

                this.IsDeleted = true;
            }

            public override string ToString()
            {
                var cond = string.Empty;
                if ((this.Condition != null) && (this.Condition.Character.HasValue || this.Condition.ClassTestOrNull != null || this.Condition.CheckCondition == true))
                {
                    if (this.Condition.Character.HasValue)
                        cond += " on char '" + this.Condition.Character + "'";
                    if (this.Condition.ClassTestOrNull != null)
                        cond += " on class";
                    else
                        cond += " check ";
                }
                return (this.IsDeleted ? "XT" : "T") + $"[{this.From} --> {this.To}] " + cond;
            }

            public int CompareTo(IFsmTransition other)
            {
                var r = this.From.CompareTo(other.From);
                if (r != 0)
                    return r;

                r = this.To.CompareTo(other.To);
                if (r != 0)
                    return r;

                r = this.Condition.Character.HasValue.CompareTo(other.Condition.Character.HasValue);
                if (r != 0)
                    return r;

                if (this.Condition.Character.HasValue && other.Condition.Character.HasValue)
                {
                    r = this.Condition.Character.Value.CompareTo(other.Condition.Character.Value);
                    if (r != 0)
                        return r;
                }
                else if (this.Condition.Character.HasValue)
                    return 1;
                else
                    return -1;

                r = Comparer<Func<char, bool>>.Default.Compare(this.Condition.ClassTestOrNull, other.Condition.ClassTestOrNull);
                if (r != 0)
                    return r;

                return 0;
            }

            public override int GetHashCode()
            {
                return this.IsDeleted.GetHashCode() ^ this.From.Id.GetHashCode() ^ this.To.Id.GetHashCode() ^ (this.Condition == null? 0 :this.Condition.Character.GetHashCode() ^ (this.Condition.ClassTestOrNull == null ? 0 : this.Condition.ClassTestOrNull.GetHashCode()));
            }

            public override bool Equals(object obj)
            {
                var other = obj as IFsmTransition;
                return other != null && (other.Fsm == this.Fsm && this.CompareTo(other) == 0);
            }
        }

        readonly List<FsmState> _states = new List<FsmState>();
        readonly List<FsmTransition> _transitions = new List<FsmTransition>();

        public IFsmState InitialState { get; private set; }

        public IReadOnlyList<IFsmState> States { get; private set; }
        public IReadOnlyList<IFsmTransition> Transitions { get { return new ReadOnlyCollection<IFsmTransition>(_transitions.Where(t => !t.IsDeleted).ToArray()); } }

        public Fsm(int statesCount = 0)
        {
            this.States = new ReadOnlyCollection<FsmState>(_states);
            //this.Transitions = new ReadOnlyCollection<FsmTransition>(_transitions);

            for (int i = 0; i < statesCount; i++)
                this.CreateState();
        }

        public void SetInitialState(IFsmState state)
        {
            if (state.Fsm != this)
                throw new ArgumentException();

            var initialState = _states[state.Id];
            if (initialState.IsDeleted)
                throw new ArgumentException();

            this.InitialState = initialState;
        }

        public IFsmState CreateState()
        {
            var state = new FsmState(this, _states.Count);
            _states.Add(state);
            return state;
        }

        public IFsmTransition Add(int from, int to, IFsmTransitionCondition Condition)// char? ch = null)
        {
            return this.CreateTransition(_states[from], _states[to], Condition);//CreateTransition(_states[from], _states[to], ch, null, null);
        }

        public IFsmTransition CreateTransition(IFsmState from, IFsmState to, IFsmTransitionCondition Condition)
        {
            if (from.Fsm != this)
                throw new ArgumentException();
            if (to.Fsm != this)
                throw new ArgumentException();

            var fromState = _states[from.Id];
            var toState = _states[to.Id];

            if (fromState.IsDeleted || toState.IsDeleted)
                throw new ArgumentException();

            var transition = new FsmTransition(this, fromState, toState, Condition);
            fromState.RegisterOutTransition(transition);
            toState.RegisterInTransition(transition);

            _transitions.Add(transition);
            return transition;
        }

        private void DeleteTransitionImpl(FsmTransition fsmTransition)
        {
            fsmTransition.FromState.UnregisterOutTransition(fsmTransition);
            fsmTransition.ToState.UnregisterInTransition(fsmTransition);
            fsmTransition.MarkAsDeleted();
        }

        public IFsm Optimize()
        {
            var newFsm = new Fsm();
            var newStateByOldId = new IFsmState[_states.Count];

            foreach (var st in _states)
            {
                if ((st.InTransitions.Count > 0 || st.OutTransitions.Count > 0) && !st.IsDeleted)
                {
                    var newState = newFsm.CreateState();
                    newStateByOldId[st.Id] = newState;

                    if (st.IsFinal)
                        newState.SetFinal();
                }
            }

            _transitions.Where(t => !t.IsDeleted)
                        .Distinct()
                        .ForEach(t => newFsm.CreateTransition(newStateByOldId[t.From.Id], newStateByOldId[t.To.Id], t.Condition));

            if (this.InitialState != null)
                newFsm.SetInitialState(newStateByOldId[this.InitialState.Id]);

            return newFsm;
        }

        IEnumerator<IFsmTransition> IEnumerable<IFsmTransition>.GetEnumerator()
        {
            return this.Transitions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Transitions.GetEnumerator();
        }
    }
}
