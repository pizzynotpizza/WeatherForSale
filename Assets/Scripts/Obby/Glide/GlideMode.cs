using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// The player's glide. Added to the player by the first <see cref="GlideZone"/> they launch into, then
    /// reused. While gliding the normal walk / jump / gravity of <see cref="PlayerController"/> is suspended
    /// and this component moves the <see cref="CharacterController"/> itself: automatic forward flight along
    /// the zone's forward, A / D steers sideways, W (or holding jump: the JUMP button on phones) climbs (never
    /// above the zone's ceiling), S dives, and a
    /// gentle constant sink otherwise. Climbing costs forward speed, diving gains it.
    /// The glide ends when the player touches ground (lands), enters a <see cref="GlideLandingZone"/>,
    /// bumps a solid side (stalls and drops), or respawns (<see cref="PlayerController.TeleportTo"/>).
    /// Input comes from <see cref="PlayerController.MoveInput"/> (the same Input System actions and touch stick).
    /// Implements: story-004, Valley 1 "Canyon glide".
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class GlideMode : MonoBehaviour
    {
        /// <summary>Glider pitch per metre per second of vertical speed, degrees (clamped by the zone's max pitch).</summary>
        private const float PitchDegreesPerSpeed = 4f;

        private PlayerController player;
        private CharacterController motor;
        private ThirdPersonCamera followCamera;
        private GlideZone zone;
        private Transform glider;

        private Vector3 forward = Vector3.forward;
        private Vector3 right = Vector3.right;
        private float ceilingY;
        private float forwardSpeed;
        private float sideSpeed;
        private float verticalSpeed;
        private float updraftSpeed;
        private float updraftAcceleration;

        /// <summary>True while the player is gliding.</summary>
        public bool IsGliding { get; private set; }

        /// <summary>Current glide velocity, world space, metres per second (zero when not gliding).</summary>
        public Vector3 Velocity => IsGliding ? forward * forwardSpeed + right * sideSpeed + Vector3.up * verticalSpeed : Vector3.zero;

        /// <summary>The glider model while gliding (its origin is between its two handle poles at grip height, X across to the poles), else null.</summary>
        public Transform Glider => IsGliding ? glider : null;

        /// <summary>World feet height the current glide cannot climb above.</summary>
        public float CeilingY => ceilingY;

        /// <summary>Raised when a glide starts.</summary>
        public event Action GlideStarted;

        /// <summary>Raised when a glide ends; true when it ended by landing (ground or landing zone).</summary>
        public event Action<bool> GlideEnded;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            motor = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (player != null) player.Teleported += OnTeleported;
        }

        private void OnDisable()
        {
            if (player != null) player.Teleported -= OnTeleported;
            End(false);
        }

        /// <summary>
        /// Starts a glide with the tuning of <paramref name="from"/>. Ignored while already gliding or while
        /// player control is off. Returns true when a glide started.
        /// </summary>
        public bool Begin(GlideZone from)
        {
            if (IsGliding || from == null || player == null || !player.ControlEnabled) return false;

            zone = from;
            forward = from.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            right = new Vector3(forward.z, 0f, -forward.x);
            ceilingY = from.CeilingY;

            Vector3 v = player.Velocity;
            forwardSpeed = Mathf.Clamp(Vector3.Dot(v, forward), from.EntryForwardSpeed, Mathf.Max(from.EntryForwardSpeed, from.DiveForwardSpeed));
            sideSpeed = Mathf.Clamp(Vector3.Dot(v, right), -from.SteerSpeed, from.SteerSpeed);
            verticalSpeed = Mathf.Clamp(v.y, -from.DiveSpeed, from.MaxEntryClimbSpeed);
            ClearUpdraft();

            player.SuspendMovement();
            IsGliding = true;
            AttachGlider(from.GliderVisual, from.GliderOffset);

            if (followCamera == null)
            {
                Camera main = Camera.main;
                if (main != null) main.TryGetComponent(out followCamera);
            }
            if (followCamera != null)
            {
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                followCamera.BeginAutoFollow(yaw, from.CameraPitch, from.CameraDistance, from.CameraSharpness);
            }

            GlideStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Ends the glide and hands control back to <see cref="PlayerController"/>. A landing keeps part of the
        /// horizontal speed (<see cref="GlideZone.LandingSpeedKeep"/>); a stall drops straight down.
        /// Does nothing when not gliding.
        /// </summary>
        public void End(bool landed)
        {
            if (!IsGliding) return;
            IsGliding = false;

            Vector3 horizontal = forward * forwardSpeed + right * sideSpeed;
            float keep = zone != null ? zone.LandingSpeedKeep : 0.5f;
            Vector3 handover = (landed ? horizontal * keep : Vector3.zero) + Vector3.up * Mathf.Min(verticalSpeed, 0f);
            // After a respawn the controller has already taken back control (and cleared all momentum).
            if (player != null && player.MovementSuspended) player.ResumeMovement(handover);

            if (glider != null)
            {
                glider.localRotation = Quaternion.identity;
                glider.gameObject.SetActive(false);
            }
            if (followCamera != null) followCamera.EndAutoFollow();

            zone = null;
            forwardSpeed = 0f;
            sideSpeed = 0f;
            verticalSpeed = 0f;
            ClearUpdraft();
            GlideEnded?.Invoke(landed);
        }

        /// <summary>
        /// Lifts the glider for the next update only (an updraft column): call it every frame while it applies.
        /// The vertical speed is pulled up toward <paramref name="upSpeed"/> at <paramref name="acceleration"/>.
        /// The ceiling still wins above it.
        /// </summary>
        public void AddUpdraft(float upSpeed, float acceleration)
        {
            if (!IsGliding) return;
            updraftSpeed = Mathf.Max(updraftSpeed, upSpeed);
            updraftAcceleration = Mathf.Max(updraftAcceleration, acceleration);
        }

        private void ClearUpdraft()
        {
            updraftSpeed = 0f;
            updraftAcceleration = 0f;
        }

        private void OnTeleported()
        {
            End(false);
        }

        private void Update()
        {
            if (!IsGliding) return;
            if (zone == null)
            {
                End(false);
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Level over (timer, finish): hold still in the air until the scene changes.
            if (!player.ControlEnabled)
            {
                player.ReportSuspendedVelocity(Vector3.zero);
                ClearUpdraft();
                return;
            }

            Vector2 input = player.MoveInput;

            // A / D: sideways.
            sideSpeed = Mathf.MoveTowards(sideSpeed, input.x * zone.SteerSpeed, zone.SteerAcceleration * dt);

            // W / S: climb or dive around a gentle sink; W fades out near the ceiling, above it the air pushes down.
            float y = transform.position.y;
            float climbRoom = Mathf.Clamp01((ceilingY - y) / Mathf.Max(0.01f, zone.CeilingSoftBand));
            // Holding jump (the on-screen JUMP button on phones) climbs too, at full rate.
            float lift = player.JumpHeld ? 1f : input.y;
            float targetVertical = lift >= 0f
                ? Mathf.Lerp(-zone.SinkSpeed, zone.ClimbSpeed * climbRoom, lift)
                : Mathf.Lerp(-zone.SinkSpeed, -zone.DiveSpeed, -lift);
            float verticalAccel = zone.VerticalAcceleration;
            if (updraftSpeed > 0f)
            {
                targetVertical = Mathf.Max(targetVertical, updraftSpeed);
                verticalAccel = Mathf.Max(verticalAccel, updraftAcceleration);
            }
            if (y > ceilingY) targetVertical = Mathf.Min(targetVertical, -zone.CeilingPushSpeed);
            ClearUpdraft();
            verticalSpeed = Mathf.MoveTowards(verticalSpeed, targetVertical, verticalAccel * dt);

            // Forward: automatic; climbing trades speed for height, diving gains it.
            float climbBlend = Mathf.Clamp01(verticalSpeed / Mathf.Max(0.01f, zone.ClimbSpeed));
            float diveBlend = Mathf.Clamp01((-verticalSpeed - zone.SinkSpeed) / Mathf.Max(0.01f, zone.DiveSpeed - zone.SinkSpeed));
            float targetForward = zone.CruiseSpeed
                                  + (zone.ClimbForwardSpeed - zone.CruiseSpeed) * climbBlend
                                  + (zone.DiveForwardSpeed - zone.CruiseSpeed) * diveBlend;
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForward, zone.ForwardAcceleration * dt);

            Vector3 velocity = forward * forwardSpeed + right * sideSpeed + Vector3.up * verticalSpeed;
            CollisionFlags flags = motor.Move(velocity * dt);
            if (!IsGliding) return;

            player.ReportSuspendedVelocity(velocity);
            Face(velocity, dt);
            PoseGlider();

            if ((flags & CollisionFlags.Below) != 0)
            {
                End(true); // touched down on solid ground (a landing ledge's top)
                return;
            }
            if ((flags & CollisionFlags.Sides) != 0) End(false); // bumped a solid face: stall and drop
        }

        private void Face(Vector3 velocity, float dt)
        {
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            if (flat.sqrMagnitude < 0.01f) return;
            Quaternion target = Quaternion.LookRotation(flat, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, zone.TurnSpeedDegrees * dt);
        }

        private void PoseGlider()
        {
            if (glider == null) return;
            float steer = zone.SteerSpeed > 0.01f ? sideSpeed / zone.SteerSpeed : 0f;
            float roll = -steer * zone.MaxBankDegrees;
            float pitch = Mathf.Clamp(-verticalSpeed * PitchDegreesPerSpeed, -zone.MaxPitchDegrees, zone.MaxPitchDegrees);
            glider.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        private void AttachGlider(Transform visual, Vector3 offset)
        {
            if (glider != null && glider != visual) glider.gameObject.SetActive(false);
            glider = visual;
            if (glider == null) return;
            glider.SetParent(transform, false);
            glider.localPosition = offset;
            glider.localRotation = Quaternion.identity;
            glider.gameObject.SetActive(true);
        }
    }
}
