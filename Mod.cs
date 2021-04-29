using UnityEngine;

namespace SelfAwareHR
{
    public class Mod : ModMeta
    {
        public override string Name => "Self-Aware HR";

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            var desc = WindowManager.SpawnLabel();
            desc.text = "modDesc".Loc();

            WindowManager.AddElementToElement(desc.gameObject, parent.gameObject, new Rect(0, 0, 400, 128f), Rect.zero);
        }

        public override WriteDictionary Serialize(GameReader.LoadMode mode)
        {
            var save = new WriteDictionary("Fluffy.SelfAwareHR");
            save["teams"] = SelfAwareHR.Serialize();
            return save;
        }

        public override void Deserialize(WriteDictionary data, GameReader.LoadMode mode)
        {
            var teams = data.Get<WriteDictionary[]>("teams");
            SelfAwareHR.Deserialize(teams);
        }
    }
}