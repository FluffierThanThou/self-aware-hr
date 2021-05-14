// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/OptimizationLog.cs

using System.Collections.Generic;
using System.Linq;
using System.Text;
using SelfAwareHR.Utilities;

namespace SelfAwareHR
{
    public class OptimizationLog
    {
        private Team          _team;
        private StringBuilder log = new StringBuilder();

        public OptimizationLog(Team team)
        {
            _team = team;
        }

        private string CountEmployees(int count)
        {
            if (count != 1)
            {
                return "employeePlural".Loc(count);
            }

            return "employeeSingular".Loc(count);
        }

        private void Log(string msg)
        {
            HUD.Instance.AutoLog.Log(msg);
            Utilities.Log.DebugOptimization(msg);
            log.AppendLine(msg);
        }

        public void LogFire(List<Actor> fired, float cost)
        {
            Log("logFired".LocColorAll(
                    _team.Name,
                    CountEmployees(fired.Count),
                    cost.Currency()));
        }

        public void LogHire(Dictionary<Actor, RoleSpecLevel> hired, float cost)
        {
            Log("logHired".LocColorAll(
                    _team.Name,
                    CountEmployees(hired.Count),
                    hired.Values.Select(s => s.Spec).Distinct().ReadableList(),
                    cost.Currency()));
        }

        public void LogRelease(List<Actor> released, Team to)
        {
            Log("logReleased".LocColorAll(
                    _team.Name,
                    CountEmployees(released.Count),
                    to.Name));
        }

        public void LogTransfer(Dictionary<Actor, RoleSpecLevel> drawn)
        {
            Log("logDrawn".LocColorAll(
                    _team.Name,
                    CountEmployees(drawn.Count),
                    drawn.Values.Select(s => s.Spec).Distinct().ReadableList()));
        }

        public override string ToString()
        {
            return log.ToString();
        }

        public void WarnNoApplicants(Dictionary<RoleSpecLevel, int> missing, bool hire = true)
        {
            var team   = _team.Name;
            var count  = missing.Aggregate(0, (acc, cur) => acc + cur.Value);
            var skills = missing.Keys.Select(s => s.Spec).Distinct().ReadableList(andKey: "listSeparatorOr");

            Log(hire
                    ? "warnNoApplicantsHire".LocColorAll(team, CountEmployees(count), skills)
                    : "warnNoApplicantsTransfer".LocColorAll(team, CountEmployees(count), skills));
        }

        public void WarnNoBudget(int count, bool hire)
        {
            var team = _team.Name;

            Log(hire
                    ? "warnNoBudgetHire".LocColorAll(team, CountEmployees(count))
                    : "warnNoBudgetFire".LocColorAll(team, CountEmployees(count)));
        }

        public void WarnNoSpace(int count, bool hire)
        {
            var team = _team.Name;

            Log(hire
                    ? "warnNoSpaceHire".LocColorAll(team, CountEmployees(count))
                    : "warnNoSpaceTransfer".LocColorAll(team, CountEmployees(count)));
        }

        public void WarnOverstaffed(int count)
        {
            Log("warnOverstaffed".LocColorAll(_team.Name, CountEmployees(count)));
        }

        public void WarnUnderstaffed(Dictionary<RoleSpecLevel, int> missing)
        {
            var count  = missing.Aggregate(0, (acc, cur) => acc + cur.Value);
            var skills = missing.Keys.Select(s => s.Spec).Distinct().ReadableList(andKey: "listSeparatorOr");
            Log("warnUnderstaffed".LocColorAll(_team.Name, CountEmployees(count), skills));
        }
    }
}