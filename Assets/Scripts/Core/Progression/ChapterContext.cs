using UnityEngine;

namespace ProjectAstra.Core.Progression
{
    /// <summary>
    /// Static holder for "which chapter are we in". Reads from a ChapterMeta
    /// MonoBehaviour placed on the BattleMap scene root. The campaign-progression
    /// ticket will replace this with a lookup into a CampaignState ScriptableObject;
    /// for UM-01 shipping today the scene's ChapterMeta is the single source.
    /// </summary>
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

    /// <summary>
    /// Scene-level component: place on the BattleMap scene root. On Awake it
    /// publishes its values into ChapterContext so DeathRegistry etc. can tag
    /// entries with the correct chapter number.
    /// </summary>
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
