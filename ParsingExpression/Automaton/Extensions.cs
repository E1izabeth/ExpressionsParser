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
                {
                    if (t.Condition != null)
                    {
                        if (t.Condition.Character.HasValue)
                            node.Text += Environment.NewLine + "on " + t.Condition.Character + " to " + t.To.Id;
                        else if (t.Condition.ClassTestOrNull != null)
                            node.Text += Environment.NewLine + "on class to " + t.To.Id;
                        else
                            node.Text += Environment.NewLine + "on ε to " + t.To.Id;
                    }       
                    else
                        node.Text += Environment.NewLine + "on ε to " + t.To.Id;
                }
            }

            foreach (var item in fsm.Transitions)
                xg[item.From.Id.ToString()].ConnectTo(xg[item.To.Id.ToString()]);

            return xg;
        }
    }
}
