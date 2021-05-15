// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/SelfAwareHRManager.cs

#define DEBUG_OPTIMIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SelfAwareHR.Solver;
using SelfAwareHR.Utilities;
using UnityEngine;
using Console = DevConsole.Console;
using Random = UnityEngine.Random;

namespace SelfAwareHR
{
    public class SelfAwareHRManager
    {
        private static readonly Dictionary<Team, SelfAwareHRManager> Settings =
            new Dictionary<Team, SelfAwareHRManager>();

        private Dictionary<Actor, RoleSpecLevel> _idealAssignments;
        private SDateTime                        _lastAssignmentOptimization;
        private OptimizationLog                  _optimizationLog;

        public bool       Active;
        public bool       ArtTasks;
        public bool       DesignTasks;
        public bool       FireWhenRedundant;
        public bool       OnlyDrawIdle    = true;
        public bool       OnlyDrawIfSpace = true;
        public bool       ProgrammingTasks;
        public Team       Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team       TeamToReleaseTo;

        protected SelfAwareHRManager(Team team)
        {
            Team = team;
        }

        private SelfAwareHRManager(WriteDictionary save)
        {
            if (!DeserializeMe(save))
            {
                throw new Exception("deserializing failed");
            }
        }

        public Dictionary<Actor, RoleSpecLevel> Assignments
        {
            get
            {
                if (_idealAssignments == null || _lastAssignmentOptimization.IsDistanceBigger(SDateTime.Now(), 60))
                {
                    OptimizeAssignments(_idealAssignments == null, true);
                }

                return _idealAssignments;
            }
        }

        // todo: may want to cache the available pcs, but they are already cached at room level...
        public IEnumerable<Furniture> Desks =>
            Rooms.SelectMany(r => r.GetFurniture("Computer")).Where(c => c.ComputerChair != null);

        public IEnumerable<Actor> Employees => Team.GetEmployeesDirect();

        public List<Actor> IdleEmployees
        {
            get { return Employees.FilterNull().Where(a => a.IsIdle).Except(Assignments.Keys).ToList(); }
        }

        // todo: may want to cache allowable rooms
        public IEnumerable<Room> Rooms =>
            GameSettings.Instance.sRoomManager.Rooms.Where(r => r.CompatibleWithTeam(Team));


        private static void DebugAssignment(Dictionary<RoleSpecLevel, int>         allTasks,
                                            Dictionary<RoleSpecLevel, int>         missing,
                                            Dictionary<Actor, List<RoleSpecLevel>> allAgents,
                                            List<Actor>                            redundant,
                                            Dictionary<Actor, RoleSpecLevel>       assignments)
        {
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
                    $"    {agent.Key.employee.FullName}: {(assigned ? assignments[agent.Key].ToString() : "NOT ASSIGNED")} {(_redundant ? "REDUNDANT" : "")}");
            }
#endif
        }

        internal static void Deserialize(WriteDictionary[] data)
        {
            Log.DebugSerializing("deserializing self-aware team settings");
            Settings.Clear();
            foreach (var datum in data)
            {
                try
                {
                    var instance = new SelfAwareHRManager(datum);
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

        public Dictionary<Actor, List<RoleSpecLevel>> EmployeeSpecs(Employee.EmployeeRole      role,
                                                                    IEnumerable<RoleSpecLevel> specs)
        {
            var agents = Employees.Where(a => a.employee.IsRole(role));
            return EmployeeSpecs(agents, specs);
        }

        public Dictionary<Actor, List<RoleSpecLevel>> EmployeeSpecs(IEnumerable<Actor>         actors,
                                                                    IEnumerable<RoleSpecLevel> specs)
        {
            var _actors = new Dictionary<Actor, List<RoleSpecLevel>>();
            foreach (var actor in actors)
            {
                var skills = new List<RoleSpecLevel>();
                foreach (var spec in specs)
                {
                    if (actor.employee.GetRoleOrNatural(false, true)                  == spec.Role &&
                        actor.employee.GetSpecialization(spec.Role, spec.Spec, actor) >= spec.Level)
                    {
                        skills.Add(spec);
                    }
                }

                _actors.Add(actor, skills);
            }

            return _actors;
        }

        public static SelfAwareHRManager For(Team team)
        {
            if (Settings.TryGetValue(team, out var teamSettings))
            {
                return teamSettings;
            }

            teamSettings = new SelfAwareHRManager(team);
            Settings.Add(team, teamSettings);

            return teamSettings;
        }

        public void GetTasksAndAgents(Employee.EmployeeRole                      role,
                                      bool                                       forceFull,
                                      ref Dictionary<RoleSpecLevel, int>         tasks,
                                      ref Dictionary<Actor, List<RoleSpecLevel>> agents)
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

        internal void OptimizeAssignments(bool force           = false,
                                          bool assignmentsOnly = false)
        {
            if (!force && !SDateTime.Now().IsDistanceBigger(_lastAssignmentOptimization, 60))
            {
                // skipping optimization if done in last hour.
                return;
            }

            var                            allTasks  = new Dictionary<RoleSpecLevel, int>();
            var                            allAgents = new Dictionary<Actor, List<RoleSpecLevel>>();
            Dictionary<RoleSpecLevel, int> missing;
            List<Actor>                    redundant;

            // we solve optimal task assignment for all roles that are either assigned or otherwise present,
            // because we need to know who is assigned/available.
            if (DesignTasks || Employees.Any(e => e.employee.IsRole(Employee.EmployeeRole.Designer)))
            {
                GetTasksAndAgents(Employee.EmployeeRole.Designer, true, ref allTasks, ref allAgents);
            }

            if (ProgrammingTasks || Employees.Any(e => e.employee.IsRole(Employee.EmployeeRole.Programmer)))
            {
                GetTasksAndAgents(Employee.EmployeeRole.Programmer, true, ref allTasks, ref allAgents);
            }

            if (ArtTasks || Employees.Any(e => e.employee.IsRole(Employee.EmployeeRole.Artist)))
            {
                GetTasksAndAgents(Employee.EmployeeRole.Artist, true, ref allTasks, ref allAgents);
            }

            // no employees or no tasks, nothing to solve.
            if (allTasks.Any() && allAgents.Any())
            {
                OptimalAssignment.Solve(allTasks, allAgents, out missing, out redundant, out var assignments);
                DebugAssignment(allTasks, missing, allAgents, redundant, assignments);
                _idealAssignments           = assignments;
                _lastAssignmentOptimization = SDateTime.Now();
            }
            else
            {
                missing   = allTasks;
                redundant = allAgents.Keys.ToList();
            }

            if (assignmentsOnly)
            {
                return;
            }

            var anyChange = false;
            _optimizationLog = new OptimizationLog(Team);
            if (missing.Any())
            {
                // filter missing specializations down to roles we actually care about.
                missing = missing.Where(p =>
                                            p.Key.Role == Employee.EmployeeRole.Designer   && DesignTasks      ||
                                            p.Key.Role == Employee.EmployeeRole.Programmer && ProgrammingTasks ||
                                            p.Key.Role == Employee.EmployeeRole.Artist     && ArtTasks)
                                 .ToDictionary();

                if (missing.Any())
                {
                    TryDrawMissing(ref missing, ref anyChange);
                }
            }

            if (redundant.Any())
            {
                // you're all fired!
                TryReleaseRedundant(ref redundant, ref anyChange);
            }

            if (anyChange)
            {
                // update the desired counts for normal HR management.
                UpdateDesiredCounts();

                // run optimization again to assign new employees.
                OptimizeAssignments(true, true);
            }
        }


        public Dictionary<RoleSpecLevel, int> RequiredSpecs(Employee.EmployeeRole role, bool forceFull = false)
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
                        Log.DebugSpecs(
                            $"{role} :: {item.Name} :: {feature.Name} :: {feature.Spec} ({feature.Level}) :: {work}");

                        totalWork               += work;
                        perLevel[feature.Level] += work;
                    }
                }
            }

            // get the total number of employees this team would
            // need, then calculate the number of employees per 
            // spec.
            var totalCount = GameData.GetOptimalEmployees(Mathf.Max(1, Mathf.CeilToInt(totalWork)));
            var specCount  = new Dictionary<RoleSpecLevel, int>();
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
                        specCount.Add(new RoleSpecLevel(role, spec.Key, i), count);
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

        public void TryDrawMissing(ref Dictionary<RoleSpecLevel, int> missing, ref bool anyChange)
        {
            if (!(TryTransferMissing(ref missing, ref anyChange) || TryHireMissing(ref missing, ref anyChange)))
            {
                _optimizationLog.WarnUnderstaffed(missing);
            }
        }

        private bool TryFireRedundant(ref List<Actor> redundant, ref bool anyChange)
        {
            if (!FireWhenRedundant)
            {
                return false;
            }

            var fired             = new List<Actor>();
            var budgetConstrained = false;
            var spent             = 0f;

            while (redundant.Any())
            {
                var employee  = redundant.First();
                var severance = employee.GetBenefitValue("Severance pay") * employee.GetMonthlySalary();
                if (!GameSettings.Instance.MyCompany.CanMakeTransaction(-severance))
                {
                    budgetConstrained = true;
                    break;
                }

                GameSettings.Instance.RegisterStat("Fired", 1);
                spent += severance;
                fired.Add(employee);
                redundant.Remove(employee);
                employee.Fire(false);
            }

            if (fired.Any())
            {
                anyChange = true;
                _optimizationLog.LogFire(fired, spent);
            }

            if (budgetConstrained && redundant.Any())
            {
                _optimizationLog.WarnNoBudget(redundant.Count, false);
            }

            return !redundant.Any();
        }

        private bool TryHireMissing(ref Dictionary<RoleSpecLevel, int> missing, ref bool anyChange)
        {
            if (!missing.Any())
            {
                return false;
            }

            if (!Team.CheckHRLevel(3))
            {
                return false;
            }

            var HR                = Team.HR;
            var availableDesks    = Desks.Count() - Employees.Count();
            var budgetConstrained = false;
            var deskConstrained   = OnlyDrawIfSpace && availableDesks <= 0;
            var hired             = new Dictionary<Actor, RoleSpecLevel>();
            var spent             = 0f;

            if (HR.Spent >= HR.Budget)
            {
                budgetConstrained = true;
                goto Done;
            }

            if (OnlyDrawIfSpace && availableDesks <= 0)
            {
                deskConstrained = true;
                goto Done;
            }

            var leaderSkill = Team.Leader.employee.GetSkill(Employee.EmployeeRole.Lead);
            var geniusChance = leaderSkill.MapRange(0, 1, .01f, .1f);
            var costFactor = leaderSkill.MapRange(0, 1, 1, .5f) / GameSettings.Instance.Environment.EmployeePool / 4;
            var minCompatibility = Mathf.Min(Team.MinCompatibility, 1 + leaderSkill * .5f);
            const int maxIterations = 20;

            Team.DisableCompatibilityUpdate = true;
            foreach (var vacancy in missing.Keys.ToList())
            {
                // vanilla makes some weird decisions in calculating costs, I've tried to reconstruct and normalize the logic.
                // we get the cost for the minimum hire, and assume that would lead to 4 candidates. We then draw candidates 
                // until the vacancies are filled, paying the same cost for each.
                // That leads to lower costs for small numbers of hires (<=4), and higher costs for larger batches. Note that 
                // our batches are by definition smaller, because we batch by Role, Specialization and Level - vanilla only 
                // batches by Role.
                var wageBracket = (Employee.WageBracket) Mathf.Clamp(vacancy.Level - 1, 0, 2);
                var hiringCost  = HireWindow.GetFinalCost(1, (int) wageBracket, true, true) * costFactor;
                var iteration   = 0;

                while (missing[vacancy] > 0 &&
                       iteration++      < maxIterations)
                {
                    if (HR.Spent + hiringCost >= HR.Budget)
                    {
                        budgetConstrained = true;
                        goto Done;
                    }

                    if (OnlyDrawIfSpace && availableDesks <= 0)
                    {
                        deskConstrained = true;
                        goto Done;
                    }

                    var candidate = new Employee(SDateTime.Now(), vacancy.Role, Random.value > .5f, wageBracket,
                                                 GameSettings.Instance.Personalities, false, vacancy.Spec, null,
                                                 Team, leaderSkill, geniusChance);
                    HR.Spent += hiringCost;
                    spent    += hiringCost;

                    GameSettings.Instance.MyCompany.MakeTransaction(-hiringCost, Company.TransactionCategory.Hire);
                    if (candidate.GetSpecialization(vacancy.Role, vacancy.Spec) >= vacancy.Level &&
                        Team.GetMinCompatibility(candidate)                     >= minCompatibility)
                    {
                        var actor = GameSettings.Instance.SpawnActor(candidate.Female, false);
                        hired.Add(actor, vacancy);
                        missing[vacancy]--;
                        availableDesks -= 1;
                        GameSettings.Instance.RegisterStat("Hired", 1);
                        actor.employee = candidate;
                        actor.Team     = Team.Name;
                        candidate.SetRoles(Employee.RoleBit.AnyRole, Employee.RoleBit.None);
                        Team.Leader.employee.ChangeSkill(Employee.EmployeeRole.Lead, .001f, false);
                    }
                }
            }

            Done: // goto? yes. shut up.

            Team.DisableCompatibilityUpdate = false;
            if (hired.Any())
            {
                anyChange = true;
                _optimizationLog.LogHire(hired, spent);
                Team.CalculateCompatibility();
            }

            missing = missing.Where(p => p.Value > 0).ToDictionary();
            if (deskConstrained)
            {
                _optimizationLog.WarnNoSpace(missing.Aggregate(0, (acc, cur) => acc + cur.Value), true);
            }

            if (budgetConstrained)
            {
                _optimizationLog.WarnNoBudget(missing.Aggregate(0, (acc, cur) => acc + cur.Value), true);
            }

            return !missing.Any();
        }

        public void TryReleaseRedundant(ref List<Actor> redundant, ref bool anyChange)
        {
            if (!(TryTransferRedundant(ref redundant, ref anyChange) || TryFireRedundant(ref redundant, ref anyChange)))
            {
                _optimizationLog.WarnOverstaffed(redundant.Count);
            }
        }

        private bool TryTransferMissing(ref Dictionary<RoleSpecLevel, int> missing, ref bool anyChange)
        {
            if (TeamsToDrawFrom.IsNullOrEmpty())
            {
                return false;
            }

            var desks            = Desks.Count() - Employees.Count();
            var spaceConstrained = false;
            if (OnlyDrawIfSpace && desks <= 0)
            {
                var count = missing.Aggregate(0, (acc, cur) => cur.Value + acc);
                _optimizationLog.WarnNoSpace(count, false);
                return false;
            }

            var candidates = TeamsToDrawFrom.SelectMany(t => OnlyDrawIdle
                                                            ? t.SelfAwareHR().IdleEmployees
                                                            : t.SelfAwareHR().Employees);

            if (candidates.IsNullOrEmpty())
            {
                _optimizationLog.WarnNoApplicants(missing, false);
                return false;
            }

            var candidateSpecs = EmployeeSpecs(candidates, missing.Keys.Distinct());
            OptimalAssignment.Solve(missing, candidateSpecs, out var unfulfilled, out _,
                                    out var assignments);

            var reassigned = new Dictionary<Actor, RoleSpecLevel>();
            while (assignments.Any())
            {
                if (OnlyDrawIfSpace && desks <= 0)
                {
                    spaceConstrained = true;
                    break;
                }

                var assignment = assignments.Pop();
                assignment.Key.Team = Team.Name;
                reassigned.Add(assignment.Key, assignment.Value);
                desks--;
            }

            if (spaceConstrained)
            {
                _optimizationLog.WarnNoSpace(assignments.Count, false);
            }

            if (reassigned.Any())
            {
                anyChange = true;
                _optimizationLog.LogTransfer(reassigned);
            }

            // planned reassignments that did not take place go back on the missing heap.
            if (assignments.Any())
            {
                foreach (var assignment in assignments)
                {
                    if (unfulfilled.ContainsKey(assignment.Value))
                    {
                        unfulfilled[assignment.Value] += 1;
                    }
                    else
                    {
                        unfulfilled[assignment.Value] = 1;
                    }
                }
            }

            missing = unfulfilled;
            return !missing.Any();
        }

        private bool TryTransferRedundant(ref List<Actor> redundant, ref bool anyChange)
        {
            if (TeamToReleaseTo == null)
            {
                return false;
            }

            // todo: add OnlyReleaseIfSpace toggle?
            var toHR             = TeamToReleaseTo.SelfAwareHR();
            var desks            = toHR.Desks.Count() - toHR.Employees.Count();
            var spaceConstrained = false;
            var released         = new List<Actor>();

            while (redundant.Any())
            {
                if (desks <= 0)
                {
                    spaceConstrained = true;
                    break;
                }

                var employee = redundant.Pop();
                released.Add(employee);
                employee.Team = TeamToReleaseTo.Name;
            }

            if (released.Any())
            {
                anyChange = true;
                _optimizationLog.LogRelease(released, TeamToReleaseTo);
            }

            if (spaceConstrained && redundant.Any())
            {
                _optimizationLog.WarnNoSpace(redundant.Count, false);
            }

            return !redundant.Any();
        }

        public static void Update(object obj, EventArgs args)
        {
            if (!Mod.Instance.enabled)
            {
                return;
            }

            var start = DateTime.Now;
            foreach (var team in GameSettings.Instance.sActorManager.Teams.Values)
            {
                var HR     = team.SelfAwareHR();
                var leader = team.Leader;
                if (HR.Active                                              &&
                    (HR.DesignTasks || HR.ProgrammingTasks || HR.ArtTasks) &&
                    team.CheckHRLevel(3)                                   &&
                    leader != null                                         &&
                    leader.enabled                                         &&
                    !leader.employee.Fired)
                {
                    var start2 = DateTime.Now;
                    HR.OptimizeAssignments(true);
                    var elapsed2 = DateTime.Now - start2;
                    Log.DebugUpdates($"updating {team.Name} took {elapsed2.TotalMilliseconds:N0}ms");
                }
            }

            var elapsed = DateTime.Now - start;
            Log.DebugUpdates($"updating all self-aware HR took {elapsed.TotalMilliseconds:N0}ms");
        }

        private void UpdateDesiredCounts()
        {
            var counts = Team.HR.MaxEmployees;
            var inputs = HUD.Instance.TeamWindow.autoWindow.HREmployeeCount;
            var updateInputs = HUD.Instance.TeamWindow.autoWindow.Teams.Length  == 1 &&
                               HUD.Instance.TeamWindow.autoWindow.Teams.First() == Team;

            if (ProgrammingTasks)
            {
                var count = Employees.Count(e => e.employee.HiredFor == Employee.EmployeeRole.Programmer);
                counts[0] = count;
                if (updateInputs)
                {
                    inputs[0].text = count.ToString();
                }
            }

            if (DesignTasks)
            {
                var count = Employees.Count(e => e.employee.HiredFor == Employee.EmployeeRole.Designer);
                counts[1] = count;
                if (updateInputs)
                {
                    inputs[1].text = count.ToString();
                }
            }

            if (ArtTasks)
            {
                var count = Employees.Count(e => e.employee.HiredFor == Employee.EmployeeRole.Artist);
                counts[2] = count;
                if (updateInputs)
                {
                    inputs[2].text = count.ToString();
                }
            }
        }

        public IEnumerable<SoftwareWorkItem> WorkItems(Employee.EmployeeRole role)
        {
            if (role == Employee.EmployeeRole.Designer)
            {
                return Team.WorkItems.OfType<DesignDocument>().Where(item => !item.AllDone(true));
            }

            return Team.WorkItems.OfType<SoftwareAlpha>()
                       .Where(item => item.InBeta || !item.AllDone(false,
                                                                   role == Employee.EmployeeRole.Artist,
                                                                   role == Employee.EmployeeRole.Programmer));
        }
    }
}