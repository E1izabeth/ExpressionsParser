using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParsingExpression.XmlGraph;

namespace ParsingExpression.Automaton
{
    static class Extensions
    {
        struct StatesGroup : IComparable<StatesGroup>
        {
            public readonly ReadOnlyCollection<IFsmState> oldStates;

            public StatesGroup(params IFsmState[] oldStates)
            {
                this.oldStates = new ReadOnlyCollection<IFsmState>(oldStates.OrderBy(s => s.Id).Distinct().ToArray());
            }

            public override int GetHashCode()
            {
                return this.oldStates.Aggregate(0, (n, s) => n ^ s.Id);
            }

            public int CompareTo(StatesGroup other)
            {
                var r = this.oldStates.Count.CompareTo(other.oldStates.Count);
                if (r != 0)
                    return r;

                for (int i = 0; i < this.oldStates.Count; i++)
                {
                    r = this.oldStates[i].CompareTo(other.oldStates[i]);
                    if (r != 0)
                        return r;
                }

                return 0;
            }

            public override bool Equals(object obj)
            {
                return obj is StatesGroup ? this.CompareTo((StatesGroup)obj) == 0 : false;
            }

            public override string ToString()
            {
                return string.Format("{{{0}}}", string.Join(", ", this.oldStates));
            }
        }

        public static IFsm MakeDFA(this IFsm fsm)
        {
            var newStateByOldGroup = new Dictionary<StatesGroup, IFsmState>();
            var newFsm = new Fsm();

            var groupsToHandle = new Queue<StatesGroup>();
            groupsToHandle.Enqueue(new StatesGroup(fsm.InitialState));

            while (groupsToHandle.Count > 0)
            {
                var group = groupsToHandle.Dequeue();
                if (!newStateByOldGroup.ContainsKey(group))
                {
                    var newState = newFsm.CreateState();
                    newStateByOldGroup.Add(group, newState);

                    if (group.oldStates.Any(s => s.IsFinal))
                        newState.SetFinal();

                    group.oldStates.SelectMany(s => s.OutTransitions)
                            .GroupBy(t => t.Condition)
                            .Select(g => new StatesGroup(g.Select(t => t.To).ToArray()))
                            .ForEach(g => groupsToHandle.Enqueue(g));
                }
            }

            newFsm.SetInitialState(newStateByOldGroup[new StatesGroup(fsm.InitialState)]);

            foreach (var kv in newStateByOldGroup)
            {
                foreach (var transitionsByConditionGroup in kv.Key.oldStates.SelectMany(s => s.OutTransitions).GroupBy(t => t.Condition))//t.Condition.Character
                {
                    var targetState = newStateByOldGroup[new StatesGroup(transitionsByConditionGroup.Select(t => t.To).ToArray())];
                    newFsm.CreateTransition(kv.Value, targetState, transitionsByConditionGroup.Key);
                }
            }

            return newFsm;
        }

        public static IFsm RemoveEmptyTransitions(this IFsm fsm)
        {
            var newStateByOldId = new IFsmState[fsm.States.Count];
            var newFsm = new Fsm();
            var visitedStates = new SortedSet<IFsmState>();

            foreach (var st in fsm.States)
            {
                visitedStates.Clear();

                if (fsm.InitialState == st || st.InTransitions.Any(t => !t.Condition.IsSigma))
                {
                    var newState = newFsm.CreateState();
                    newStateByOldId[st.Id] = newState;

                    if (st.Flatten(s => s.OutTransitions.Where(t => t.Condition.IsSigma).Select(t => t.To), visitedStates.Add).Any(s => s.IsFinal))
                        newState.SetFinal();
                }
            }

            newFsm.SetInitialState(newStateByOldId[fsm.InitialState.Id]);

            var visitedTransitions = new SortedSet<string>();

            for (int oldStateId = 0; oldStateId < newStateByOldId.Length; oldStateId++)
            {
                List<int> endStates = new List<int>();
                if (newStateByOldId[oldStateId] != null)
                {
                    foreach (var t in fsm.States[oldStateId].OutTransitions)
                    {
                        if (!t.Condition.IsSigma)
                        {
                            newFsm.CreateTransition(newStateByOldId[oldStateId], newStateByOldId[t.To.Id], t.Condition);
                            // newFsm.CreateTransition(newStateByOldId[oldStateId], newStateByOldId.Where(l => l != null).Single(k => t.To.Id == k.Id), t.Character, null);
                        }
                        else
                        {
                            visitedTransitions.Clear();
                            t.Flatten(ct => ct.To.OutTransitions.Where(nt => nt.Condition.IsSigma && ct.To != t.From), tt => visitedTransitions.Add(tt.ToString()))
                                .SelectMany(ct => ct.To.OutTransitions.Where(nt => !nt.Condition.IsSigma))
                                .ForEach(ct => newFsm.CreateTransition(newStateByOldId[oldStateId], newStateByOldId[ct.To.Id], ct.Condition));
                        }
                    }
                }
            }

            return newFsm;
        }

        private class IntersectionComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            private readonly Func<T, T, bool> _elementsComparer;

            public IntersectionComparer(Func<T, T, bool> elementsComparer)
            {
                _elementsComparer = elementsComparer;
            }

            bool IEqualityComparer<IEnumerable<T>>.Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                return x.Any(t1 => y.Any(t2 => _elementsComparer(t1, t2)));
            }

            int IEqualityComparer<IEnumerable<T>>.GetHashCode(IEnumerable<T> obj)
            {
                return 0;
            }
        }

        private static readonly IntersectionComparer<IFsmTransition> _transitionsIntersectionComparer = new IntersectionComparer<IFsmTransition>(
            (t1, t2) => t1.To == t2.To
        );

        private static readonly IntersectionComparer<IFsmState> _statesIntersectionComparer = new IntersectionComparer<IFsmState>(
            (s1, s2) => s1.Id == s2.Id
        );

        public static IFsm MinimizeDFA(this IFsm fsm)
        {
            var newFsm = new Fsm();

            newFsm.SetInitialState(newFsm.CreateState());

            var newStateByOld = fsm.States.Where(s => fsm.InitialState != s)
                                    .GroupBy(s => s.OutTransitions, _transitionsIntersectionComparer)
                                    .ToDictionary(s => s.ToArray(), s => newFsm.CreateState(), _statesIntersectionComparer);

            newStateByOld.Add(new[] { fsm.InitialState }, newFsm.InitialState);

            fsm.Transitions.Select(t => (
                from: newStateByOld[new[] { t.From }],
                to: newStateByOld[new[] { t.To }],
                cond: t.Condition
            )).Distinct()
                .ForEach(t => newFsm.CreateTransition(t.from, t.to, t.cond));

            newFsm.SetInitialState(newStateByOld[new[] { fsm.InitialState }]);

            foreach (var s in fsm.States)
                if (s.IsFinal)
                    newStateByOld[new[] { s }].SetFinal();

            return newFsm;
        }

        public static XmlGraph.XmlGraph BuildGraph(this IFsm fsm)
        {
            var xg = new XmlGraph.XmlGraph();

            foreach (var item in fsm.States)
            {
                var node = xg.CreateNode(item.Id.ToString());

                if (fsm.InitialState == item)
                    node.Text += "Initial ";

                if (item.IsFinal)
                    node.Text += "Final ";

                node.Text += "$" + item.Id;

                foreach (var t in item.OutTransitions)
                    node.Text += Environment.NewLine + "on " + t.Condition.MakeTransitionString() + " to " + t.To.Id;
            }

            foreach (var item in fsm.Transitions.GroupBy(t => (t.From.Id, t.To.Id)))
            {
                var link = xg[item.First().From.Id.ToString()].ConnectTo(xg[item.First().To.Id.ToString()]);
                link.Text = string.Join(", ", item.Select(t => t.Condition.MakeTransitionString()));
            }

            return xg;
        }

        private static string MakeTransitionString(this IFsmTransitionCondition cond)
        {
            string result;

            if (cond != null)
            {
                if (cond.Character.HasValue)
                    result = cond.Character.ToString();
                else if (cond.ClassTestOrNull != null)
                    result = "class";
                else
                    result = "ε";
            }
            else
                result = "ε";

            return result;
        }
    }
}
