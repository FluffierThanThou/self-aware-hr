// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Mod.cs

using System;
using System.Collections.Generic;
using System.Linq;
using SelfAwareHR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using static SelfAwareHR.Utilities.UIHelpers;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class Mod : ModBehaviour
    {
        public const  string HRPanelPath = "HRWindow/ContentPanel/LabeledPanel/HRPanel/Panel";
        public const  string Level3Path  = HRPanelPath + "/Level3";
        public static Mod    Instance;

        private Text          _autoSpecsLabel;
        private Toggle        _autoSpecsToggle;
        private bool          _dirty = true;
        private Button        _drawFromTeamsButton;
        private Text          _drawFromTeamsLabel;
        private Text          _fireWhenRedundantLabel;
        private Toggle        _fireWhenRedundantToggle;
        private bool          _ignoreUpdates;
        private Text          _onlyDrawIfIdleLabel;
        private Toggle        _onlyDrawIfIdleToggle;
        private Text          _onlyDrawIfSpaceLabel;
        private Toggle        _onlyDrawIfSpaceToggle;
        private int           _prevHRLevel = int.MinValue;
        private Button        _releaseToTeamButton;
        private Text          _releaseToTeamLabel;
        private Text          _selfAwareHRLabel;
        private StarCounter   _starCounter;
        private Toggle        _teamRolesArtToggle;
        private Toggle        _teamRolesDesignToggle;
        private Text          _teamRolesLabel;
        private Toggle        _teamRolesProgrammingToggle;
        private RectTransform _teamRolesRow;
        private Team[]        _teams;
        private Button        _triggerOptimizationButton;

        private static AutomationWindow AutomationWindow => HUD.Instance.TeamWindow.autoWindow;

        public bool ExtraFieldsAdded => HRPanel.GetComponentInChildren<Sentinel>() != null;

        public RectTransform HRPanel => WindowManager.FindElementPath(HRPanelPath);

        public List<SelfAwareHRManager> Settings => Teams?.Select(team => SelfAwareHRManager.For(team)).ToList();

        protected Team[] Teams
        {
            get => _teams;
            set => _teams = value ?? new Team[0];
        }

        public List<Team> TeamsToDrawFrom => Settings.SelectMany(hr => hr.TeamsToDrawFrom).Distinct().ToList();

        public Team TeamToReleaseTo => Settings.SelectNotNull(hr => hr.TeamToReleaseTo).Mode();

        // if I understand correctly, this causes selected teams to be on top of the list next time? 
        public string Type => "SelfAwareHR";

        public void AddExtraFields(object sender, EventArgs eventArgs)
        {
            if (!enabled || ExtraFieldsAdded)
            {
                return;
            }

            _ignoreUpdates = true;

            // create new UI elements
            _selfAwareHRLabel = CreateLocalizedText("selfAwareHR");
            _selfAwareHRLabel.fontStyle = FontStyle.Bold;
            _autoSpecsLabel = CreateLocalizedText("autoSpecs");
            _autoSpecsToggle = CreateToggle(OnAutoSpecsChanged);
            _teamRolesLabel = CreateLocalizedText("teamRoles");
            _teamRolesDesignToggle = CreateToggle(OnTeamRolesDesignChanged, label: "Design".Loc());
            _teamRolesProgrammingToggle = CreateToggle(OnTeamRolesProgrammingChanged, label: "Programming".Loc());
            _teamRolesArtToggle = CreateToggle(OnTeamRolesArtChanged, label: "Art".Loc());
            _teamRolesRow = CreateRowLayout(_teamRolesDesignToggle, _teamRolesProgrammingToggle, _teamRolesArtToggle);
            _drawFromTeamsLabel = CreateLocalizedText("drawFromTeams");
            _drawFromTeamsButton = CreateButton(OnDrawFromTeamsButtonClick);
            _onlyDrawIfSpaceLabel = CreateLocalizedText("onlyDrawIfSpace");
            _onlyDrawIfSpaceToggle = CreateToggle(OnOnlyDrawIfSpaceChanged);
            _onlyDrawIfIdleLabel = CreateLocalizedText("onlyDrawIfIdle");
            _onlyDrawIfIdleToggle = CreateToggle(OnOnlyDrawIfIdleChanged);
            _releaseToTeamLabel = CreateLocalizedText("releaseToTeam");
            _releaseToTeamButton = CreateButton(OnReleaseToTeamButtonClick);
            _fireWhenRedundantLabel = CreateLocalizedText("fireWhenRedundant");
            _fireWhenRedundantToggle = CreateToggle(OnFireWhenRedundantChanged);
            _triggerOptimizationButton = CreateButton(OnTriggerOptimizationButtonClick,
                                                      "triggerOptimizationLabel".Loc(),
                                                      "triggerOptimizationTip".Loc());

            // attach elements to HR window
            // find the HR window panel to append to
            var hrPanel = WindowManager.FindElementPath(HRPanelPath);

            // create a copy of the 3 star banner
            var level3 = Instantiate(WindowManager.FindElementPath(Level3Path));
            _starCounter = level3.GetComponentInChildren<StarCounter>();

            // start adding our custom stuff
            hrPanel.AddComponents(_selfAwareHRLabel, level3);
            hrPanel.AddComponents(_autoSpecsLabel, _autoSpecsToggle);
            hrPanel.AddComponents(_teamRolesLabel, _teamRolesRow);
            hrPanel.AddComponents(_drawFromTeamsLabel, _drawFromTeamsButton);
            hrPanel.AddComponents(_releaseToTeamLabel, _releaseToTeamButton);
            hrPanel.AddComponents(_onlyDrawIfSpaceLabel, _onlyDrawIfSpaceToggle);
            hrPanel.AddComponents(_onlyDrawIfIdleLabel, _onlyDrawIfIdleToggle);
            hrPanel.AddComponents(_fireWhenRedundantLabel, _fireWhenRedundantToggle);
            // adding an empty text element, because of the above mentioned two-column layout
            hrPanel.AddComponents(CreateText(""), _triggerOptimizationButton);

            // add a sentinel to the hrPanel to get enabled/disabled notifications, we can't use
            // this behaviours' lifecycle directly, because it is attached to something else, 
            // not actually sure what (mod container? base component?).
            var sentinel = hrPanel.gameObject.AddComponent<Sentinel>();

            // we can't do our (re-)initialization on enable, because the components
            // are activated _before_ the teams are set. Instead, we'll use this trigger
            // to set a flag, and do our init on the next update.
            // Technically, that means we may have a frame with incorrect information,
            // but nobody is going to notice that.
            sentinel.onEnable += SetDirty;

            // it turns out that we also do actually need an on-update trigger to react
            // to changes outside our control. I was somewhat surprised that the game 
            // also uses an Update() hook to continuously update certain dynamic UI elements.
            sentinel.onUpdate += CheckDirty;

            _ignoreUpdates = false;
        }

        public void Awake()
        {
            // add event listeners for initialization and normal operation.
            GameSettings.IsDoneLoadingGame += AddExtraFields;
            Instance                       =  this;

            if ((SelectorController.Instance?.DoneLoading ?? false) && !ExtraFieldsAdded)
            {
                // attempt adding our stuff if we're toggled on after game load event has already triggered
                AddExtraFields(null, null);
            }
        }

        // called every update through our sentinel object, we need to reinitialize
        // our data for different teams whenever the window is (re-)opened, but also
        // react to some changes that affect us and may have happened outside our 
        // control;
        // 
        // - Team leader changes,
        // 
        // and, when we add UI elements for current specs, required specs, desks, etc;
        // - Adding/removing projects
        // - Adding/removing employees
        // - Employees gaining skills
        // - Adding/removing rooms/desks
        public void CheckDirty()
        {
            if (!enabled)
            {
                return;
            }

            if (_dirty)
            {
                UpdateControls();
                return;
            }

            if (!Teams.SequenceEqual(AutomationWindow.Teams))
            {
                UpdateControls();
                return;
            }

            var level = Teams?.MaxSafeInt(team => team.GetHRLevel()) ?? 0;
            if (_prevHRLevel != level)
            {
                UpdateStarCounter();
            }
        }

        public override void OnActivate()
        {
            Console.Log("Self-Aware HR activated");
            enabled                =  true;
            TimeOfDay.OnHourPassed += SelfAwareHRManager.Update;
        }

        public void OnAutoSpecsChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).Active = value;
            }

            UpdateActiveInputs();
        }

        public override void OnDeactivate()
        {
            Console.Log("Self-Aware HR deactivated");
            TryRemoveExtraFields();
            enabled                =  false;
            TimeOfDay.OnHourPassed -= SelfAwareHRManager.Update;
        }

        public void OnDrawFromTeamsButtonClick()
        {
            HUD.Instance.TeamSelectWindow.Show(false,
                                               TeamsToDrawFrom.Select(team => team.Name).ToHashSet(),
                                               SetTeamsToDrawFrom,
                                               Type);
        }

        public void OnFireWhenRedundantChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).FireWhenRedundant = value;
            }
        }

        public void OnOnlyDrawIfIdleChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).OnlyDrawIdle = value;
            }
        }

        public void OnOnlyDrawIfSpaceChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).OnlyDrawIfSpace = value;
            }
        }

        public void OnReleaseToTeamButtonClick()
        {
            HUD.Instance.TeamSelectWindow.Show(true, TeamToReleaseTo?.Name, SetTeamToReleaseTo, Type);
        }

        private void OnTeamRolesArtChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).ArtTasks = value;
            }

            UpdateActiveInputs();
        }

        private void OnTeamRolesDesignChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).DesignTasks = value;
            }

            UpdateActiveInputs();
        }

        private void OnTeamRolesProgrammingChanged(bool value)
        {
            if (_ignoreUpdates)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).ProgrammingTasks = value;
            }

            UpdateActiveInputs();
        }

        public void OnTriggerOptimizationButtonClick()
        {
            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).OptimizeAssignments(true);
            }
        }

        public void SetDirty()
        {
            _dirty = true;
        }

        public void SetTeamsToDrawFrom(string[] selectedTeams)
        {
            var teamsToDrawFrom = selectedTeams.Select(GameSettings.GetTeam).ToList();
            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).TeamsToDrawFrom = teamsToDrawFrom.Where(t => t != team).ToList();
            }

            UpdateLabels();
        }

        public void SetTeamToReleaseTo(string[] selectedTeams)
        {
            Team teamToReleaseTo;
            switch (selectedTeams.Length)
            {
                case 0:
                    teamToReleaseTo = null;
                    break;
                case 1:
                    teamToReleaseTo = GameSettings.GetTeam(selectedTeams[0]);
                    break;
                default:
                    teamToReleaseTo = null;
                    Console.LogError("selected multiple teams to release to. That should not be possible.");
                    break;
            }

            foreach (var team in Teams)
            {
                SelfAwareHRManager.For(team).TeamToReleaseTo = teamToReleaseTo == team ? null : teamToReleaseTo;
            }

            UpdateLabels();
        }

        public void TryRemoveExtraFields()
        {
            // I honestly have no idea what this will do to the layout. 
            // The game gives you ample warning that disabling code mods
            // is a bad idea though, so yeah - you've been warned.
            _autoSpecsLabel?.gameObject.Destroy();
            _autoSpecsToggle?.gameObject.Destroy();
            _drawFromTeamsLabel?.gameObject.Destroy();
            _drawFromTeamsButton?.gameObject.Destroy();
            _onlyDrawIfIdleLabel?.gameObject.Destroy();
            _onlyDrawIfIdleToggle?.gameObject.Destroy();
            _onlyDrawIfSpaceLabel?.gameObject.Destroy();
            _onlyDrawIfSpaceToggle?.gameObject.Destroy();
            _releaseToTeamLabel?.gameObject.Destroy();
            _releaseToTeamButton?.gameObject.Destroy();
            _fireWhenRedundantLabel?.gameObject.Destroy();
            _fireWhenRedundantToggle?.gameObject.Destroy();
            _triggerOptimizationButton?.gameObject.Destroy();
            _teamRolesArtToggle?.gameObject.Destroy();
            _teamRolesDesignToggle?.gameObject.Destroy();
            _teamRolesProgrammingToggle?.gameObject.Destroy();
            _teamRolesLabel?.gameObject.Destroy();
            _teamRolesRow?.gameObject.Destroy();
            _starCounter?.gameObject.Destroy();
        }

        public void UpdateActiveInputs()
        {
            var inputs = HUD.Instance.TeamWindow.autoWindow.HREmployeeCount;
            inputs[0].readOnly = _autoSpecsToggle.isOn && _teamRolesProgrammingToggle.isOn;
            inputs[1].readOnly = _autoSpecsToggle.isOn && _teamRolesDesignToggle.isOn;
            inputs[2].readOnly = _autoSpecsToggle.isOn && _teamRolesArtToggle.isOn;
        }

        public void UpdateControls()
        {
            _ignoreUpdates = true;
            Teams          = AutomationWindow.Teams;
            UpdateStarCounter();
            UpdateToggles();
            UpdateActiveInputs();
            UpdateLabels();
            _ignoreUpdates = false;
            _dirty         = false;
        }

        public void UpdateLabels()
        {
            // update button labels with team names
            _drawFromTeamsButton.SetLabel(TeamsToDrawFrom?.ToArray().GetListAbbrev("Team", team => team.Name));
            _releaseToTeamButton.SetLabel(TeamToReleaseTo?.Name ?? "None");

            // update desks label with desks taken/available
            var desks     = Teams.Sum(t => t.SelfAwareHR().Desks.Count());
            var employees = Teams.Sum(t => t.GetEmployeesDirect().Count);
            _onlyDrawIfSpaceLabel.text = "onlyDrawIfSpaceLabel".Loc() +
                                         "onlyDrawIfSpaceLabelStatus".Loc(desks - employees, desks)
                                                                     .FontColor(Color.gray);
        }

        private void UpdateStarCounter()
        {
            var level = Teams?.MaxSafeInt(team => team.GetHRLevel()) ?? 0;
            _starCounter.NonActiveColor = level >= 3 ? AutomationWindow.ActiveLevel : AutomationWindow.InactiveLevel;
            _starCounter.SetVerticesDirty();
            _prevHRLevel = level;
        }

        public void UpdateToggles()
        {
            // todo: is there a 'mixed' toggle state?
            _autoSpecsToggle.isOn            = Settings.Mode(hr => hr.Active);
            _onlyDrawIfIdleToggle.isOn       = Settings.Mode(hr => hr.OnlyDrawIdle);
            _onlyDrawIfSpaceToggle.isOn      = Settings.Mode(hr => hr.OnlyDrawIfSpace);
            _fireWhenRedundantToggle.isOn    = Settings.Mode(hr => hr.FireWhenRedundant);
            _teamRolesDesignToggle.isOn      = Settings.Mode(hr => hr.DesignTasks);
            _teamRolesProgrammingToggle.isOn = Settings.Mode(hr => hr.ProgrammingTasks);
            _teamRolesArtToggle.isOn         = Settings.Mode(hr => hr.ArtTasks);
        }
    }
}