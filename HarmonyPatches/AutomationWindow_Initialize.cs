// // Copyright Karel Kroeze, 2021-2021.
// // SelfAwareHR/SelfAwareHR/AutomationWindow_Initialize.cs

// using HarmonyLib;

// // ReSharper disable InconsistentNaming

// namespace SelfAwareHR.HarmonyPatches
// {
//     // Hook into the HR automation window Show() method to grab the list
//     // of teams the window is opened for, and update the custom elements
//     // we've added to the window.
//     [HarmonyPatch(typeof(AutomationWindow), nameof(AutomationWindow.Show))]
//     public class AutomationWindow_Show
//     {
//         // We might be able to directly grab the teams from the method args, 
//         // but I'm not sure how Harmony deals with params. Regardless, this
//         // will do.
//         [HarmonyPostfix]
//         public static void Postfix(AutomationWindow __instance)
//         {
//             SelfAwareWindow.Instance.Show(__instance.Teams);
//         }
//     }
// }