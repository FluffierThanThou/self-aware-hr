using UnityEngine;

namespace SelfAwareHR
{
    public class Mod : ModMeta
    {
        public Mod()
        {
            // using a sentinel gameObject to get events means we dont need harmony (for now)
            // var harmony = new Harmony("fluffy.self-aware-hr");
            // harmony.PatchAll();
        }

        public override string Name => "Self-Aware HR";

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            var desc = WindowManager.SpawnLabel();
            desc.text = "modDesc".Loc();

            WindowManager.AddElementToElement(desc.gameObject, parent.gameObject, new Rect(0, 0, 400, 128f), Rect.zero);
        }
    }
}