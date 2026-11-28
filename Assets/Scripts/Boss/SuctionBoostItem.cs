using System;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// The Storm Crystal the boss drops in the later phases after every successful suction. It floats, spins and
    /// glows; Sam picks it up by walking into it, and the next suction is easier (more gain, longer window, no decay).
    /// Implements: user change request for story-008 (important item drop that eases the suction).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class SuctionBoostItem : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Spin speed, degrees per second.")] private float spinDegreesPerSecond = 90f;
        [SerializeField, Min(0f), Tooltip("Height of the bob, metres.")] private float bobHeight = 0.35f;
        [SerializeField, Min(0.1f), Tooltip("Radius within which Sam picks it up, metres.")] private float pickupRadius = 1.8f;

        private Vector3 basePosition;
        private bool taken;

        /// <summary>Raised once, when the player picks the crystal up.</summary>
        public event Action Collected;

        private void Awake()
        {
            var sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = pickupRadius / Mathf.Max(0.01f, transform.lossyScale.x);
        }

        private void Start()
        {
            basePosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 2.2f) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (taken || other.GetComponentInParent<PlayerController>() == null) return;
            taken = true;
            Collected?.Invoke();
            Destroy(gameObject);
        }
    }
}
