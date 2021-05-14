// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/OptimalAssignment.cs

using System.Collections.Generic;
using System.Linq;
using SelfAwareHR.Utilities;

namespace SelfAwareHR.Solver
{
    public class OptimalAssignment
    {
        public static void Solve(Dictionary<RoleSpecLevel, int>         tasks,
                                 Dictionary<Actor, List<RoleSpecLevel>> agents,
                                 out Dictionary<RoleSpecLevel, int>     missing,
                                 out List<Actor>                        redundant,
                                 out Dictionary<Actor, RoleSpecLevel>   assignments)
        {
            var n          = tasks.Count + agents.Count + 2;
            var i          = 0;
            var source     = i++;
            var drain      = i++;
            var agentNodes = new Dictionary<Actor, int>();
            foreach (var agent in agents)
            {
                agentNodes.Add(agent.Key, i++);
            }

            var taskNodes = new Dictionary<RoleSpecLevel, int>();
            foreach (var task in tasks)
            {
                taskNodes.Add(task.Key, i++);
            }

            missing     = new Dictionary<RoleSpecLevel, int>();
            redundant   = new List<Actor>();
            assignments = new Dictionary<Actor, RoleSpecLevel>();


            var graph = new FlowNetwork(n);
            foreach (var agent in agents)
            {
                // flow from source to employees, capacity is 1
                graph.AddEdge(source, agentNodes[agent.Key]);
                foreach (var skill in agent.Value)
                {
                    // flow from employee to tasks, capacity is 1
                    graph.AddEdge(agentNodes[agent.Key], taskNodes[skill]);
                }
            }


            foreach (var task in tasks)
            {
                // flow from tasks to drain, capacity is number required
                graph.AddEdge(taskNodes[task.Key], drain, task.Value);
            }

            // max flow solver, will try to fill as much capacity as possible.
            var solver = new FordFulkerson(graph, source, drain);

            // todo: we have a max flow solution, but not a min cost.
            // craft cost function based on skill, seniority, salary, etc.
            // either add a minimizing step (Fulkerson's Out of Kilter?),
            // or move to a different max-flow, min-cost algorithm (simplex?).

            // assigned tasks
            foreach (var agent in agents)
            {
                var node = agentNodes[agent.Key];
                var edge = graph.Out(node).FirstOrDefault(e => e.Flow > double.Epsilon);
                if (edge != null)
                {
                    // agent was assigned to a task
                    var task = taskNodes.First(t => t.Value == edge.To);
                    assignments.Add(agent.Key, task.Key);
                }
                else
                {
                    // agent was not assigned to a task
                    redundant.Add(agent.Key);
                }
            }

            // missing skills
            foreach (var task in tasks)
            {
                var node = taskNodes[task.Key];
                var edge = graph.Out(node).First(e => e.To == drain);
                if (edge.Capacity - edge.Flow > double.Epsilon)
                {
                    // not all capacity was used -> not all tasks are assigned.
                    missing.Add(task.Key, (int) (edge.Capacity - edge.Flow + double.Epsilon));
                }
            }
        }
    }
}