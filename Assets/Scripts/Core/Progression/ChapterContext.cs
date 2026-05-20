using UnityEngine;

namespace ProjectAstra.Core.Progression
{
    // Static holder for "which chapter are we in". Reads from a ChapterMeta
    // MonoBehaviour placed on the BattleMap scene root. The campaign-progression
    // ticket will replace this with a lookup into a CampaignState ScriptableObject;
    // until then ChapterMeta is the single source of truth.
    public static class ChapterContext
    {
        public static int CurrentChapterNumber { get; private set; } = 1;
        public static string CurrentChapterTitle { get; private set; } = "";

        public static void SetFromScene(int chapterNumber, string chapterTitle)
        {
            CurrentChapterNumber = Mathf.Max(1, chapterNumber);
            CurrentChapterTitle = chapterTitle ?? "";
        }
    }

    // Scene component: drop on the BattleMap scene root. On Awake it publishes
    // its values into ChapterContext so DeathRegistry and friends can tag
    // entries with the correct chapter number.
    public class ChapterMeta : MonoBehaviour
    {
        [SerializeField] private int _chapterNumber = 1;
        [SerializeField] private string _chapterTitle = "";

        private void Awake()
        {
            ChapterContext.SetFromScene(_chapterNumber, _chapterTitle);
        }
    }
}
