// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR.cs

#define UNITY_ASSERTIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class SelfAwareHR
    {
        private static readonly Dictionary<Team, SelfAwareHR> Settings =
            new Dictionary<Team, SelfAwareHR>();

        public bool Active = true;
        public bool FireWhenRedundant;
        public bool OnlyDrawIdle = true;
        public bool OnlyDrawIfSpace = true;

        public Team Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team TeamToReleaseTo;

        private SelfAwareHR(WriteDictionary save)
        {
            if (!DeserializeMe(save))
            {
                throw new Exception("deserializing failed");
            }
        }

        protected SelfAwareHR(Team team)
        {
            Team = team;
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

        internal void Optimize()
        {
            Log.Debug($"optimizing: {Team.Name}");
            var optimalDesign = RequiredSpecs(Employee.EmployeeRole.Designer);
            var optimalCode = RequiredSpecs(Employee.EmployeeRole.Programmer);
            var optimalArt = RequiredSpecs(Employee.EmployeeRole.Artist);
        }

        public Dictionary<string, int[]> RequiredSpecs(Employee.EmployeeRole role, bool forceFull = false)
        {
            float totalWork = 0;
            var specWork = new Dictionary<string, float[]>();

            // calculate the total amount of work, and the amount
            // of work per specialization level.
            foreach (var item in WorkItems(role))
            {
                Log.Debug($"\t\t{item.Name} ({item.SoftwareName})");
                foreach (var task in item.Features)
                {
                    if (task.RelevantFor(role, forceFull))
                    {
                        var feature = task.Feature;
                        var work = task.DevTime(role);
                        var perLevel = specWork.GetOrAdd(feature.Spec, spec => new float[4]);

                        totalWork += work;
                        perLevel[feature.Level] += work;
                        Log.Debug($"\t\t\t{feature.Name} ({feature.Level}, {work:F2})");
                    }
                }
            }

            if (specWork.Any())
            {
                Log.Debug($"\t {role}: {totalWork}");
                Log.DebugSpecs(specWork);
            }

            // get the total number of employees this team would
            // need, then calculate the number of employees per 
            // spec.
            var totalCount = GameData.GetOptimalEmployees(Mathf.Max(1, Mathf.CeilToInt(totalWork)));
            var specCount = new Dictionary<string, int[]>();
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
                var counts = specCount.GetOrAdd(spec.Key, _ => new int[4]);
                for (var i = 3; i >= 0; i--)
                {
                    var raw = spec.Value[i] / totalWork * totalCount;
                    var count = Mathf.RoundToInt(raw);
                    var diff = raw - count;

                    if (diff > extra)
                    {
                        extra += 1 - diff;
                        count += 1;
                    }
                    else
                    {
                        extra -= diff;
                    }

                    counts[i] = count;
                }
            }

            if (specCount.Any())
            {
                Log.Debug($"\t{role} count: {totalCount}");
                Log.DebugSpecs(specCount);
            }

            return specCount;
        }

        public IEnumerable<SoftwareWorkItem> WorkItems(Employee.EmployeeRole role)
        {
            if (role == Employee.EmployeeRole.Designer)
            {
                return Team.WorkItems.OfType<DesignDocument>().Where(item => !item.AllDone(true));
            }

            return Team.WorkItems.OfType<SoftwareWorkItem>()
                       .Where(item => !item.AllDone(false,
                                                    role == Employee.EmployeeRole.Artist,
                                                    role == Employee.EmployeeRole.Programmer));
        }

        protected WriteDictionary SerializeMe()
        {
            // Teams do not inherit Writable, and cannot be referenced uniquely. I guess we're going to have 
            // to reference them by name, which I don't like, but at least the names _should_ be unique.
            Log.DebugSerializing($"serializing data for {Team.Name}");
            Log.DebugSerializing(ToString());
            var save = new WriteDictionary();
            save["Team"] = Team.Name;
            save["Active"] = Active;
            save["OnlyDrawIdle"] = OnlyDrawIdle;
            save["OnlyDrawIfSpace"] = OnlyDrawIfSpace;
            save["FireWhenRedundant"] = FireWhenRedundant;
            save["ReleaseTo"] = TeamToReleaseTo?.Name ?? "None";
            save["DrawFrom"] = TeamsToDrawFrom?.FilterNull().Select(team => team.Name).ToArray();

            return save;
        }

        protected bool DeserializeMe(WriteDictionary save)
        {
            var teams = GameSettings.Instance.sActorManager.Teams;
            var teamName = save.Get<string>("Team");

            Log.DebugSerializing($"deserializing data for {teamName}");

            if (teamName.IsNullOrEmpty() ||
                !teams.TryGetValue(teamName, out Team))
            {
                Console.LogError(
                    $"failed to load self-aware settings for '{teamName ?? "NULL"}', settings will be reset. If this keeps happening, please report the issue.");
                return false;
            }

            Active = save.Get("Active", false);
            OnlyDrawIdle = save.Get("OnlyDrawIdle", true);
            OnlyDrawIfSpace = save.Get("OnlyDrawIfSpace", true);
            FireWhenRedundant = save.Get("FireWhenRedundant", false);

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

        internal static WriteDictionary[] Serialize()
        {
            Log.DebugSerializing("serializing self-aware team settings");
            return Settings.Values.Select(instance => instance.SerializeMe())
                           .ToArray();
        }

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
    }
}