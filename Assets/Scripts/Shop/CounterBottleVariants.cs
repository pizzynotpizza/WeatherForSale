using System;
using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// The filled bottle on the counter: one generated model per weather (rain, wind, sun, snow). When the bottle
    /// object is shown, only the model for the map just played (<see cref="ShopSession.ChosenMapId"/>) is visible.
    /// The click collider stays on the parent, so handing it over works the same for every model.
    /// Usage: put on the counter bottle object; add one entry per map id with its model child.
    /// </summary>
    public sealed class CounterBottleVariants : MonoBehaviour
    {
        /// <summary>One map's bottle model.</summary>
        [Serializable]
        public struct Variant
        {
            [Tooltip("Map id, for example rainforest.")] public string mapId;
            [Tooltip("The model shown for that map (a child of this object).")] public GameObject model;
        }

        [SerializeField] private ShopSession session;
        [SerializeField] private Variant[] variants = Array.Empty<Variant>();
        [SerializeField, Tooltip("Shown when no entry matches the map (optional).")] private GameObject fallback;

        private void OnEnable() => Show(session != null ? session.ChosenMapId : null);

        /// <summary>Shows only the model for <paramref name="mapId"/> (the fallback when none matches).</summary>
        public void Show(string mapId)
        {
            bool found = false;
            foreach (Variant v in variants)
            {
                if (v.model == null) continue;
                bool match = !string.IsNullOrEmpty(mapId) && v.mapId == mapId;
                v.model.SetActive(match);
                found |= match;
            }
            if (fallback != null) fallback.SetActive(!found);
        }
    }
}
