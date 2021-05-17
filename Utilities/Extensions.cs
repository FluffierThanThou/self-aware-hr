// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Extensions.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Console = DevConsole.Console;
using Object = UnityEngine.Object;

namespace SelfAwareHR.Utilities
{
    public static class Extensions
    {
        public static Dictionary<T, float> AddUp<T>(this Dictionary<T, float> dict,
                                                    Dictionary<T, float>      other,
                                                    Func<float, float>        transform)
        {
            foreach (var pair in other)
            {
                dict.AddUp(pair.Key, transform(pair.Value));
            }

            return dict;
        }

        public static void Destroy(this Object obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }

        public static float DevTime(this SoftwareWorkItem.FeatureProgress feature, Employee.EmployeeRole role)
        {
            switch (role)
            {
                case Employee.EmployeeRole.Programmer:
                    return feature.CDevTime;
                case Employee.EmployeeRole.Designer:
                    return feature.DevTime;
                case Employee.EmployeeRole.Artist:
                    return feature.ADevTime;
                default:
                    return 0;
            }
        }

        public static IEnumerable<T> FilterNull<T>(this IEnumerable<T> list)
        {
            return list?.SelectNotNull(item => item);
        }

        public static IEnumerable<string> FilterNullOrEmpty(this IEnumerable<string> list)
        {
            return list?.Where(e => !e.IsNullOrEmpty());
        }

        public static float FixChance(this SupportWork support, float offset, bool daysPerMonth)
        {
            var bugsSolved = support.StartBugs != 0
                ? Mathf.Clamp01(support.TargetProduct.Bugs / (float) support.StartBugs)
                : 0f;
            var chance = offset + bugsSolved * (1f - offset);
            return !daysPerMonth ? chance : chance / GameSettings.DaysPerMonth;
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || !list.Any();
        }

        public static T Mode<T>(this IEnumerable<T> list, T defaultValue = default)
        {
            if (list.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return list.Mode(item => item, defaultValue);
        }

        public static T Pop<T>(this ICollection<T> collection)
        {
            if (collection.IsNullOrEmpty())
            {
                return default;
            }

            var item = collection.Last();
            collection.Remove(item);
            return item;
        }

        public static string ReadableList(this IEnumerable<string> collection,
                                          bool                     finalAnd    = true,
                                          bool                     oxfordComma = true,
                                          string                   commaKey    = "listSeparatorComma",
                                          string                   andKey      = "listSeparatorAnd",
                                          string                   emptyKey    = "listEmpty")
        {
            var list = collection.FilterNullOrEmpty().ToList();
            if (list.IsNullOrEmpty())
            {
                return emptyKey.Loc();
            }

            if (list.Count() == 1)
            {
                return list.First();
            }

            var comma  = commaKey.Loc();
            var and    = andKey.Loc();
            var result = new StringBuilder();
            for (var i = 0; i < list.Count(); i++)
            {
                var first = i == 0;
                var last  = i == list.Count() - 1;

                if (!first && (!last || oxfordComma || !finalAnd))
                {
                    result.Append(comma);
                }

                if (last && finalAnd)
                {
                    result.Append(and);
                }

                result.Append(list[i]);
            }

            return result.ToString();
        }

        public static bool RelevantFor(this SoftwareWorkItem.FeatureProgress task,
                                       Employee.EmployeeRole                 role,
                                       bool                                  forceFull = false)
        {
            switch (role)
            {
                case Employee.EmployeeRole.Designer:
                    return task.DevTime > float.Epsilon && (forceFull || !task.CodeDone);
                case Employee.EmployeeRole.Programmer:
                    return task.CDevTime > float.Epsilon && (forceFull || !task.CodeDone);
                case Employee.EmployeeRole.Artist:
                    return task.ADevTime > float.Epsilon && (forceFull || !task.ArtDone);
                default:
                    return false;
            }
        }

        public static SelfAwareHRManager SelfAwareHR(this Team team)
        {
            return SelfAwareHRManager.For(team);
        }

        public static bool TryLoc(this string input, out string output)
        {
            if (Localization.CurrentTranslation == null)
            {
                output = null;
                Console.LogWarning("TryLoc called before Localizations were initialized");
                return false;
            }

            if (Localization.CurrentTranslation.TryGetValue(input, out var outputs))
            {
                output = outputs[0];
                return true;
            }

            var defaultLanguage = Localization.GetLanguage("English", Localization.GetLanguages().FirstOrDefault());
            if (defaultLanguage != null && defaultLanguage.TryGetValue(input, out outputs))
            {
                output = outputs[0];
                return true;
            }

            output = null;
            return false;
        }
    }
}