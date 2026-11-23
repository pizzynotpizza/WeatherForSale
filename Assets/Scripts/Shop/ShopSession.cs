using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Injectable go-between that survives scene loads: which customer is being served, which map
    /// was chosen and whether the player came back with a bottle. Referenced through serialized
    /// fields (no static singleton). Runtime state is not serialized, is kept in memory across
    /// scene changes (<see cref="HideFlags.DontUnloadUnusedAsset"/>) and resets when play starts.
    /// Implements: story-002 Implementation Notes ("pass the chosen map through an injectable
    /// middleman, not a static singleton").
    /// </summary>
    [CreateAssetMenu(fileName = "ShopSession", menuName = "Weather For Sale/Shop/Shop Session")]
    public sealed class ShopSession : ScriptableObject
    {
        [NonSerialized] private int customerIndex;
        [NonSerialized] private string chosenMapId;
        [NonSerialized] private bool returnedWithBottle;

        /// <summary>Index into the CustomerRoster of the customer currently being served.</summary>
        public int CustomerIndex => customerIndex;

        /// <summary>Id of the map chosen for the current customer, or null before a choice.</summary>
        public string ChosenMapId => chosenMapId;

        /// <summary>True after the player came back from a map with a filled bottle.</summary>
        public bool ReturnedWithBottle => returnedWithBottle;

        private void OnEnable()
        {
            // Keep the instance (and its non-serialized state) alive while scenes that do not
            // reference it are loaded, e.g. the obby level.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
            ResetSession();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Covers "Enter Play Mode without domain reload", where OnEnable is not called again.
            if (change == PlayModeStateChange.ExitingEditMode) ResetSession();
        }
#endif

        /// <summary>Back to the start of the day: first customer, no map, no bottle.</summary>
        public void ResetSession()
        {
            customerIndex = 0;
            chosenMapId = null;
            returnedWithBottle = false;
        }

        /// <summary>Records the right map choice just before the map scene loads.</summary>
        public void RecordDeparture(int customer, string mapId)
        {
            customerIndex = Mathf.Max(0, customer);
            chosenMapId = mapId;
            returnedWithBottle = false;
        }

        /// <summary>
        /// Call when the player finishes a map with a filled bottle, before loading the Shop scene
        /// (story 003 wires this). The Shop then puts the bottle on the counter.
        /// </summary>
        public void ReturnWithBottle()
        {
            returnedWithBottle = true;
        }

        /// <summary>The bottle was handed over: move on to the next customer.</summary>
        public void CompleteDelivery()
        {
            returnedWithBottle = false;
            chosenMapId = null;
            customerIndex++;
        }
    }
}
