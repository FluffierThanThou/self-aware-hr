// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareWindow.cs

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static SelfAwareHR.UIHelpers;
using Console = DevConsole.Console;

namespace SelfAwareHR
{
    public class SelfAwareWindow : ModBehaviour
    {
        public const  string          HRPanelPath = "HRWindow/ContentPanel/LabeledPanel/HRPanel/Panel";
        public const  string          Level3Path  = HRPanelPath + "/Level3";
        public static SelfAwareWindow Instance;

        private Text   _autoSpecsLabel;
        private Toggle _autoSpecsToggle;
        private bool   _dirty = true;
        private Button _drawFromTeamsButton;
        private Text   _drawFromTeamsLabel;
        private bool   _extraFieldsAdded;
        private Text   _fireWhenRedundantLabel;
        private Toggle _fireWhenRedundantToggle;
        private bool   _initializing;
        private Text   _onlyDrawIfIdleLabel;
        private Toggle _onlyDrawIfIdleToggle;
        private Text   _onlyDrawIfSpaceLabel;
        private Toggle _onlyDrawIfSpaceToggle;

        private int         _prevHRLevel = int.MinValue;
        private Button      _releaseToTeamButton;
        private Text        _releaseToTeamLabel;
        private Text        _selfAwareHRLabel;
        private StarCounter _starCounter;
        private Team[]      _teams;
        private Button      _triggerOptimizationButton;

        protected SelfAwareHR[] TeamSettings;

        protected Team[] Teams
        {
            get => _teams;
            set
            {
                _teams       = value ?? new Team[0];
                TeamSettings = _teams.Select(team => SelfAwareHR.For(team)).ToArray();
            }
        }

        public Team[] TeamsToDrawFrom => TeamSettings.SelectMany(hr => hr.TeamsToDrawFrom).Distinct().ToArray();

        public Team TeamToReleaseTo => TeamSettings.SelectNotNull(hr => hr.TeamToReleaseTo).Mode();

        // if I understand correctly, this causes selected teams to be on top of the list next time? 
        public string Type => "SelfAwareHR";

        private static AutomationWindow AutomationWindow => HUD.Instance.TeamWindow.autoWindow;

        public void Awake()
        {
            // add event listeners for initialization and normal operation.
            TimeOfDay.OnDayPassed          += OnDayPassed;
            GameSettings.IsDoneLoadingGame += AddExtraFields;
            Instance                       =  this;

            if (!_extraFieldsAdded && (SelectorController.Instance?.DoneLoading ?? false))
            {
                // attempt adding our stuff if we're toggled on after game load event has already triggered
                AddExtraFields(null, null);
            }
        }

        public void UpdateControls()
        {
            _initializing = true;
            Teams         = AutomationWindow.Teams;
            UpdateStarCounter();
            UpdateToggles();
            UpdateLabels();
            _initializing = false;
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
            _autoSpecsToggle.isOn         = TeamSettings.Mode(hr => hr.Active);
            _onlyDrawIfIdleToggle.isOn    = TeamSettings.Mode(hr => hr.OnlyDrawIdle);
            _onlyDrawIfSpaceToggle.isOn   = TeamSettings.Mode(hr => hr.OnlyDrawIfSpace);
            _fireWhenRedundantToggle.isOn = TeamSettings.Mode(hr => hr.FireWhenRedundant);
        }

        public void UpdateLabels()
        {
            // update button labels with team names
            _drawFromTeamsButton.SetLabel(TeamsToDrawFrom?.GetListAbbrev("Team", team => team.Name));
            _releaseToTeamButton.SetLabel(TeamToReleaseTo?.Name ?? "None");
        }

        public void OnReleaseToTeamButtonClick()
        {
            HUD.Instance.TeamSelectWindow.Show(true, TeamToReleaseTo?.Name, SetTeamToReleaseTo, Type);
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
                SelfAwareHR.For(team).TeamToReleaseTo = teamToReleaseTo;
            }

            UpdateLabels();
        }

        public void OnDrawFromTeamsButtonClick()
        {
            HUD.Instance.TeamSelectWindow.Show(false,
                                               TeamsToDrawFrom.Select(team => team.Name).ToHashSet(),
                                               SetTeamsToDrawFrom,
                                               Type);
        }

        public void SetTeamsToDrawFrom(string[] selectedTeams)
        {
            var teamsToDrawFrom = selectedTeams.Select(GameSettings.GetTeam).ToList();
            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).TeamsToDrawFrom = teamsToDrawFrom;
            }

            UpdateLabels();
        }

        public void OnAutoSpecsChanged(bool value)
        {
            if (_initializing)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).Active = value;
            }
        }

        public override void OnDeactivate()
        {
            Console.Log("Self-Aware HR deactivated");
            TryRemoveExtraFields();
            enabled = false;
        }

        public override void OnActivate()
        {
            Console.Log("Self-Aware HR activated");
            enabled = true;
        }

        public void OnDayPassed(object sender, EventArgs eventArgs)
        {
            if (!enabled)
            {
                return;
            }

            Console.Log("doing some fancy pants shit");
        }

        public void AddExtraFields(object sender, EventArgs eventArgs)
        {
            if (!enabled || _extraFieldsAdded)
            {
                return;
            }

            // create new UI elements
            _selfAwareHRLabel           = CreateLocalizedText("selfAwareHR");
            _selfAwareHRLabel.fontStyle = FontStyle.Bold;
            _autoSpecsLabel             = CreateLocalizedText("autoSpecs");
            _autoSpecsToggle            = CreateToggle(OnAutoSpecsChanged);
            _drawFromTeamsLabel         = CreateLocalizedText("drawFromTeams");
            _drawFromTeamsButton        = CreateButton(OnDrawFromTeamsButtonClick);
            _onlyDrawIfSpaceLabel       = CreateLocalizedText("onlyDrawIfSpace");
            _onlyDrawIfSpaceToggle      = CreateToggle(OnOnlyDrawIfSpaceChanged);
            _onlyDrawIfIdleLabel        = CreateLocalizedText("onlyDrawIfIdle");
            _onlyDrawIfIdleToggle       = CreateToggle(OnOnlyDrawIfIdleChanged);
            _releaseToTeamLabel         = CreateLocalizedText("releaseToTeam");
            _releaseToTeamButton        = CreateButton(OnReleaseToTeamButtonClick);
            _fireWhenRedundantLabel     = CreateLocalizedText("fireWhenRedundant");
            _fireWhenRedundantToggle    = CreateToggle(OnFireWhenRedundantChange);
            _triggerOptimizationButton = CreateButton(OnTriggerOptimizationButtonClick,
                                                      "triggerOptimizationLabel".Loc(),
                                                      "triggerOptimizationDesc".Loc());

            // attach elements to HR window
            // find the HR window panel to append to
            var hrPanel = WindowManager.FindElementPath(HRPanelPath);

            // create a copy of the 3 star banner
            var level3 = Instantiate(WindowManager.FindElementPath(Level3Path));
            _starCounter = level3.GetComponentInChildren<StarCounter>();

            // start adding our custom stuff
            // Note that "AppendLine" appears to have been a bit of a misnomer, as the panel seems to
            // force a two-column layout regardless of the positions and sizes we set.
            // TODO: try to figure if and how I can manipulate that, and whether I even want to.
            AppendLine(hrPanel, _selfAwareHRLabel, level3);
            AppendLine(hrPanel, _autoSpecsLabel, _autoSpecsToggle);
            AppendLine(hrPanel, _drawFromTeamsLabel, _drawFromTeamsButton);
            AppendLine(hrPanel, _onlyDrawIfSpaceLabel, _onlyDrawIfSpaceToggle);
            AppendLine(hrPanel, _onlyDrawIfIdleLabel, _onlyDrawIfIdleToggle);
            AppendLine(hrPanel, _releaseToTeamLabel, _releaseToTeamButton);
            AppendLine(hrPanel, _fireWhenRedundantLabel, _fireWhenRedundantToggle);
            // adding an empty text element, because of the above mentioned two-column layout
            AppendLine(hrPanel, CreateText(""), _triggerOptimizationButton);

            // add this behaviour to the panel to get unity lifecycle events
            transform.SetParent(hrPanel);

            // profit!
            _extraFieldsAdded = true;
        }

        // we can't do our (re-)initialization on enable, because the components
        // are activated _before_ the teams are set. Instead, we'll use this trigger
        // to set a flag, and do our init on the next update.
        // If I've got my timings right, that should work out just fine.
        public void OnEnable()
        {
            SetDirty();
        }

        public void SetDirty()
        {
            _dirty = true;
        }

        // it turns out that we also do actually need an on-update trigger to react
        // to changes outside our control. I was somewhat surprised that the game 
        // also uses an Update() hook to continuously update certain dynamic UI elements.
        // we need to reinitialize our data for different teams whenever the window is (re-)opened, but also
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
        public void Update()
        {
            CheckDirty();
        }

        // called every update, keep it light.
        public void CheckDirty()
        {
            var level = Teams?.MaxSafeInt(team => team.GetHRLevel()) ?? 0;
            if (_prevHRLevel != level)
            {
                UpdateStarCounter();
            }

            if (!_dirty || !enabled)
            {
                return;
            }

            UpdateControls();
            _dirty = false;
        }

        public void OnTriggerOptimizationButtonClick()
        {
            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).Optimize();
            }
        }

        public void OnFireWhenRedundantChange(bool value)
        {
            if (_initializing)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).FireWhenRedundant = value;
            }
        }

        public void OnOnlyDrawIfIdleChanged(bool value)
        {
            if (_initializing)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).OnlyDrawIdle = value;
            }
        }

        public void OnOnlyDrawIfSpaceChanged(bool value)
        {
            if (_initializing)
            {
                return;
            }

            foreach (var team in Teams)
            {
                SelfAwareHR.For(team).OnlyDrawIfSpace = value;
            }
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
        }
    }
}