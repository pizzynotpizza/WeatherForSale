using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// The map that pops up from the counter: one large picture button per <see cref="MapCatalog"/>
    /// entry (Rainforest, Valley, Desert, Glacier) plus a close button. There is no confirmation
    /// box; a press reports the choice straight away and the flow decides right or wrong.
    /// Implements: story-002 criteria 3-5.
    /// </summary>
    public sealed class MapSelectPanel : MonoBehaviour
    {
        [SerializeField] private MapCatalog catalog;
        [SerializeField] private ShopStrings strings;
        [SerializeField, Tooltip("Panel root (full-screen dim + card); shown and hidden.")]
        private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField, Tooltip("One button per catalog entry, same order as the catalog.")]
        private Button[] mapButtons = Array.Empty<Button>();
        [SerializeField, Tooltip("Picture on each button, same order as the catalog.")]
        private Image[] mapImages = Array.Empty<Image>();
        [SerializeField, Tooltip("Name label under each button, same order as the catalog.")]
        private Text[] mapLabels = Array.Empty<Text>();
        [SerializeField] private Button closeButton;

        /// <summary>Raised when the player presses a map. The panel stays open; the flow closes it.</summary>
        public event Action<MapEntry> MapChosen;

        /// <summary>Raised when the player presses the close button.</summary>
        public event Action CloseRequested;

        /// <summary>True while the panel is visible.</summary>
        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < mapButtons.Length; i++)
            {
                if (mapButtons[i] == null) continue;
                int index = i; // captured once here; no per-click allocation
                mapButtons[i].onClick.AddListener(() => OnMapPressed(index));
            }
            if (closeButton != null) closeButton.onClick.AddListener(OnClosePressed);
            Refresh();
        }

        private void OnDestroy()
        {
            foreach (Button button in mapButtons)
            {
                if (button != null) button.onClick.RemoveAllListeners();
            }
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClosePressed);
        }

        /// <summary>Shows the panel.</summary>
        public void Open()
        {
            Refresh();
            if (root != null) root.SetActive(true);
            ClearUiSelection();
        }

        /// <summary>Hides the panel.</summary>
        public void Close()
        {
            if (root != null) root.SetActive(false);
            ClearUiSelection();
        }

        /// <summary>Same as pressing the map with <paramref name="mapId"/> (for tests and tools).</summary>
        public void Choose(string mapId)
        {
            MapEntry entry = catalog != null ? catalog.Find(mapId) : null;
            if (entry != null) MapChosen?.Invoke(entry);
        }

        private void Refresh()
        {
            if (titleText != null && strings != null) titleText.text = strings.mapPanelTitle;
            if (catalog == null) return;
            for (int i = 0; i < mapButtons.Length; i++)
            {
                MapEntry entry = i < catalog.Count ? catalog.maps[i] : null;
                if (mapButtons[i] != null) mapButtons[i].gameObject.SetActive(entry != null);
                if (entry == null) continue;
                if (i < mapImages.Length && mapImages[i] != null && entry.thumbnail != null) mapImages[i].sprite = entry.thumbnail;
                if (i < mapLabels.Length && mapLabels[i] != null) mapLabels[i].text = entry.displayName;
            }
        }

        private void OnMapPressed(int index)
        {
            if (!IsOpen || catalog == null || index >= catalog.Count) return;
            MapChosen?.Invoke(catalog.maps[index]);
        }

        private static void ClearUiSelection()
        {
            // A button left selected would be "clicked" again by Space/Enter (UI Submit), which
            // collides with Space advancing the Dialog box.
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void OnClosePressed()
        {
            CloseRequested?.Invoke();
        }
    }
}
