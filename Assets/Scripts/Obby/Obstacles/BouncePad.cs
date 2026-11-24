using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Launches the player when they land on it (giant bounce mushroom, spring, launcher).
    /// Needs a <see cref="RideableSurface"/> on the same GameObject (the surface the player lands on).
    /// Implements: story-004, Rainforest 1 "bounce mushrooms" (เห็ดเด้งส่งตัวสูง).
    /// </summary>
    [RequireComponent(typeof(RideableSurface))]
    public sealed class BouncePad : MonoBehaviour
    {
        [SerializeField, Tooltip("World-space velocity given to the player on landing, metres per second. Y sets the height: apex = y^2 / (2 * gravity).")]
        private Vector3 launchVelocity = new Vector3(0f, 15f, 0f);

        [SerializeField, Min(0f), Tooltip("Seconds after a launch during which the pad ignores new landings.")]
        private float cooldownSeconds = 0.3f;

        [SerializeField, Tooltip("Visual squashed while launching (optional). Scaled around its own origin.")]
        private Transform squashTarget;

        [SerializeField, Range(0.2f, 1f), Tooltip("Vertical scale of the visual at the deepest point of the squash (1 = no squash).")]
        private float squashScaleY = 0.65f;

        [SerializeField, Min(0.01f), Tooltip("Duration of the squash-and-recover animation, seconds.")]
        private float squashSeconds = 0.3f;

        private RideableSurface surface;
        private Vector3 baseScale = Vector3.one;
        private float nextAllowedTime;
        private float squashTimer;

        private void Awake()
        {
            surface = GetComponent<RideableSurface>();
            if (squashTarget != null) baseScale = squashTarget.localScale;
        }

        private void OnEnable()
        {
            surface.RiderLanded += OnRiderLanded;
        }

        private void OnDisable()
        {
            surface.RiderLanded -= OnRiderLanded;
        }

        private void OnRiderLanded(PlayerController player)
        {
            if (Time.time < nextAllowedTime) return;
            nextAllowedTime = Time.time + cooldownSeconds;
            squashTimer = squashSeconds;
            player.Launch(launchVelocity);
        }

        private void Update()
        {
            if (squashTarget == null || squashTimer <= 0f) return;

            squashTimer = Mathf.Max(0f, squashTimer - Time.deltaTime);
            float progress = 1f - squashTimer / squashSeconds;
            float squash = Mathf.Sin(progress * Mathf.PI); // 0 -> 1 -> 0
            float shrink = (1f - squashScaleY) * squash;
            squashTarget.localScale = new Vector3(baseScale.x * (1f + shrink * 0.5f), baseScale.y * (1f - shrink), baseScale.z * (1f + shrink * 0.5f));
            if (squashTimer <= 0f) squashTarget.localScale = baseScale;
        }
    }
}
