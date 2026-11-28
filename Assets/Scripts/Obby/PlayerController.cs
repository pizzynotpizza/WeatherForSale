using UnityEngine;
using UnityEngine.InputSystem;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Third-person character controller for the obby. Camera-relative walking and jumping on a
    /// <see cref="CharacterController"/>. Reads keyboard / gamepad through the Input System and
    /// also accepts virtual (touch) input through <see cref="SetVirtualMove"/> and
    /// <see cref="RequestJump"/>, so touch and keyboard drive the very same controller.
    /// Implements: design/game-brief.md Controls, story-001 criterion 1.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Tunable movement values.")]
        private ObbyLevelConfig config;

        [SerializeField, Tooltip("Camera whose yaw defines 'forward'. Only its horizontal facing is used.")]
        private Transform cameraReference;

        [SerializeField, Min(0f), Tooltip("Safety limit, metres per frame: if the surface under the player moves further than this in one frame (a platform snapping back), the carry is skipped.")]
        private float maxCarryDistance = 2f;

        [SerializeField, Range(0f, 1f), Tooltip("A contact counts as ground when its normal points up at least this much (1 = flat floor only).")]
        private float groundNormalMinY = 0.5f;

        [Header("Slippery slopes")]
        [SerializeField, Min(0f), Tooltip("Multiplier on the downhill pull on a tilted slippery surface. The pull is gravity projected on the slope, times (1 - Traction). 0 = no sliding.")]
        private float slideGravityScale = 1f;

        [SerializeField, Min(0.1f), Tooltip("Drag on the slide, per second. Standing still, the player slides down at (downhill pull / drag); running uphill nets moveSpeed minus that.")]
        private float slideDrag = 2f;

        [SerializeField, Range(0f, 45f), Tooltip("Surfaces tilted less than this many degrees count as flat (no slide).")]
        private float slideMinAngle = 3f;

        // Ground probe for the slope normal: a ray from this high above the feet, reaching this far below them.
        private const float SlopeProbeHeight = 0.5f;
        private const float SlopeProbeDepth = 0.4f;

        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction jumpAction;

        private Vector2 virtualMove;
        private bool virtualJumpHeld;
        private float horizontalSpeedX;
        private float horizontalSpeedZ;
        private float verticalSpeed;
        private float timeSinceGrounded = float.MaxValue;
        private float jumpBufferRemaining;

        // Moving-platform support: the surface the player stood on after the last move, and the player's
        // position in that surface's local space (so translation and rotation of the surface carry them).
        private RideableSurface surface;
        private Transform carrier;
        private Vector3 carrierLocalPoint;
        private RideableSurface groundSurfaceHit;
        private Collider lastGroundCollider;
        private RideableSurface lastGroundSurface;
        private Vector3 externalVelocity;
        private float pendingLiftAcceleration;
        private float pendingLiftMaxSpeed;

        // Downhill slide on a tilted slippery surface (horizontal, world space). Zero everywhere else, so
        // normal ground and flat ice behave exactly as without it.
        private Vector3 slideVelocity;

        /// <summary>The <see cref="RideableSurface"/> the player is standing on, or null (static ground or airborne).</summary>
        public RideableSurface CurrentSurface => surface;

        /// <summary>When false the player ignores all input and stands still (game over / level complete).</summary>
        public bool ControlEnabled { get; set; } = true;

        /// <summary>True if the controller was touching the ground at the end of the last update.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Current velocity in world space (metres per second), as applied in the last update (own movement plus any slope slide).</summary>
        public Vector3 Velocity => new Vector3(horizontalSpeedX + slideVelocity.x, verticalSpeed, horizontalSpeedZ + slideVelocity.z);

        /// <summary>Top horizontal speed from the config, used to normalise <see cref="Velocity"/> for animation.</summary>
        public float MaxMoveSpeed => config != null ? config.moveSpeed : 1f;

        /// <summary>Raised on the frame a jump actually starts (after coyote time / jump buffer checks).</summary>
        public event System.Action Jumped;

        /// <summary>
        /// True while another component (the glide, <see cref="GlideMode"/>) moves the player: walking, jumping,
        /// gravity and pushes are paused here. See <see cref="SuspendMovement"/> / <see cref="ResumeMovement"/>.
        /// </summary>
        public bool MovementSuspended { get; private set; }

        /// <summary>Raised at the end of every <see cref="TeleportTo"/> (respawn). An external movement mode ends itself here.</summary>
        public event System.Action Teleported;

        /// <summary>
        /// The current move input from the same Input System Move action (WASD, arrows, left stick) plus the
        /// on-screen stick: x = right, y = forward, length 0..1. Zero while <see cref="ControlEnabled"/> is false.
        /// </summary>
        public Vector2 MoveInput
        {
            get
            {
                if (!ControlEnabled || moveAction == null) return Vector2.zero;
                return Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>() + virtualMove, 1f);
            }
        }

        /// <summary>
        /// True while the jump key / gamepad button or the on-screen jump button is held (the glide climbs on it).
        /// False while <see cref="ControlEnabled"/> is false.
        /// </summary>
        public bool JumpHeld => ControlEnabled && ((jumpAction != null && jumpAction.IsPressed()) || virtualJumpHeld);

        /// <summary>Sets whether the on-screen jump button is held (touch controls call it on press and release).</summary>
        public void SetVirtualJumpHeld(bool held)
        {
            virtualJumpHeld = held;
        }

        /// <summary>
        /// Hands movement to another component until <see cref="ResumeMovement"/> (or a respawn): this controller
        /// stops moving the CharacterController, leaves the surface it rode and counts as airborne.
        /// </summary>
        public void SuspendMovement()
        {
            if (MovementSuspended) return;
            MovementSuspended = true;
            SetSurface(null);
            IsGrounded = false;
            slideVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            timeSinceGrounded = float.MaxValue;
            jumpBufferRemaining = 0f;
        }

        /// <summary>Takes movement back after <see cref="SuspendMovement"/>, starting from <paramref name="velocity"/> (world, metres per second).</summary>
        public void ResumeMovement(Vector3 velocity)
        {
            MovementSuspended = false;
            horizontalSpeedX = velocity.x;
            horizontalSpeedZ = velocity.z;
            verticalSpeed = velocity.y;
            timeSinceGrounded = float.MaxValue; // no coyote jump out of a glide
            jumpBufferRemaining = 0f;
        }

        /// <summary>While movement is suspended, sets what <see cref="Velocity"/> reports (animation), metres per second.</summary>
        public void ReportSuspendedVelocity(Vector3 velocity)
        {
            if (!MovementSuspended) return;
            horizontalSpeedX = velocity.x;
            horizontalSpeedZ = velocity.z;
            verticalSpeed = velocity.y;
        }

        /// <summary>
        /// Injects the config and camera when the player is created from code. Optional when the
        /// serialized fields are already assigned.
        /// </summary>
        public void Initialize(ObbyLevelConfig levelConfig, Transform cameraTransform)
        {
            config = levelConfig;
            cameraReference = cameraTransform;
        }

        /// <summary>Sets the on-screen stick value (x = right, y = forward, length 0..1). Pass zero to release.</summary>
        public void SetVirtualMove(Vector2 value)
        {
            virtualMove = Vector2.ClampMagnitude(value, 1f);
        }

        /// <summary>Queues a jump, as if the jump key had just been pressed (on-screen jump button).</summary>
        public void RequestJump()
        {
            if (config != null) jumpBufferRemaining = config.jumpBufferTime;
        }

        /// <summary>
        /// Gives the player an instant velocity change (bounce pad, launcher). Horizontal parts are added
        /// to the current horizontal speed (air control then pulls it back toward the input); a non-zero
        /// vertical part replaces the vertical speed, and an upward one lifts the player off the ground
        /// without coyote time.
        /// </summary>
        public void Launch(Vector3 velocity)
        {
            horizontalSpeedX += velocity.x;
            horizontalSpeedZ += velocity.z;
            if (Mathf.Approximately(velocity.y, 0f)) return;

            verticalSpeed = velocity.y;
            if (velocity.y > 0f)
            {
                IsGrounded = false;
                timeSinceGrounded = float.MaxValue;
                jumpBufferRemaining = 0f;
                Jumped?.Invoke(); // plays the jump animation for bounces too
            }
        }

        /// <summary>
        /// Adds a drift velocity (metres per second, world space) for the next move only: call it every
        /// frame while it applies (wind gust, current). It moves the player without changing their own speed.
        /// </summary>
        public void AddExternalVelocity(Vector3 velocity)
        {
            externalVelocity += velocity;
        }

        /// <summary>
        /// Adds upward acceleration for the next update only (wind vent): call it every frame while it
        /// applies. It is added on top of gravity, so it must exceed gravity to lift; it never pushes
        /// the vertical speed above <paramref name="maxUpSpeed"/>. A grounded player is lifted off the floor.
        /// </summary>
        public void AddLift(float acceleration, float maxUpSpeed)
        {
            pendingLiftAcceleration = Mathf.Max(pendingLiftAcceleration, acceleration);
            pendingLiftMaxSpeed = Mathf.Max(pendingLiftMaxSpeed, maxUpSpeed);
        }

        /// <summary>
        /// Moves the player instantly to a position and facing, clearing all momentum.
        /// Used for respawning; does not touch the level timer.
        /// </summary>
        public void TeleportTo(Vector3 position, float yawDegrees)
        {
            SetSurface(null);
            externalVelocity = Vector3.zero;
            slideVelocity = Vector3.zero;

            // A CharacterController overrides transform changes while enabled.
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            characterController.enabled = true;

            horizontalSpeedX = 0f;
            horizontalSpeedZ = 0f;
            verticalSpeed = 0f;
            jumpBufferRemaining = 0f;
            timeSinceGrounded = float.MaxValue;

            // A respawn always ends an external movement mode (glide).
            MovementSuspended = false;
            Teleported?.Invoke();
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            SetSurface(null);
            moveAction.Disable();
            jumpAction.Disable();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < groundNormalMinY) return;

            Collider ground = hit.collider;
            if (ground != lastGroundCollider)
            {
                lastGroundCollider = ground;
                lastGroundSurface = ground != null ? ground.GetComponentInParent<RideableSurface>() : null;
            }
            if (lastGroundSurface != null) groundSurfaceHit = lastGroundSurface;
        }

        /// <summary>Switches the surface the player rides, telling the old and new one, and re-samples the carry point.</summary>
        private void SetSurface(RideableSurface next)
        {
            if (next != surface)
            {
                RideableSurface previous = surface;
                surface = next;
                if (previous != null) previous.SetRider(null);
                if (next != null) next.SetRider(this);
            }

            carrier = surface != null ? surface.transform : null;
            if (carrier != null) carrierLocalPoint = carrier.InverseTransformPoint(transform.position);
        }

        /// <summary>
        /// Face normal of the ground straight under the player, when it belongs to the surface they stand on
        /// and is tilted between <see cref="slideMinAngle"/> and the walkable limit. A ray (not the contact
        /// normal) so standing on a pad's edge never reads as a slope.
        /// </summary>
        private bool TryGetSlopeNormal(out Vector3 normal)
        {
            normal = Vector3.up;
            Vector3 origin = transform.position + Vector3.up * SlopeProbeHeight; // inside the capsule: the ray ignores it
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, SlopeProbeHeight + SlopeProbeDepth,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;

            RideableSurface hitSurface = hit.collider == lastGroundCollider ? lastGroundSurface : hit.collider.GetComponentInParent<RideableSurface>();
            if (hitSurface != surface) return false;
            if (hit.normal.y < groundNormalMinY || hit.normal.y > Mathf.Cos(slideMinAngle * Mathf.Deg2Rad)) return false;
            normal = hit.normal;
            return true;
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            jumpAction?.Dispose();
        }

        private void Update()
        {
            if (config == null) return;

            if (MovementSuspended)
            {
                // Another component (glide) moves the player; drop per-frame pushes so they do not pile up.
                externalVelocity = Vector3.zero;
                pendingLiftAcceleration = 0f;
                pendingLiftMaxSpeed = 0f;
                jumpBufferRemaining = 0f;
                return;
            }

            float dt = Time.deltaTime;

            Vector2 input = Vector2.zero;
            if (ControlEnabled)
            {
                input = moveAction.ReadValue<Vector2>() + virtualMove;
                input = Vector2.ClampMagnitude(input, 1f);
                if (jumpAction.WasPressedThisFrame()) jumpBufferRemaining = config.jumpBufferTime;
            }
            else
            {
                jumpBufferRemaining = 0f;
            }

            // Camera-relative move direction on the horizontal plane.
            Vector3 moveDir = Vector3.zero;
            if (input.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = cameraReference != null ? cameraReference.forward : transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
                forward.Normalize();
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                moveDir = forward * input.y + right * input.x;
            }

            // Tilted slippery ground (story-004, Glacier 2 snow ramp): gravity projected on the slope plane,
            // times (1 - Traction), pulls the player downhill; drag caps it at a terminal slide speed. The slide
            // is kept in the air and handed to the normal movement on any other ground (grip / ice then apply).
            float slopeStick = 0f;
            if (IsGrounded && surface != null && surface.Traction < 1f && TryGetSlopeNormal(out Vector3 slopeNormal))
            {
                float pull = config.gravity * slideGravityScale * (1f - surface.Traction) * slopeNormal.y;
                slideVelocity += new Vector3(slopeNormal.x, 0f, slopeNormal.z) * (pull * dt);
                slideVelocity -= slideVelocity * Mathf.Min(1f, slideDrag * dt);

                // Moving downhill, push down a little harder so the controller stays on the slope.
                Vector3 downhill = new Vector3(slopeNormal.x, 0f, slopeNormal.z);
                float tilt = downhill.magnitude;
                if (tilt > 0.0001f)
                {
                    float downhillSpeed = Vector3.Dot(new Vector3(horizontalSpeedX, 0f, horizontalSpeedZ) + slideVelocity, downhill / tilt);
                    slopeStick = Mathf.Max(0f, downhillSpeed) * (tilt / Mathf.Max(0.05f, slopeNormal.y));
                }
            }
            else if (IsGrounded && slideVelocity.sqrMagnitude > 0f)
            {
                horizontalSpeedX += slideVelocity.x;
                horizontalSpeedZ += slideVelocity.z;
                slideVelocity = Vector3.zero;
            }

            // Horizontal velocity: accelerate toward the target (less control in the air).
            // Slippery ground (ice) scales the ground acceleration down: the player keeps their momentum.
            float grip = IsGrounded && surface != null ? surface.Traction : 1f;
            float accel = config.groundAcceleration * (IsGrounded ? grip : config.airControl);
            float maxDelta = accel * dt;
            float targetX = moveDir.x * config.moveSpeed;
            float targetZ = moveDir.z * config.moveSpeed;
            float dx = targetX - horizontalSpeedX;
            float dz = targetZ - horizontalSpeedZ;
            float deltaLen = Mathf.Sqrt(dx * dx + dz * dz);
            if (deltaLen <= maxDelta || deltaLen < 0.0001f)
            {
                horizontalSpeedX = targetX;
                horizontalSpeedZ = targetZ;
            }
            else
            {
                float k = maxDelta / deltaLen;
                horizontalSpeedX += dx * k;
                horizontalSpeedZ += dz * k;
            }

            // Vertical: coyote time + jump buffer + gravity.
            if (IsGrounded) timeSinceGrounded = 0f;
            else timeSinceGrounded += dt;

            jumpBufferRemaining -= dt;
            bool canJump = timeSinceGrounded <= config.coyoteTime;
            if (jumpBufferRemaining > 0f && canJump)
            {
                verticalSpeed = config.jumpSpeed;
                jumpBufferRemaining = 0f;
                timeSinceGrounded = float.MaxValue; // consume coyote window: no double jump
                Jumped?.Invoke();
            }
            else if (IsGrounded && verticalSpeed <= 0f)
            {
                verticalSpeed = -(config.groundedStickSpeed + slopeStick);
            }
            else
            {
                verticalSpeed = Mathf.Max(verticalSpeed - config.gravity * dt, -config.maxFallSpeed);
            }

            // Wind vent lift (on top of gravity / the grounded stick).
            if (pendingLiftAcceleration > 0f)
            {
                if (IsGrounded && verticalSpeed < 0f) verticalSpeed = 0f;
                if (verticalSpeed < pendingLiftMaxSpeed)
                {
                    verticalSpeed = Mathf.Min(verticalSpeed + pendingLiftAcceleration * dt, pendingLiftMaxSpeed);
                }
                pendingLiftAcceleration = 0f;
                pendingLiftMaxSpeed = 0f;
            }

            // Riding a moving surface: follow its movement since the last frame (plus its own drift).
            Vector3 carry = Vector3.zero;
            if (IsGrounded && surface != null && carrier != null)
            {
                Physics.SyncTransforms(); // let the controller see the platform where it is drawn
                carry = carrier.TransformPoint(carrierLocalPoint) - transform.position;
                carry += surface.SurfaceVelocity * dt;
                if (carry.sqrMagnitude > maxCarryDistance * maxCarryDistance) carry = Vector3.zero;
            }

            Vector3 velocity = new Vector3(horizontalSpeedX + slideVelocity.x, verticalSpeed, horizontalSpeedZ + slideVelocity.z);
            Vector3 motion = (velocity + externalVelocity) * dt + carry;
            externalVelocity = Vector3.zero;
            groundSurfaceHit = null;
            characterController.Move(motion);
            IsGrounded = characterController.isGrounded;
            SetSurface(IsGrounded ? groundSurfaceHit : null);

            // Bumping into a ceiling cancels upward speed.
            if ((characterController.collisionFlags & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            {
                verticalSpeed = 0f;
            }

            // Face the direction of travel.
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, config.turnSpeed * dt);
            }
        }
    }
}
