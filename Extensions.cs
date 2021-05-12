// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Extensions.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Console = DevConsole.Console;
using Object = UnityEngine.Object;

namespace SelfAwareHR
{
    public static class Extensions
    {
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

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || !list.Any();
        }

        public static string Join<T>(this IEnumerable<T> list, string sep = ", ", Func<T, string> stringifier = null)
        {
            return stringifier != null ? string.Join(sep, list.Select(stringifier)) : string.Join(sep, list);
        }

        public static string Join(this IEnumerable<string> list, string sep = ", ")
        {
            return string.Join(sep, list);
        }

        public static T Mode<T>(this IEnumerable<T> list, T defaultValue = default)
        {
            if (list.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return list.Mode(item => item, defaultValue);
        }

        public static bool RelevantFor(this SoftwareWorkItem.FeatureProgress task,
                                       Employee.EmployeeRole                 role,
                                       bool                                  forceFull = false)
        {
            switch (role)
            {
                case Employee.EmployeeRole.Designer:
                    return task.DevTime > 0 && (!task.CodeDone || forceFull);
                case Employee.EmployeeRole.Programmer:
                    return task.CDevTime > 0 && (!task.CodeDone || forceFull);
                case Employee.EmployeeRole.Artist:
                    return task.ADevTime > 0 && (!task.ArtDone || forceFull);
                default:
                    return false;
            }
        }

        public static SelfAwareHR SelfAwareHR(this Team team)
        {
            return global::SelfAwareHR.SelfAwareHR.For(team);
        }

        public static List<T> Splice<T>(this List<T> list, int start, int count)
        {
            var sub = list.Skip(start).Take(count).ToList();
            list.RemoveRange(start, count);
            return sub;
        }

        public static List<T> Splice<T>(this List<T> list, int count)
        {
            return Splice(list, 0, count);
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