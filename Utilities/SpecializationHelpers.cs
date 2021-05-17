// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/SpecializationHelpers.cs

using System.Collections.Generic;
using UnityEngine;

namespace SelfAwareHR.Utilities
{
    public class SpecializationHelpers
    {
        private static Dictionary<RoleSpecLevel, float> _workItemWork = new Dictionary<RoleSpecLevel, float>();

        public static void DevelopmentSpecializations(Employee.EmployeeRole                role,
                                                      SoftwareWorkItem                     product,
                                                      ref Dictionary<RoleSpecLevel, float> specWork)
        {
            var totalWork = 0f;
            _workItemWork.Clear();

            foreach (var task in product.Features)
            {
                if (task.RelevantFor(role))
                {
                    var feature = task.Feature;
                    var work    = task.DevTime(role);
                    var spec    = new RoleSpecLevel(role, feature.Spec, Mathf.Max(0, feature.Level));

                    Log.DebugSpecs(
                        $"{role} :: {product.Name} :: {feature.Name} :: {feature.Spec} ({feature.Level}) :: {work}");

                    totalWork += work;
                    if (!_workItemWork.ContainsKey(spec))
                    {
                        _workItemWork[spec] = 0;
                    }

                    _workItemWork[spec] += work;
                }
            }

            var totalCount = GameData.GetOptimalEmployees(Mathf.Max(1, Mathf.CeilToInt(totalWork)));
            var fte        = totalWork / totalCount;

            specWork.AddUp(_workItemWork, v => v / fte);
        }

        public static void LegalSpecializations(LegalWork item, ref Dictionary<RoleSpecLevel, float> ftes)
        {
            int level;
            switch (item.Type)
            {
                case LegalWork.WorkType.InternalLawsuit:
                    level = 1;
                    break;
                case LegalWork.WorkType.ExternalLawsuit:
                    level = 2;
                    break;
                case LegalWork.WorkType.Patent:
                default:
                    level = 3;
                    break;
            }

            var spec = new RoleSpecLevel(Employee.EmployeeRole.Service, "Law", level);
            var fte  = GameData.GetOptimalEmployees(Mathf.CeilToInt(item.DevTime));
            ftes.AddUp(spec, fte);
        }

        private void MarketingSpecializations(MarketingPlan marketing, ref Dictionary<RoleSpecLevel, float> ftes)
        {
            // todo: add a way for player to change marketing staff levels.
            // I can't find a good way to determine the needed amount of staff, 
            // as there is no devtime equivalent on marketing, and all the 
            // marketing jobs seem to have unit cost. Hardcoding 1 employee for
            // each hype/post marketing task, 2 for each part of a press release.

            switch (marketing.Type)
            {
                case MarketingPlan.TaskType.Hype:
                    ftes.AddUp(new RoleSpecLevel(Employee.EmployeeRole.Service, "Marketing", 1), 1);
                    return;
                case MarketingPlan.TaskType.PostMarket:
                    ftes.AddUp(new RoleSpecLevel(Employee.EmployeeRole.Service, "Marketing", 1), 1);
                    return;
                case MarketingPlan.TaskType.PressRelease:
                    if ((marketing.PressOptions & MarketingPlan.PressOption.Text) != 0)
                    {
                        ftes.AddUp(new RoleSpecLevel(Employee.EmployeeRole.Service, "Marketing", 1), 2);
                    }

                    if ((marketing.PressOptions & MarketingPlan.PressOption.Image) != 0)
                    {
                        ftes.AddUp(new RoleSpecLevel(Employee.EmployeeRole.Service, "Marketing", 2), 2);
                    }

                    if ((marketing.PressOptions & MarketingPlan.PressOption.Video) != 0)
                    {
                        ftes.AddUp(new RoleSpecLevel(Employee.EmployeeRole.Service, "Marketing", 3), 2);
                    }

                    return;
            }
        }

        private static void PortingSpecializations(SoftwarePort                         softwarePort,
                                                   ref Dictionary<RoleSpecLevel, float> ftes)
        {
            var work = 1 + softwarePort.Product.DevTime / 2f;
            var spec = new RoleSpecLevel(Employee.EmployeeRole.Programmer, "System", 1);
            var fte  = GameData.GetOptimalEmployees(Mathf.CeilToInt(work));
            ftes.AddUp(spec, fte);

            Log.DebugSpecs(
                "{role} :: PORT :: {softwarePort.Name} :: {softwarePort.Current.ActualProduct.Name} :: System (0) :: {work}");
        }

        public Dictionary<RoleSpecLevel, int> RequiredSpecs(Employee.EmployeeRole role, Team team)
        {
            var HR   = team.SelfAwareHR();
            var ftes = new Dictionary<RoleSpecLevel, float>();

            // add up the number of (partial) fte's for each project
            foreach (var item in HR.WorkItems(role))
            {
                switch (item)
                {
                    case SoftwareWorkItem product:
                        DevelopmentSpecializations(role, product, ref ftes);
                        break;
                    case SoftwarePort port:
                        PortingSpecializations(port, ref ftes);
                        break;
                    case LegalWork legal:
                        LegalSpecializations(legal, ref ftes);
                        break;
                    case MarketingPlan marketing:
                        MarketingSpecializations(marketing, ref ftes);
                        break;
                    case ResearchWork research:
                        ResearchSpecializations(research, ref ftes);
                        break;
                    case SupportWork support:
                        SupportSpecializations(support, ref ftes);
                        break;
                }
            }


            var specCount = new Dictionary<RoleSpecLevel, int>();
            // todo: refactor logic for RoleSpecLevel structs.
            // batch per role and spec, then iterate levels?
            foreach (var spec in ftes)
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

        private static void ResearchSpecializations(ResearchWork research, ref Dictionary<RoleSpecLevel, float> ftes)
        {
            var spec = new RoleSpecLevel(Employee.EmployeeRole.Designer, research.Spec, 3);
            var fte  = GameData.GetOptimalEmployees(Mathf.CeilToInt(research.Max));
            ftes.AddUp(spec, fte);
        }

        private void SupportSpecializations(SupportWork support, ref Dictionary<RoleSpecLevel, float> ftes)
        {
            // todo: allow user to set support skill level requirement.
            var specSupport = new RoleSpecLevel(Employee.EmployeeRole.Service, "Support", 1);
            // todo: implement way to set "any" spec.
            var specFixing = new RoleSpecLevel(Employee.EmployeeRole.Programmer, "System", 1);

            // tickets generated = Sqrt(Userbase)/10 * FixChance(.25) per hour, randomized from zero (*0.5);
            // tickets resolved = 200 * 1.5 per day, for average skill => 300.
            // tickets verified = FixChance(.1) * 4/5
            // verified bugs fixed = 1 - (1 - FixChance(0))^2 per hour

            // todo: implement delay on releasing employees
            // we're now adding current tickets/bugs to attempt to deal with backlogs, but that may
            // cause issues with fluctuating required specs. 
            var expectedTickets = Mathf.Sqrt(support.TargetProduct.Userbase) / 20 * support.FixChance(.25f, true) * 24;
            var expectedVerifiedBugs = expectedTickets * support.FixChance(.1f, false) * .8f;
            var currentVerifiedBugs = support.Verified - (support.StartBugs - support.TargetProduct.Bugs);
            var bugsPerFte = 1 - Mathf.Pow(1 - support.FixChance(0, false), 2) / GameSettings.DaysPerMonth * 24;
            var fteSupport = (expectedTickets + support.Tickets.Count) / 300;
            var fteFixing = (expectedVerifiedBugs + currentVerifiedBugs) / bugsPerFte;

            ftes.AddUp(specSupport, fteSupport);
            ftes.AddUp(specFixing, fteFixing);
        }
    }
}