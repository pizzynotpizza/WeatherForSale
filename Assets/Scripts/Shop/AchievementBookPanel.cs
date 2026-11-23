using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// The Achievement book opened from the counter: a book-style panel listing the four
    /// achievements. Without an <see cref="IAchievementSource"/> (story 005) every entry is shown
    /// locked and greyed out.
    /// Implements: story-002 criterion 6 (Achievement book on the counter, can be opened).
    /// </summary>
    public sealed class AchievementBookPanel : MonoBehaviour
    {
        private static readonly AchievementId[] Order =
        {
            AchievementId.Die10Times,
            AchievementId.Earn12Stars,
            AchievementId.NormalEnding,
            AchievementId.TrueEnding,
        };

        [SerializeField] private ShopStrings strings;
        [SerializeField, Tooltip("Panel root (full-screen dim + book); shown and hidden.")]
        private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField, Tooltip("Row labels in order: Die 10 times, Earn 12 stars, Normal Ending, True Ending.")]
        private Text[] rowLabels = Array.Empty<Text>();
        [SerializeField, Tooltip("Lock icon per row, shown while locked.")]
        private GameObject[] rowLocks = Array.Empty<GameObject>();
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("Optional component implementing IAchievementSource (story 005).")]
        private MonoBehaviour sourceBehaviour;
        [SerializeField] private Color unlockedColor = new Color(0.33f, 0.2f, 0.12f);
        [SerializeField] private Color lockedColor = new Color(0.33f, 0.2f, 0.12f, 0.4f);

        private IAchievementSource source;

        /// <summary>True while the book is open.</summary>
        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (source == null) source = sourceBehaviour as IAchievementSource;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        /// <summary>Injects the achievement data (story 005). Null shows everything locked.</summary>
        public void SetSource(IAchievementSource achievementSource)
        {
            source = achievementSource;
            if (IsOpen) Refresh();
        }

        /// <summary>Opens the book.</summary>
        public void Open()
        {
            Refresh();
            if (root != null) root.SetActive(true);
            ClearUiSelection();
        }

        /// <summary>Closes the book.</summary>
        public void Close()
        {
            if (root != null) root.SetActive(false);
            ClearUiSelection();
        }

        private static void ClearUiSelection()
        {
            // A button left selected would be "clicked" again by Space/Enter (UI Submit), which
            // collides with Space advancing the Dialog box.
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void Refresh()
        {
            if (titleText != null && strings != null) titleText.text = strings.achievementsTitle;
            for (int i = 0; i < Order.Length; i++)
            {
                bool unlocked = source != null && source.IsUnlocked(Order[i]);
                if (i < rowLabels.Length && rowLabels[i] != null)
                {
                    if (strings != null) rowLabels[i].text = strings.AchievementLabel(Order[i]);
                    rowLabels[i].color = unlocked ? unlockedColor : lockedColor;
                }
                if (i < rowLocks.Length && rowLocks[i] != null) rowLocks[i].SetActive(!unlocked);
            }
        }
    }
}
