using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A launch trigger just past a cliff edge: a player who walks or jumps off the edge into it starts
    /// gliding (<see cref="GlideMode"/>, added to the player on first use). The zone carries the glide's
    /// tuning, so every leg of a course can be tuned on its own launch. The glide flies along this
    /// transform's forward (horizontal) and never climbs above <see cref="CeilingY"/>.
    /// Implements: story-004, Valley 1 "Canyon glide" (glide legs between landing ledges).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GlideZone : MonoBehaviour
    {
        [Header("Ceiling")]
        [SerializeField, Tooltip("Glide ceiling (feet height) above this zone's origin, metres. W stops climbing as it gets near; above it the air pushes the glider down.")]
        private float ceilingHeight = 2.5f;

        [SerializeField, Min(0.01f), Tooltip("W's climb fades to zero over this many metres below the ceiling.")]
        private float ceilingSoftBand = 1.0f;

        [SerializeField, Min(0f), Tooltip("Sink speed forced above the ceiling, metres per second (W is ignored up there).")]
        private float ceilingPushSpeed = 2.5f;

        [Header("Forward (automatic)")]
        [SerializeField, Min(0f), Tooltip("Forward speed in level flight, metres per second.")]
        private float cruiseSpeed = 8f;

        [SerializeField, Min(0f), Tooltip("Forward speed while climbing at full climb rate (climbing trades speed for height).")]
        private float climbForwardSpeed = 7f;

        [SerializeField, Min(0f), Tooltip("Forward speed while diving at full dive rate.")]
        private float diveForwardSpeed = 9.5f;

        [SerializeField, Min(0f), Tooltip("Lowest forward speed right after launch (a walk-off is slower than the glide), metres per second.")]
        private float entryForwardSpeed = 7f;

        [SerializeField, Min(0.01f), Tooltip("How quickly the forward speed follows its target, metres per second squared.")]
        private float forwardAcceleration = 4f;

        [Header("Steering (A / D)")]
        [SerializeField, Min(0f), Tooltip("Top sideways speed, metres per second.")]
        private float steerSpeed = 6f;

        [SerializeField, Min(0.01f), Tooltip("Sideways acceleration, metres per second squared.")]
        private float steerAcceleration = 16f;

        [Header("Height (W = up, S = down)")]
        [SerializeField, Min(0f), Tooltip("Constant gentle sink with no vertical input, metres per second.")]
        private float sinkSpeed = 1.1f;

        [SerializeField, Min(0f), Tooltip("Climb rate holding W (net, below the ceiling band), metres per second.")]
        private float climbSpeed = 2.6f;

        [SerializeField, Min(0f), Tooltip("Dive rate holding S, metres per second.")]
        private float diveSpeed = 5f;

        [SerializeField, Min(0.01f), Tooltip("How quickly the vertical speed follows its target, metres per second squared.")]
        private float verticalAcceleration = 7f;

        [SerializeField, Min(0f), Tooltip("Upward speed kept from a jump when the glide starts, metres per second.")]
        private float maxEntryClimbSpeed = 1.5f;

        [Header("Landing / turning")]
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the horizontal glide speed kept when the glide ends on a landing ledge.")]
        private float landingSpeedKeep = 0.5f;

        [SerializeField, Min(0f), Tooltip("How fast the player turns to face the flight direction, degrees per second.")]
        private float turnSpeedDegrees = 360f;

        [Header("Glider visual")]
        [SerializeField, Tooltip("Glider model shown on the player's back while gliding (visual only, no colliders). Shared between zones.")]
        private Transform gliderVisual;

        [SerializeField, Tooltip("Glider origin (between its two handle poles, at grip height) on the player, local metres (player origin is at the feet): Sam's hands raised up and out.")]
        private Vector3 gliderOffset = new Vector3(0f, 1.26f, 0.08f);

        [SerializeField, Range(0f, 60f), Tooltip("Glider roll at full steering, degrees.")]
        private float maxBankDegrees = 25f;

        [SerializeField, Range(0f, 45f), Tooltip("Largest glider pitch (nose up climbing, nose down diving), degrees.")]
        private float maxPitchDegrees = 15f;

        [Header("Camera")]
        [SerializeField, Min(0.5f), Tooltip("Camera distance behind the player while gliding, metres.")]
        private float cameraDistance = 6f;

        [SerializeField, Range(-20f, 60f), Tooltip("Camera pitch while gliding, degrees (low, so the canyon ahead is in view).")]
        private float cameraPitch = 10f;

        [SerializeField, Min(0.1f), Tooltip("How quickly the camera eases behind the glider (per second).")]
        private float cameraSharpness = 3f;

        /// <summary>World height (feet) the glide cannot climb above.</summary>
        public float CeilingY => transform.position.y + ceilingHeight;
        /// <summary>Climb fade band below the ceiling, metres.</summary>
        public float CeilingSoftBand => ceilingSoftBand;
        /// <summary>Forced sink above the ceiling, metres per second.</summary>
        public float CeilingPushSpeed => ceilingPushSpeed;
        /// <summary>Level-flight forward speed, metres per second.</summary>
        public float CruiseSpeed => cruiseSpeed;
        /// <summary>Forward speed at full climb, metres per second.</summary>
        public float ClimbForwardSpeed => climbForwardSpeed;
        /// <summary>Forward speed at full dive, metres per second.</summary>
        public float DiveForwardSpeed => diveForwardSpeed;
        /// <summary>Minimum forward speed at launch, metres per second.</summary>
        public float EntryForwardSpeed => entryForwardSpeed;
        /// <summary>Forward acceleration, metres per second squared.</summary>
        public float ForwardAcceleration => forwardAcceleration;
        /// <summary>Top sideways speed, metres per second.</summary>
        public float SteerSpeed => steerSpeed;
        /// <summary>Sideways acceleration, metres per second squared.</summary>
        public float SteerAcceleration => steerAcceleration;
        /// <summary>Sink with no vertical input, metres per second.</summary>
        public float SinkSpeed => sinkSpeed;
        /// <summary>Climb holding W, metres per second.</summary>
        public float ClimbSpeed => climbSpeed;
        /// <summary>Dive holding S, metres per second.</summary>
        public float DiveSpeed => diveSpeed;
        /// <summary>Vertical acceleration, metres per second squared.</summary>
        public float VerticalAcceleration => verticalAcceleration;
        /// <summary>Upward speed kept from a jump at launch, metres per second.</summary>
        public float MaxEntryClimbSpeed => maxEntryClimbSpeed;
        /// <summary>Fraction of horizontal speed kept on landing.</summary>
        public float LandingSpeedKeep => landingSpeedKeep;
        /// <summary>Turn rate toward the flight direction, degrees per second.</summary>
        public float TurnSpeedDegrees => turnSpeedDegrees;
        /// <summary>The glider model to show, or null.</summary>
        public Transform GliderVisual => gliderVisual;
        /// <summary>Glider position on the player, local metres.</summary>
        public Vector3 GliderOffset => gliderOffset;
        /// <summary>Glider roll at full steering, degrees.</summary>
        public float MaxBankDegrees => maxBankDegrees;
        /// <summary>Largest glider pitch, degrees.</summary>
        public float MaxPitchDegrees => maxPitchDegrees;
        /// <summary>Camera distance while gliding, metres.</summary>
        public float CameraDistance => cameraDistance;
        /// <summary>Camera pitch while gliding, degrees.</summary>
        public float CameraPitch => cameraPitch;
        /// <summary>Camera easing rate while gliding, per second.</summary>
        public float CameraSharpness => cameraSharpness;

        private void OnTriggerEnter(Collider other) => TryStart(other);

        private void OnTriggerStay(Collider other) => TryStart(other);

        private void TryStart(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.MovementSuspended || player.IsGrounded || !player.ControlEnabled) return;
            if (!player.TryGetComponent(out GlideMode glide)) glide = player.gameObject.AddComponent<GlideMode>();
            glide.Begin(this);
        }
    }
}
