using System;
using System.Collections;
using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Shows the current customer at the counter and moves them with script tweens (the models are
    /// static, not rigged): slide in from the street, gentle idle bob, happy hop on delivery and
    /// slide away. Spawns the customer's prefab from <see cref="CustomerData"/> so new customers
    /// need data only. Honours <see cref="ShopAudioSettings.reduceMotion"/> (no slide/bob/hop).
    /// Implements: story-002 criteria 2 and 7.
    /// </summary>
    public sealed class CustomerController : MonoBehaviour
    {
        private enum Motion { None, Sliding, Standing, Hopping }

        [SerializeField] private ShopUiConfig config;
        [SerializeField] private ShopAudioSettings settings;
        [SerializeField, Tooltip("Where the customer stands, about 1 m beyond the counter front.")]
        private Transform standPoint;
        [SerializeField, Tooltip("Where customers slide in from (off to one side of the street).")]
        private Transform enterFrom;
        [SerializeField, Tooltip("Where customers slide away to.")]
        private Transform exitTo;
        [SerializeField, Tooltip("World yaw that makes a customer model face the player behind the counter.")]
        private float faceCounterYaw = 180f;

        private GameObject current;
        private Coroutine routine;
        private Motion motion = Motion.None;
        private float bobTime;

        /// <summary>Raised when a customer has finished sliding in.</summary>
        public event Action Arrived;

        /// <summary>Raised when the happy hop has finished.</summary>
        public event Action HopFinished;

        /// <summary>Raised when the customer has slid away and been removed.</summary>
        public event Action Left;

        /// <summary>True while a customer model is in the scene.</summary>
        public bool HasCustomer => current != null;

        private bool ReduceMotion => settings != null && settings.reduceMotion;

        /// <summary>Spawns <paramref name="data"/>'s model off to the side and slides it to the counter.</summary>
        public void Arrive(CustomerData data)
        {
            Spawn(data, ReduceMotion ? standPoint.position : enterFrom.position);
            if (ReduceMotion)
            {
                motion = Motion.Standing;
                Arrived?.Invoke();
                return;
            }
            Run(SlideRoutine(enterFrom.position, standPoint.position, config.slideInSeconds, false));
        }

        /// <summary>Spawns <paramref name="data"/>'s model already standing at the counter (returning with a bottle).</summary>
        public void PlaceAtCounter(CustomerData data)
        {
            Spawn(data, standPoint.position);
            motion = Motion.Standing;
        }

        /// <summary>Plays the happy hop, then raises <see cref="HopFinished"/>.</summary>
        public void Hop()
        {
            if (current == null || ReduceMotion)
            {
                HopFinished?.Invoke();
                return;
            }
            Run(HopRoutine());
        }

        /// <summary>Slides the customer away, removes the model and raises <see cref="Left"/>.</summary>
        public void Leave()
        {
            if (current == null || ReduceMotion)
            {
                Clear();
                Left?.Invoke();
                return;
            }
            Run(SlideRoutine(current.transform.position, exitTo.position, config.slideOutSeconds, true));
        }

        /// <summary>Removes the current customer immediately, without events.</summary>
        public void Clear()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (current != null) Destroy(current);
            current = null;
            motion = Motion.None;
        }

        private void Spawn(CustomerData data, Vector3 position)
        {
            Clear();
            Quaternion facing = Quaternion.Euler(0f, faceCounterYaw, 0f);
            if (data != null && data.prefab != null)
            {
                current = Instantiate(data.prefab, position, facing, transform);
                current.name = data.displayName;
            }
            else
            {
                Debug.LogWarning("[CustomerController] Customer '" + (data != null ? data.name : "null") + "' has no prefab.");
                current = new GameObject(data != null ? data.displayName : "Customer");
                current.transform.SetParent(transform, false);
                current.transform.SetPositionAndRotation(position, facing);
            }
            bobTime = 0f;
        }

        private void Run(IEnumerator coroutine)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(coroutine);
        }

        private void Update()
        {
            if (motion != Motion.Standing || current == null || ReduceMotion) return;
            bobTime += Time.deltaTime;
            float y = Mathf.Sin(bobTime * config.bobFrequency * Mathf.PI * 2f) * config.bobAmplitude;
            current.transform.position = standPoint.position + Vector3.up * (y + config.bobAmplitude);
        }

        private IEnumerator SlideRoutine(Vector3 from, Vector3 to, float seconds, bool leaving)
        {
            motion = Motion.Sliding;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float eased = k * k * (3f - 2f * k);
                float waddle = Mathf.Abs(Mathf.Sin(t * config.slideWaddleFrequency * Mathf.PI)) * config.slideWaddleHeight;
                if (current != null) current.transform.position = Vector3.Lerp(from, to, eased) + Vector3.up * waddle;
                yield return null;
            }
            routine = null;
            if (leaving)
            {
                Clear();
                Left?.Invoke();
            }
            else
            {
                if (current != null) current.transform.position = to;
                motion = Motion.Standing;
                bobTime = 0f;
                Arrived?.Invoke();
            }
        }

        private IEnumerator HopRoutine()
        {
            motion = Motion.Hopping;
            int hops = Mathf.Max(1, config.hopCount);
            float t = 0f;
            while (t < config.hopSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / config.hopSeconds) * hops;
                float phase = k - Mathf.Floor(k);
                if (k >= hops) phase = 0f;
                float y = 4f * config.hopHeight * phase * (1f - phase);
                if (current != null) current.transform.position = standPoint.position + Vector3.up * y;
                yield return null;
            }
            routine = null;
            motion = Motion.Standing;
            bobTime = 0f;
            HopFinished?.Invoke();
        }
    }
}
