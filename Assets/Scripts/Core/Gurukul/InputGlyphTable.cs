using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.Gurukul
{
    // What to draw on a prompt for a given action, per input device. The first thing in the project
    // to use InputManager's device detection, which has been sitting there with no consumers.
    //
    // Labels now, sprites when the art pass lands — the field is already here so nothing downstream
    // changes when they arrive.
    [CreateAssetMenu(fileName = "InputGlyphTable", menuName = "Project Astra/Gurukul/Input Glyph Table")]
    public class InputGlyphTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameInputAction action;
            public string keyboardLabel;
            public string gamepadLabel;
            public Sprite keyboardSprite;
            public Sprite gamepadSprite;
        }

        [SerializeField] private List<Entry> entries = new()
        {
            new() { action = GameInputAction.Confirm, keyboardLabel = "Z", gamepadLabel = "A" },
            new() { action = GameInputAction.Cancel, keyboardLabel = "X", gamepadLabel = "B" },
        };

        [Tooltip("Shown when an action has no entry, so a missing glyph reads as a content gap rather than an empty prompt.")]
        [SerializeField] private string fallbackLabel = "?";

        public string LabelFor(GameInputAction action, InputDeviceType device)
        {
            if (!TryFind(action, out Entry entry)) return fallbackLabel;
            string label = IsGamepad(device) ? entry.gamepadLabel : entry.keyboardLabel;
            return string.IsNullOrEmpty(label) ? fallbackLabel : label;
        }

        public Sprite SpriteFor(GameInputAction action, InputDeviceType device)
        {
            if (!TryFind(action, out Entry entry)) return null;
            return IsGamepad(device) ? entry.gamepadSprite : entry.keyboardSprite;
        }

        // Mouse falls in with keyboard — they are the same physical setup to a player.
        private static bool IsGamepad(InputDeviceType device) =>
            device == InputDeviceType.Gamepad;

        private bool TryFind(GameInputAction action, out Entry found)
        {
            foreach (Entry entry in entries)
            {
                if (entry.action != action) continue;
                found = entry;
                return true;
            }
            found = default;
            return false;
        }
    }
}
