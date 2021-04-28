// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareInstance.cs

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SelfAwareHR
{
    public class SelfAwareInstance
    {
        private static readonly Dictionary<Team, SelfAwareInstance> Settings =
            new Dictionary<Team, SelfAwareInstance>();

        public bool Active;
        public bool FireWhenRedundant = false;
        public bool OnlyDrawIdle = true;
        public bool OnlyDrawIfSpace = true;
        public Team Team;
        public List<Team> TeamsToDrawFrom = new List<Team>();
        public Team TeamToReleaseTo = null;

        protected SelfAwareInstance(Team team)
        {
            Team = team;
            Active = true;
        }

        public static SelfAwareInstance For(Team team)
        {
            Assert.IsNotNull(team, "team != null");

            if (Settings.TryGetValue(team, out var teamSettings))
            {
                return teamSettings;
            }

            teamSettings = new SelfAwareInstance(team);
            Settings.Add(team, teamSettings);

            return teamSettings;
        }

        internal void Optimize()
        {
            throw new NotImplementedException();
        }
    }
}