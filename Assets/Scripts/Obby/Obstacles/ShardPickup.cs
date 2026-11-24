using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A hidden purple Shard placed on a secret branch of a level. Touching it marks it collected,
    /// hides it and raises <see cref="Collected"/>. It counts nothing yet: story 006 (story mode) will
    /// subscribe to the event and persist the collection.
    /// Implements: story-004 (one hidden Shard per map, position only).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ShardPickup : MonoBehaviour
    {
        [SerializeField, Tooltip("Which map this shard belongs to (map id, for example rainforest). Used by story 006.")]
        private string mapId = "";

        [SerializeField, Tooltip("Visual that spins and bobs; hidden when collected. Optional.")]
        private Transform visual;

        [SerializeField, Min(0f), Tooltip("Spin speed, degrees per second.")]
        private float spinDegreesPerSecond = 90f;

        [SerializeField, Min(0f), Tooltip("Peak bobbing height, metres.")]
        private float bobHeight = 0.15f;

        [SerializeField, Min(0.1f), Tooltip("Seconds for one bob cycle.")]
        private float bobPeriodSeconds = 2f;

        private Vector3 visualHomeLocalPosition;

        /// <summary>Raised once, when the player touches the shard.</summary>
        public event Action<ShardPickup> Collected;

        /// <summary>True after the player has touched it in this attempt.</summary>
        public bool IsCollected { get; private set; }

        /// <summary>Map id this shard belongs to.</summary>
        public string MapId => mapId;

        private void Awake()
        {
            if (visual != null) visualHomeLocalPosition = visual.localPosition;
        }

        private void Update()
        {
            if (IsCollected || visual == null) return;
            visual.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
            float bob = Mathf.Sin(Time.time * (2f * Mathf.PI / bobPeriodSeconds)) * bobHeight;
            visual.localPosition = visualHomeLocalPosition + new Vector3(0f, bob, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected || other.GetComponentInParent<PlayerController>() == null) return;
            IsCollected = true;
            if (visual != null) visual.gameObject.SetActive(false);
            GetComponent<Collider>().enabled = false;
            Collected?.Invoke(this);
        }
    }
}
