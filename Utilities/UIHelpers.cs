// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/UIHelpers.cs

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SelfAwareHR.Utilities
{
    public static class UIHelpers
    {
        public static void AddComponents(this RectTransform parent, params Component[] components)
        {
            foreach (var component in components)
            {
                WindowManager.AddElementToElement(component.gameObject, parent.gameObject, Rect.zero, Rect.zero);
            }
        }

        private static void AddTooltip(GameObject gameObject, string tip, string desc)
        {
            var tt = gameObject.AddComponent<GUIToolTipper>();
            tt.Localize = false;
            if (desc.IsNullOrEmpty())
            {
                tt.TooltipDescription = tip;
            }
            else
            {
                tt.ToolTipValue       = tip;
                tt.TooltipDescription = desc;
            }
        }

        public static Button CreateButton(UnityAction onClick, string label = "", string tip = "", string desc = "")
        {
            var button = WindowManager.SpawnButton();
            button.onClick.AddListener(onClick);
            button.SetLabel(label);

            if (!tip.IsNullOrEmpty() || !desc.IsNullOrEmpty())
            {
                AddTooltip(button.gameObject, tip, desc);
            }

            return button;
        }

        public static Text CreateLocalizedText(string key,
                                               string labelSuffix = "Label",
                                               string tipSuffix   = "Tip",
                                               string descSuffix  = "Desc")
        {
            if (!key.TryLoc(out var label))
            {
                label = (key + labelSuffix).Loc();
            }

            (key + tipSuffix).TryLoc(out var tip);
            (key + descSuffix).TryLoc(out var desc);
            return CreateText(label, tip, desc);
        }

        public static RectTransform CreateRowLayout(params Component[] children)
        {
            var go        = new GameObject("RowLayout", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var transform = go.GetComponent<RectTransform>();
            foreach (var child in children)
            {
                child.transform.SetParent(transform);
            }

            return transform;
        }

        public static Text CreateText(string label, string tooltipTitle = null, string tooltipDesc = null)
        {
            var text = WindowManager.SpawnLabel();
            text.text = label;

            if (!tooltipTitle.IsNullOrEmpty() || !tooltipDesc.IsNullOrEmpty())
            {
                AddTooltip(text.gameObject, tooltipTitle, tooltipDesc);
            }

            return text;
        }

        public static Toggle CreateToggle(UnityAction<bool> onChange,
                                          bool              value = false,
                                          string            label = "",
                                          string            tip   = "",
                                          string            desc  = "")
        {
            var toggle = WindowManager.SpawnCheckbox();
            toggle.onValueChanged.AddListener(onChange);
            toggle.isOn = value;
            toggle.SetLabel(label);

            if (!tip.IsNullOrEmpty() || !desc.IsNullOrEmpty())
            {
                AddTooltip(toggle.gameObject, tip, desc);
            }

            return toggle;
        }

        public static void SetLabel(this Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            text.text = label;
        }

        public static void SetLabel(this Toggle toggle, string label)
        {
            var text = toggle.GetComponentInChildren<Text>();
            text.text = label;
        }
    }
}