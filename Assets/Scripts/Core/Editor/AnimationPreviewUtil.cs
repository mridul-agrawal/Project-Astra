using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Shared editor helpers for previewing sprite-frame animations: blitting a
    // Sprite (including a Multiple sub-sprite of a sheet) into a GUI rect, and
    // pulling the first frame out of a looping controller for a thumbnail. Used
    // by Animation Studio and (later) the Map Editor's environment canvas.
    public static class AnimationPreviewUtil
    {
        public static void DrawSprite(Rect area, Sprite sprite, bool fitAspect = true)
        {
            if (sprite == null || sprite.texture == null) return;

            Texture texture = sprite.texture;
            Rect r = sprite.rect;
            var texCoords = new Rect(r.x / texture.width, r.y / texture.height,
                r.width / texture.width, r.height / texture.height);
            Rect draw = fitAspect ? FitAspect(area, r.width / r.height) : area;
            GUI.DrawTextureWithTexCoords(draw, texture, texCoords, true);
        }

        public static Sprite FirstFrame(RuntimeAnimatorController controller)
        {
            if (controller == null || controller.animationClips.Length == 0) return null;
            return FirstFrame(controller.animationClips[0]);
        }

        public static Sprite FirstFrame(AnimationClip clip)
        {
            if (clip == null) return null;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (bindings.Length == 0) return null;
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            return keys.Length > 0 ? keys[0].value as Sprite : null;
        }

        // Centers an aspect-correct rect inside area (letterboxes rather than stretches).
        public static Rect FitAspect(Rect area, float aspect)
        {
            if (aspect <= 0f) return area;
            float w = area.width, h = area.height;
            if (w / h > aspect) w = h * aspect; else h = w / aspect;
            return new Rect(area.x + (area.width - w) * 0.5f, area.y + (area.height - h) * 0.5f, w, h);
        }
    }
}
