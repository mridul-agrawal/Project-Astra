using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Changes a name everywhere it is used, in one go.
    //
    // Renaming by hand means finding every reference and hoping; missing one leaves a hub that
    // looks authored and silently does nothing.
    public static class HubRename
    {
        public readonly struct Result
        {
            public readonly int Places;
            public readonly int Things;

            public Result(int places, int things)
            {
                Places = places;
                Things = things;
            }

            public string Reads => Places == 0
                ? "nothing used it"
                : $"changed in {Places} {(Places == 1 ? "place" : "places")} across " +
                  $"{Things} {(Things == 1 ? "thing" : "things")}";
        }

        public static bool CanRename(string from, string to) =>
            !string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to) && from != to;

        public static Result Everywhere(string from, string to)
        {
            if (!CanRename(from, to)) return new Result(0, 0);

            int places = 0, things = 0;

            foreach (IGrouping<Object, HubUsage> owner in HubUsages.Of(from).GroupBy(usage => usage.In))
            {
                int changed = Rewrite(owner.Key, owner.Select(usage => usage.Path), to);
                if (changed == 0) continue;

                places += changed;
                things++;
            }

            Settle();
            return new Result(places, things);
        }

        private static int Rewrite(Object owner, IEnumerable<string> paths, string to)
        {
            var editable = new SerializedObject(owner);
            int changed = 0;

            foreach (string path in paths)
            {
                SerializedProperty field = editable.FindProperty(path);
                if (field == null || field.propertyType != SerializedPropertyType.String) continue;

                field.stringValue = to;
                changed++;
            }

            if (changed == 0) return 0;

            editable.ApplyModifiedProperties();
            MarkChanged(owner);
            return changed;
        }

        private static void MarkChanged(Object owner)
        {
            EditorUtility.SetDirty(owner);

            var part = owner as Component;
            if (part != null) EditorSceneManager.MarkSceneDirty(part.gameObject.scene);
        }

        private static void Settle()
        {
            AssetDatabase.SaveAssets();
            HubUsages.Forget();
            HubIds.Forget();
            HubWatch.LookAgainSoon();
        }
    }
}
