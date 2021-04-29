// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR.cs

#define UNITY_ASSERTIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class SelfAwareHR
    {
        private static readonly Dictionary<Team, SelfAwareHR> Settings =
            new Dictionary<Team, SelfAwareHR>();

        public bool Active = true;
        public bool FireWhenRedundant;
        public bool OnlyDrawIdle    = true;
        public bool OnlyDrawIfSpace = true;

        public Team       Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team       TeamToReleaseTo;

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
            throw new NotImplementedException();
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
            save["ReleaseTo"]         = TeamToReleaseTo?.Name ?? "None";
            save["DrawFrom"]          = TeamsToDrawFrom?.FilterNull().Select(team => team.Name).ToArray();

            return save;
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

            Active            = save.Get("Active", false);
            OnlyDrawIdle      = save.Get("OnlyDrawIdle", true);
            OnlyDrawIfSpace   = save.Get("OnlyDrawIfSpace", true);
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