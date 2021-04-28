// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/Extensions.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Console = DevConsole.Console;
using Object = UnityEngine.Object;

namespace SelfAwareHR
{
    public static class Extensions
    {
        public static SelfAwareInstance SelfAwareHRSettings(this Team team)
        {
            return SelfAwareInstance.For(team);
        }

        public static void Destroy(this Object obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }

        public static T MaxBy<T>(this IEnumerable<T> list, Func<T, T, int> compareFunc)
        {
            if (list.IsNullOrEmpty())
            {
                return default;
            }

            return list.Aggregate((max, cur) => compareFunc(cur, max) < 0 ? cur : max);
        }

        public static T Mode<T>(this IEnumerable<T> list, T defaultValue = default)
        {
            if (list.IsNullOrEmpty())
            {
                return defaultValue;
            }

            return list.Mode(item => item, defaultValue);
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