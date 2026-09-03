using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Editor
{
    // The panel a designer builds the hub from: pick a room, pick a thing, click where it goes.
    [Overlay(typeof(SceneView), "Hub", true)]
    public sealed class HubSceneOverlay : Overlay
    {
        private const float ThumbnailSize = 54f;

        private static HubPalette.Entry armed;
        private string category;

        public static HubPalette.Entry Armed => armed;
        public static void Disarm() => armed = null;

        public override VisualElement CreatePanelContent()
        {
            var body = new IMGUIContainer(Draw) { style = { width = 260, paddingBottom = 4 } };
            HubEditing.Changed += body.MarkDirtyRepaint;
            return body;
        }

        private void Draw()
        {
            HubRoom room = HubEditing.EditingRoom;

            DrawRoomPicker(room);
            DrawVisitPicker();
            DrawWhatIsWrong();
            EditorGUILayout.Space(2);
            DrawOverlayToggles();

            if (room == null)
            {
                EditorGUILayout.HelpBox("Pick a room to start placing things in it.", MessageType.None);
                return;
            }

            EditorGUILayout.Space(4);
            DrawPalette(room);

            EditorGUILayout.Space(4);
            DrawSelection();
        }

        // What is selected and what can be done to it, so making a prop interactive never means
        // remembering which component to add.
        private static void DrawSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) return;

            EditorGUILayout.LabelField(selected.name, EditorStyles.boldLabel);

            if (HubAuthoring.IsInteractive(selected)) DrawInteractive(selected);
            else if (GUILayout.Button("Make it something she can look at"))
                Select(HubAuthoring.MakeInspectable(selected));
        }

        private static void DrawInteractive(GameObject selected)
        {
            var inspectable = selected.GetComponent<InspectableInteractable>();
            if (inspectable != null)
            {
                EditorGUILayout.LabelField($"She can look at this  ·  {inspectable.InteractableId}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Give it a conversation in the Inspector.", EditorStyles.miniLabel);

                if (GUILayout.Button("Not interactive any more")) HubAuthoring.Revert(selected);
                return;
            }

            EditorGUILayout.LabelField("She can already walk up to this.", EditorStyles.miniLabel);
        }

        private static void Select(Object made)
        {
            if (made is Component component) Selection.activeGameObject = component.gameObject;
        }

        private void DrawRoomPicker(HubRoom current)
        {
            HubRoom[] rooms = HubRooms.InLoadedScenes().OrderBy(r => r.LocationId).ToArray();

            if (rooms.Length == 0)
            {
                EditorGUILayout.HelpBox("Open the Hub scene to edit a room.", MessageType.Info);
                return;
            }

            string[] names = rooms.Select(r => r.LocationId).Append("Show every room").ToArray();
            int index = System.Array.IndexOf(rooms, current);
            if (index < 0) index = rooms.Length;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Room", GUILayout.Width(38));

                int picked = EditorGUILayout.Popup(index, names);
                if (picked != index) HubEditing.Isolate(picked < rooms.Length ? rooms[picked] : null);

                using (new EditorGUI.DisabledScope(current == null))
                    if (GUILayout.Button("Frame", EditorStyles.miniButton, GUILayout.Width(48)))
                        HubEditing.Frame(current);
            }
        }

        // Which visit the room is being shown as. Picking one stands its cast in the room, so the
        // same room can be looked at as it is on the first day and as it is on the fourth.
        private void DrawVisitPicker()
        {
            HubVisitData[] visits = HubVisitLens.All().ToArray();
            if (visits.Length == 0) return;

            string[] names = new[] { "the empty room" }.Concat(visits.Select(v => v.VisitId)).ToArray();
            int current = System.Array.IndexOf(visits, HubVisitLens.Visit) + 1;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("As", GUILayout.Width(38));

                int picked = EditorGUILayout.Popup(current, names);
                if (picked != current) HubVisitLens.Visit = picked == 0 ? null : visits[picked - 1];

                using (new EditorGUI.DisabledScope(!HubLaunch.CanLaunch || HubVisitLens.Visit == null))
                    if (GUILayout.Button("Play", EditorStyles.miniButton, GUILayout.Width(48)))
                        HubLaunch.PlayFrom(HubVisitLens.Visit, 0, null);
            }

            DrawWhatChanged(visits);
        }

        // What this visit does differently from the one before it, so flipping between them says
        // what actually moved rather than leaving it to be spotted.
        private static void DrawWhatChanged(HubVisitData[] visits)
        {
            HubVisitData visit = HubVisitLens.Visit;
            if (visit == null) return;

            int index = System.Array.IndexOf(visits, visit);
            if (index <= 0) { EditorGUILayout.LabelField("The first one.", EditorStyles.miniLabel); return; }

            foreach (string line in HubVisitDiff.Describe(visits[index - 1], visit))
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
        }

        // Always there, so a broken hub is something you already know about rather than something
        // you find out by pressing Play.
        private static void DrawWhatIsWrong()
        {
            int wrong = HubWatch.Count;
            if (wrong == 0) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(wrong == 1 ? "1 problem" : $"{wrong} problems",
                    EditorStyles.miniLabel);

                if (GUILayout.Button("Show me", EditorStyles.miniButton, GUILayout.Width(70)))
                    HubEditorWindow.OpenOnProblems();
            }
        }

        private void DrawOverlayToggles()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Toggle(HubEditing.Overlay.Extent, "Edge");
                Toggle(HubEditing.Overlay.Blocking, "Blocking");
                Toggle(HubEditing.Overlay.Interaction, "Reach");
                Toggle(HubEditing.Overlay.Spawns, "Start");
            }
        }

        private static void Toggle(HubEditing.Overlay overlay, string label)
        {
            bool shown = HubEditing.Shows(overlay);
            if (GUILayout.Toggle(shown, label, EditorStyles.miniButton) != shown)
            {
                HubEditing.Toggle(overlay);
                SceneView.RepaintAll();
            }
        }

        private void DrawPalette(HubRoom room)
        {
            HubPalette palette = HubPalette.Load();
            string[] categories = palette.Categories.ToArray();

            if (categories.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Drop art into {HubArtImporter.ArtFolder} and it appears here.", MessageType.Info);
                return;
            }

            DrawCategoryTabs(categories);
            DrawEntries(palette.InCategory(category).ToArray(), room);
            DrawArmedEntry(palette);
        }

        private void DrawCategoryTabs(string[] categories)
        {
            if (!categories.Contains(category)) category = categories[0];

            int picked = GUILayout.Toolbar(System.Array.IndexOf(categories, category), categories,
                EditorStyles.miniButton);
            category = categories[picked];
        }

        private void DrawEntries(IReadOnlyList<HubPalette.Entry> entries, HubRoom room)
        {
            const int columns = 4;

            for (int i = 0; i < entries.Count; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns && i + column < entries.Count; column++)
                        DrawEntry(entries[i + column]);
                }
            }
        }

        private static void DrawEntry(HubPalette.Entry entry)
        {
            Texture preview = AssetPreview.GetAssetPreview(entry.Source) ??
                              AssetPreview.GetMiniThumbnail(entry.Source);

            var content = new GUIContent(preview, entry.label);
            bool isArmed = armed == entry;

            if (GUILayout.Toggle(isArmed, content, EditorStyles.miniButton,
                    GUILayout.Width(ThumbnailSize), GUILayout.Height(ThumbnailSize)) != isArmed)
                armed = isArmed ? null : entry;
        }

        // What the armed thing is, right where it was picked, so nobody opens the palette asset to
        // say that a floor is a floor.
        private static void DrawArmedEntry(HubPalette palette)
        {
            if (armed == null) return;

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(armed.label, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            armed.kind = (HubPalette.Kind)EditorGUILayout.EnumPopup("Is", armed.kind);
            if (armed.kind == HubPalette.Kind.Object)
            {
                armed.blocks = EditorGUILayout.Toggle("Stops her", armed.blocks);
                if (armed.blocks)
                    armed.footprint = EditorGUILayout.Vector2Field("Footprint", armed.footprint);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (armed.kind == HubPalette.Kind.Ground) armed.blocks = false;
                palette.Save();
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField("Click in the scene to place it. Hold shift to keep placing.",
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Done", EditorStyles.miniButton)) armed = null;

                if (GUILayout.Button("Remove", EditorStyles.miniButton))
                {
                    palette.Forget(armed);
                    armed = null;
                }
            }
        }
    }
}
