// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Log.cs

#if DEBUG
#define DEBUG_OPTIMIZATION
#define DEBUG_SPECIALIZATIONS
#endif

using DevConsole;

namespace SelfAwareHR.Utilities
{
    public static class Log
    {
        public static void Debug(string message)
        {
#if DEBUG // it would be really nice if the game allowed use to use System.Diagnostics
            Console.Log(message);
#endif
        }

        public static void DebugOptimization(string message)
        {
#if DEBUG_OPTIMIZATION
            Console.Log(message);
#endif
        }

        public static void DebugSerializing(string message)
        {
#if DEBUG_SERIALIZING
            Console.Log(message);
#endif
        }

        public static void DebugSpecs(string message)
        {
#if DEBUG_SPECIALIZATIONS
            Console.Log(message);
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