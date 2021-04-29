// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/UIHelpers.cs

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SelfAwareHR
{
    public static class UIHelpers
    {
        public static int LINE_HEIGHT = 60;

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
                tt.ToolTipValue = tip;
                tt.TooltipDescription = desc;
            }
        }

        public static Text CreateLocalizedText(string key,
                                               string labelSuffix = "Label",
                                               string tipSuffix = "Tip",
                                               string descSuffix = "Desc")
        {
            if (!key.TryLoc(out var label))
            {
                label = (key + labelSuffix).Loc();
            }

            (key + tipSuffix).TryLoc(out var tip);
            (key + descSuffix).TryLoc(out var desc);
            return CreateText(label, tip, desc);
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

        public static Toggle CreateToggle(Action<bool> onChange,
                                          bool value = false,
                                          string label = "",
                                          string tip = "",
                                          string desc = "")
        {
            var toggle = WindowManager.SpawnCheckbox();
            toggle.isOn = value;
            toggle.SetLabel(label);

            if (!tip.IsNullOrEmpty() || !desc.IsNullOrEmpty())
            {
                AddTooltip(toggle.gameObject, tip, desc);
            }

            return toggle;
        }

        public static void AppendLine(RectTransform parent, params Component[] elements)
        {
            var width = (int)parent.rect.width / elements.Length;
            var y = (int)parent.rect.yMax;
            for (var i = 0; i < elements.Length; i++)
            {
                var rect = new Rect(i * width, y, width, LINE_HEIGHT);
                WindowManager.AddElementToElement(elements[i].gameObject, parent.gameObject, rect, Rect.zero);
            }
        }
    }
}