// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Meta.cs

using SelfAwareHR.Utilities;
using UnityEngine;

namespace SelfAwareHR
{
    public class Meta : ModMeta
    {
        public override string Name => "Self-Aware HR";

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            var desc = WindowManager.SpawnLabel();
            desc.text = "modDesc".Loc();

            WindowManager.AddElementToElement(desc.gameObject, parent.gameObject, new Rect(0, 0, 400, 128f), Rect.zero);
        }

        public override void Deserialize(WriteDictionary data, GameReader.LoadMode mode)
        {
            Log.DebugSerializing("de-serializing self-aware mod data");
            var teams = data.Get<WriteDictionary[]>("teams");
            SelfAwareHRManager.Deserialize(teams);
        }

        public override WriteDictionary Serialize(GameReader.LoadMode mode)
        {
            Log.DebugSerializing("serializing self-aware mod data");
            var save = new WriteDictionary("Fluffy.SelfAwareHR");
            save["teams"] = SelfAwareHRManager.Serialize();
            return save;
        }
    }
}