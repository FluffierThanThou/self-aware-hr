// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/SelfAwareHR.cs

#define DEBUG_OPTIMIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SelfAwareHR.Solver;
using UnityEngine;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class SelfAwareHR
    {
        private static readonly Dictionary<Team, SelfAwareHR> Settings =
            new Dictionary<Team, SelfAwareHR>();

        private Dictionary<Employee, RoleSpecLevel> _idealAssignments;
        private bool                                _manualOptimization;

        public bool       Active = true;
        public bool       ArtTasks;
        public bool       DesignTasks;
        public bool       FireWhenRedundant;
        public bool       OnlyDrawIdle    = true;
        public bool       OnlyDrawIfSpace = true;
        public bool       ProgrammingTasks;
        public Team       Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team       TeamToReleaseTo;

        protected SelfAwareHR(Team team)
        {
            Team = team;
        }

        private SelfAwareHR(WriteDictionary save)
        {
            if (!DeserializeMe(save))
            {
                throw new Exception("deserializing failed");
            }
        }


        // todo: may want to cache the available pcs, but they are already cached at room level...
        public IEnumerable<Furniture> Desks =>
            Rooms.SelectMany(r => r.GetFurniture("Computer")).Where(c => c.ComputerChair != null);

        public IEnumerable<Actor> Employees => Team.GetEmployeesDirect();

        // todo: may want to cache allowable rooms
        public IEnumerable<Room> Rooms =>
            GameSettings.Instance.sRoomManager.Rooms.Where(r => r.CompatibleWithTeam(Team));

        internal static void Deserialize(WriteDictionary[] data)
        {
            Log.DebugSerializing("deserializing self-aware team settings");
            Settings.Clear();
            foreach (var datum in data)
            {
                try
                {
                    var instance = new SelfAwareHR(datum);
                    Settings[instance.Team] = instance;
                }
                catch (Exception ex)
                {
                    Console.LogError($"error deserializing self-aware team settings: ${ex.Message}\n${ex.StackTrace}");
                }
            }

            Log.DebugSerializing($"{Settings.Count} self-aware team settings deserialized");
        }

        protected bool DeserializeMe(WriteDictionary save)
        {
            var teams    = GameSettings.Instance.sActorManager.Teams;
            var teamName = save.Get<string>("Team");

            Log.DebugSerializing($"deserializing data for {teamName}");

            if (teamName.IsNullOrEmpty() ||
                !teams.TryGetValue(teamName, out Team))
            {
                Console.LogError(
                    $"failed to load self-aware settings for '{teamName ?? "NULL"}', settings will be reset. If this keeps happening, please report the issue.");
                return false;
            }

            Active           = save.Get("Active", false);
            OnlyDrawIdle     = save.Get("OnlyDrawIdle", true);
            OnlyDrawIfSpace  = save.Get("OnlyDrawIfSpace", true);
            DesignTasks      = save.Get("DesignTasks", false);
            ProgrammingTasks = save.Get("ProgrammingTasks", false);
            ArtTasks         = save.Get("ArtTasks", false);

            var teamToReleaseTo = save.Get<string>("ReleaseTo");
            if (!teamToReleaseTo.IsNullOrEmpty())
            {
                teams.TryGetValue(teamToReleaseTo, out TeamToReleaseTo);
            }

            var teamsToDrawFrom = save.Get<string[]>("DrawFrom");
            if (!teamsToDrawFrom.IsNullOrEmpty())
            {
                TeamsToDrawFrom = new List<Team>();
                foreach (var name in teamsToDrawFrom)
                {
                    if (teams.TryGetValue(name, out var team))
                    {
                        TeamsToDrawFrom.Add(team);
                    }
                }
            }

            Log.DebugSerializing($"deserialized self-aware team settings for {Team.Name}");
            Log.DebugSerializing(ToString());
            return true;
        }

        public Dictionary<Employee, SpecLevel[]> EmployeeSpecs(Employee.EmployeeRole  role,
                                                               IEnumerable<SpecLevel> specs)
        {
            var agents = new Dictionary<Employee, SpecLevel[]>();

            foreach (var actor in Team.GetEmployeesDirect().Where(a => a.employee.IsRole(role)))
            {
                var skills = new List<SpecLevel>();
                foreach (var spec in specs)
                {
                    if (actor.employee.GetSpecialization(role, spec.Spec, actor) >= spec.Level)
                    {
                        skills.Add(spec);
                    }
                }

                agents.Add(actor.employee, skills.ToArray());
            }

            return agents;
        }

        public static SelfAwareHR For(Team team)
        {
            if (Settings.TryGetValue(team, out var teamSettings))
            {
                return teamSettings;
            }

            teamSettings = new SelfAwareHR(team);
            Settings.Add(team, teamSettings);

            return teamSettings;
        }

        public void GetTasksAndAgents(Employee.EmployeeRole                         role,
                                      bool                                          forceFull,
                                      ref Dictionary<RoleSpecLevel, int>            tasks,
                                      ref Dictionary<Employee, List<RoleSpecLevel>> agents)
        {
            var _tasks  = RequiredSpecs(role, forceFull);
            var _agents = EmployeeSpecs(role, _tasks.Keys.Distinct());

            foreach (var _task in _tasks)
            {
                tasks.Add(new RoleSpecLevel(role, _task.Key), _task.Value);
            }

            foreach (var _agent in _agents)
            {
                if (!agents.TryGetValue(_agent.Key, out var skills))
                {
                    skills = new List<RoleSpecLevel>();
                    agents.Add(_agent.Key, skills);
                }

                foreach (var _skill in _agent.Value)
                {
                    skills.Add(new RoleSpecLevel(role, _skill));
                }
            }
        }

        private void NotifyFailedToFillVacancies(Dictionary<RoleSpecLevel, int> missing)
        {
            throw new NotImplementedException();
        }

        private void NotifyFailedToRelease_NoDesks()
        {
            throw new NotImplementedException();
        }

        private void NotifyFailedToReleaseRedundant(List<Employee> redundant)
        {
            throw new NotImplementedException();
        }

        private void NotifyReleased(List<Employee> released)
        {
            throw new NotImplementedException();
        }

        internal void Optimize(bool manual = false)
        {
            _manualOptimization = manual;
            var allTasks  = new Dictionary<RoleSpecLevel, int>();
            var allAgents = new Dictionary<Employee, List<RoleSpecLevel>>();

            if (DesignTasks)
            {
                GetTasksAndAgents(Employee.EmployeeRole.Designer, true, ref allTasks, ref allAgents);
            }

            if (ProgrammingTasks)
            {
                GetTasksAndAgents(Employee.EmployeeRole.Programmer, true, ref allTasks, ref allAgents);
            }

            if (ArtTasks)
            {
                GetTasksAndAgents(Employee.EmployeeRole.Artist, true, ref allTasks, ref allAgents);
            }

            if (allTasks.Any() && allAgents.Any())
            {
                Solve(allTasks, allAgents, out var missing, out var redundant, out var assignments);

#if DEBUG_OPTIMIZATION
                Log.Debug("Tasks:");
                foreach (var task in allTasks)
                {
                    missing.TryGetValue(task.Key, out var shortfall);
                    Log.Debug($"    {task.Key}: {task.Value - shortfall}/{task.Value} {(shortfall > 0 ? "!!" : "")}");
                }

                Log.Debug("Agents:");
                foreach (var agent in allAgents)
                {
                    var _redundant = redundant.Contains(agent.Key);
                    var assigned   = assignments.ContainsKey(agent.Key);
                    Log.Debug(
                        $"    {agent.Key.FullName}: {(assigned ? assignments[agent.Key].ToString() : "NOT ASSIGNED")} {(_redundant ? "REDUNDANT" : "")}");
                }
#endif
                if (redundant.Any() && !(TryReleaseRedundant(ref redundant) || TryFireRedundant(ref redundant)))
                {
                    NotifyFailedToReleaseRedundant(redundant);
                }

                if (missing.Any() && !(TryDrawMissing(ref missing) || TryHireMissing(ref missing)))
                {
                    NotifyFailedToFillVacancies(missing);
                }

                _idealAssignments = assignments;
            }

            if (manual)
            {
                // todo: popup for no action needed.
            }
        }

        public Dictionary<SpecLevel, int> RequiredSpecs(Employee.EmployeeRole role, bool forceFull = false)
        {
            float totalWork = 0;
            var   specWork  = new Dictionary<string, float[]>();

            // calculate the total amount of work, and the amount
            // of work per specialization level.
            foreach (var item in WorkItems(role))
            {
                foreach (var task in item.Features)
                {
                    if (task.RelevantFor(role, forceFull))
                    {
                        var feature  = task.Feature;
                        var work     = task.DevTime(role);
                        var perLevel = specWork.GetOrAdd(feature.Spec, spec => new float[4]);

                        totalWork               += work;
                        perLevel[feature.Level] += work;
                    }
                }
            }

            // get the total number of employees this team would
            // need, then calculate the number of employees per 
            // spec.
            var totalCount = GameData.GetOptimalEmployees(Mathf.Max(1, Mathf.CeilToInt(totalWork)));
            var specCount  = new Dictionary<SpecLevel, int>();
            foreach (var spec in specWork)
            {
                // assign whole employees to each spec level, making
                // sure that the highest levels are staffed. Added 
                // "partial" employees at higher levels are compensated
                // by removing employees at lower levels.
                //
                // Note that at worst, this assigns .99 extra per spec, and
                // it never assigns less employees than optimal. This makes
                // sure each spec is staffed, but we may want to take away
                // staff from specs with multiple employees assigned to get
                // closer to the optimal team size.

                var extra = 0f;

                // count down from the highest level requirement to make 
                // sure these are adequately staffed.
                for (var i = 3; i >= 0; i--)
                {
                    var raw   = spec.Value[i] / totalWork * totalCount;
                    var count = Mathf.RoundToInt(raw);
                    var diff  = raw - count;

                    if (diff > extra)
                    {
                        extra += 1 - diff;
                        count += 1;
                    }
                    else
                    {
                        extra -= diff;
                    }

                    if (count > 0)
                    {
                        specCount.Add(new SpecLevel(spec.Key, i), count);
                    }
                }
            }

            return specCount;
        }

        internal static WriteDictionary[] Serialize()
        {
            Log.DebugSerializing("serializing self-aware team settings");
            return Settings.Values
                           .Select(instance => instance.SerializeMe())
                           .ToArray();
        }

        protected WriteDictionary SerializeMe()
        {
            // Teams do not inherit Writable, and cannot be referenced uniquely. I guess we're going to have 
            // to reference them by name, which I don't like, but at least the names _should_ be unique.
            Log.DebugSerializing($"serializing data for {Team.Name}");
            Log.DebugSerializing(ToString());
            var save = new WriteDictionary();
            save["Team"]              = Team.Name;
            save["Active"]            = Active;
            save["OnlyDrawIdle"]      = OnlyDrawIdle;
            save["OnlyDrawIfSpace"]   = OnlyDrawIfSpace;
            save["FireWhenRedundant"] = FireWhenRedundant;
            save["DesignTasks"]       = DesignTasks;
            save["ProgrammingTasks"]  = ProgrammingTasks;
            save["ArtTasks"]          = ArtTasks;
            save["ReleaseTo"]         = TeamToReleaseTo?.Name ?? "None";
            save["DrawFrom"]          = TeamsToDrawFrom?.FilterNull().Select(team => team.Name).ToArray();

            return save;
        }

        public static void Solve(Dictionary<RoleSpecLevel, int>            tasks,
                                 Dictionary<Employee, List<RoleSpecLevel>> agents,
                                 out Dictionary<RoleSpecLevel, int>        missing,
                                 out List<Employee>                        redundant,
                                 out Dictionary<Employee, RoleSpecLevel>   assignments)
        {
            var n          = tasks.Count + agents.Count + 2;
            var i          = 0;
            var source     = i++;
            var drain      = i++;
            var agentNodes = new Dictionary<Employee, int>();
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
            redundant   = new List<Employee>();
            assignments = new Dictionary<Employee, RoleSpecLevel>();


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

            // max flow solver
            var solver = new FordFulkerson(graph, source, drain);

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("self-aware team settings");
            sb.AppendLine($"\tteam: {Team.Name}");
            sb.AppendLine($"\tactive: {Active}");
            sb.AppendLine($"\tfire: {FireWhenRedundant}");
            sb.AppendLine($"\tidle: {OnlyDrawIdle}");
            sb.AppendLine($"\tspace: {OnlyDrawIfSpace}");
            sb.AppendLine($"\trelease: {TeamToReleaseTo?.Name ?? "None"}");
            var draw = TeamsToDrawFrom.Any()
                ? TeamsToDrawFrom
                 .Select(team => team.Name)
                 .Aggregate((a, b) => $"{a}, {b}")
                : "None";
            sb.AppendLine($"\tdraw: {draw}");

            return sb.ToString();
        }

        private bool TryDrawMissing(ref Dictionary<RoleSpecLevel, int> missing)
        {
            throw new NotImplementedException();
        }

        private bool TryFireRedundant(ref List<Employee> redundant)
        {
            foreach (var employee in redundant)
            {
                employee.MyActor.Fire(false);
            }

            GameSettings.Instance.RegisterStat("Fired", redundant.Count);
            return true;
        }

        private bool TryHireMissing(ref Dictionary<RoleSpecLevel, int> missing)
        {
            throw new NotImplementedException();
        }

        private bool TryReleaseRedundant(ref List<Employee> redundant)
        {
            if (TeamToReleaseTo == null)
            {
                return false;
            }

            var HR      = TeamToReleaseTo.SelfAwareHR();
            var desks   = HR.Desks.Count() - HR.Employees.Count();
            var txCount = Mathf.Min(desks, redundant.Count);
            if (txCount > 0)
            {
                var release = redundant.Splice(desks);
                foreach (var employee in release)
                {
                    employee.MyActor.Team = TeamToReleaseTo.Name;
                }

                NotifyReleased(release);

                if (!redundant.Any())
                {
                    return true;
                }
            }

            NotifyFailedToRelease_NoDesks();
            return false;
        }

        public IEnumerable<SoftwareWorkItem> WorkItems(Employee.EmployeeRole role)
        {
            if (role == Employee.EmployeeRole.Designer)
            {
                return Team.WorkItems.OfType<DesignDocument>().Where(item => !item.AllDone(true));
            }

            return Team.WorkItems.OfType<SoftwareAlpha>()
                       .Where(item => !item.AllDone(false,
                                                    role == Employee.EmployeeRole.Artist,
                                                    role == Employee.EmployeeRole.Programmer));
        }

        public struct RoleSpecLevel : IEquatable<RoleSpecLevel>
        {
            public Employee.EmployeeRole Role;
            public string                Spec;
            public int                   Level;

            public RoleSpecLevel(Employee.EmployeeRole role, [NotNull] string spec, int level)
            {
                Role  = role;
                Spec  = spec ?? throw new ArgumentNullException(nameof(spec));
                Level = level;
            }

            public RoleSpecLevel(Employee.EmployeeRole role, SpecLevel specLevel) : this(
                role, specLevel.Spec, specLevel.Level)
            {
            }

            public bool Equals(RoleSpecLevel other)
            {
                return Role == other.Role && Spec == other.Spec && Level == other.Level;
            }

            public override bool Equals(object obj)
            {
                return obj is RoleSpecLevel other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int) Role;
                    hashCode = (hashCode * 397) ^ (Spec != null ? Spec.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Level;
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{Role} :: {Spec} ({Level})";
            }
        }

        public struct SpecLevel : IEquatable<SpecLevel>
        {
            public string Spec;
            public int    Level;

            public static implicit operator SpecLevel(FeatureBase feature)
            {
                return new SpecLevel(feature.Spec, feature.Level);
            }

            public static implicit operator SpecLevel(RoleSpecLevel rsl)
            {
                return new SpecLevel(rsl.Spec, rsl.Level);
            }

            public SpecLevel(string spec, int level)
            {
                Spec  = spec;
                Level = level;
            }

            public bool Equals(SpecLevel other)
            {
                return Spec == other.Spec && Level == other.Level;
            }

            public override bool Equals(object obj)
            {
                return obj is SpecLevel other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Spec != null ? Spec.GetHashCode() : 0) * 397) ^ Level;
                }
            }

            public override string ToString()
            {
                return $"{Spec} ({Level})";
            }
        }
    }
}