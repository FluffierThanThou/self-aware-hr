// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Log.cs

#define DEBUG_OPTIMIZATION

using System.Collections.Generic;
using DevConsole;

namespace SelfAwareHR
{
    public static class Log
    {
        public static void Debug(string message)
        {
#if DEBUG // it would be really nice if the game allowed use to use System.Diagnostics
            Console.Log(message);
#endif
        }

        public static void DebugSerializing(string message)
        {
#if DEBUG_SERIALIZING
            Console.Log(message);
#endif
        }

        public static void DebugSpecs<T>(Dictionary<string, T[]> specs)
        {
#if DEBUG_OPTIMIZATION
            foreach (var spec in specs)
            {
                Debug($"\t{spec.Key}: {spec.Value.Join()}");
            }
#endif
        }

        public static void DebugSpecs<T>(Dictionary<string, T> specs)
        {
#if DEBUG_OPTIMIZATION
            foreach (var spec in specs)
            {
                Debug($"\t{spec.Key}: {spec.Value}");
            }
#endif
        }

        public static void DebugUpdates(string message)
        {
#if DEBUG_UPDATES
            Console.Log(message);
#endif
        }
    }
}