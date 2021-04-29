// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class SelfAwareHR : Writeable
    {
        private static readonly Dictionary<Team, SelfAwareHR> Settings =
            new Dictionary<Team, SelfAwareHR>();

        [SaveField(false)] public bool Active            = true;
        [SaveField(false)] public bool FireWhenRedundant = false;
        [SaveField(true)]  public bool OnlyDrawIdle      = true;
        [SaveField(true)]  public bool OnlyDrawIfSpace   = true;

        public Team       Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team       TeamToReleaseTo;

        protected SelfAwareHR(Team team)
        {
            Team = team;
        }

        private SelfAwareHR()
        {
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

        protected override void SerializeMe(WriteDictionary save, GameReader.NewLoadMode mode, bool checkDIDs)
        {
            // Teams do not inherit Writable, and cannot be referenced uniquely. I guess we're going to have 
            // to reference them by name, which I don't like, but at least the names _should_ be unique.
            save["Team"]      = Team.Name;
            save["DrawFrom"]  = TeamsToDrawFrom?.FilterNull().Select(team => team.Name).ToArray();
            save["ReleaseTo"] = TeamToReleaseTo?.Name;
        }

        protected override object DeserializeMe(WriteDictionary save, bool loading)
        {
            var Teams    = GameSettings.Instance.sActorManager.Teams;
            var teamName = save.Get<string>("Team");
            if (teamName.IsNullOrEmpty() ||
                !Teams.TryGetValue(teamName, out Team))
            {
                Console.LogError(
                    $"failed to load team '{teamName ?? "NULL"}' for self-aware HR settings, settings will be reset.");
                return null;
            }

            var teamToReleaseTo = save.Get<string>("ReleaseTo");
            if (!teamToReleaseTo.IsNullOrEmpty())
            {
                Teams.TryGetValue(teamName, out TeamToReleaseTo);
            }

            var teamsToDrawFrom = save.Get<string[]>("DrawFrom");
            if (!teamsToDrawFrom.IsNullOrEmpty())
            {
                TeamsToDrawFrom = new List<Team>();
                foreach (var name in teamsToDrawFrom)
                {
                    if (Teams.TryGetValue(name, out var team))
                    {
                        TeamsToDrawFrom.Add(team);
                    }
                }
            }

            return this;
        }

        protected override bool WriteDID()
        {
            return false;
        }

        internal static WriteDictionary[] Serialize()
        {
            return Settings.Values.Select(instance => instance.SerializeThis(GameReader.NewLoadMode.Any, false))
                           .ToArray();
        }

        internal static void Deserialize(WriteDictionary[] data)
        {
            foreach (var datum in data)
            {
                try
                {
                    var instance = new SelfAwareHR().DeserializeThis(datum, true) as SelfAwareHR;
                    if (instance != null && instance.Team != null)
                    {
                        Settings[instance.Team] = instance;
                    }
                }
                catch (Exception ex)
                {
                    Console.LogError($"error deserializing self-aware HR instance: ${ex.Message}\n${ex.StackTrace}");
                }
            }
        }
    }
}