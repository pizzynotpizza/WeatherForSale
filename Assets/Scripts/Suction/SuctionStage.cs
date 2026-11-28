using UnityEngine;
using UnityEngine.Rendering;
using WeatherForSale.Obby;

namespace WeatherForSale.Suction
{
    /// <summary>
    /// In-place suction at the end of a map's level 2 (the 3D half; <see cref="SuctionMinigame"/> does
    /// the UI and input). <see cref="Begin"/> freezes Sam on the SuctionSpot, swings the camera round
    /// to his front, shows the map-coloured weather orb, puts an empty bottle in both his hands
    /// pointed at the orb and starts the streaks flowing into it. Sam goes into a pulling pose
    /// (wide braced legs, upper body leaning back and away from the orb, a strain shake, arms
    /// reaching for the bottle by two-bone IK), layered on top of his idle animation in LateUpdate.
    /// While the gauge fills the orb shrinks; when it is full the orb hides and the streaks stop.
    /// Every placement, size, angle, rate and colour comes from <see cref="SuctionConfig"/>; positions
    /// are in the spot's frame (+X Sam's right, +Z the way Sam faces).
    /// Implements: story-003 change request (suction in place at the end of level 2).
    /// </summary>
    public sealed class SuctionStage : MonoBehaviour
    {
        [SerializeField] private SuctionConfig config;
        [SerializeField] private SuctionMinigame minigame;
        [SerializeField, Tooltip("Map id of this scene: colours the weather that waits before the suction starts.")]
        private string sceneMapId = "";

        [Header("Player and camera")]
        [SerializeField] private PlayerController player;
        [SerializeField, Tooltip("Follow camera; disabled during suction.")] private ThirdPersonCamera followCamera;
        [SerializeField, Tooltip("The camera that is moved to Sam's front.")] private Transform viewCamera;
        [SerializeField, Tooltip("Where Sam stands during suction; its forward is the way he faces.")] private Transform spot;

        [Header("Props")]
        [SerializeField, Tooltip("Bottle prefab (Assets/Art/Props/Bottle/Bottle.prefab).")] private GameObject bottlePrefab;
        [SerializeField, Tooltip("The weather orb (sphere, translucent material); hidden until Begin.")] private Transform orb;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField, Tooltip("Streaks from all around, flowing into the bottle.")] private ParticleSystem surroundStreaks;
        [SerializeField, Tooltip("Streaks leaving the orb toward the bottle.")] private ParticleSystem orbStreaks;

        [Header("HUD")]
        [SerializeField, Tooltip("Obby HUD pieces hidden during suction (timer, falls, touch controls).")]
        private GameObject[] hideDuringSuction = System.Array.Empty<GameObject>();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private bool running;
        private bool finished;
        private Transform orbFollow;          // boss fight: the thing being drawn in (replaces the fixed orb spot)
        private bool moveCamera = true;
        private float cameraBlend;
        private Vector3 cameraFromPosition;
        private Quaternion cameraFromRotation;
        private Vector3 cameraToPosition;
        private Quaternion cameraToRotation;
        private Vector3 bottlePosition;   // where the streaks end: near the bottle's mouth
        private Vector3 gripPosition;     // middle of the bottle, between the hands
        private Vector3 orbPosition;
        private Transform heldBottle;
        private Quaternion frameRotation;
        private float frameYaw;
        private Vector3 frameRight;
        private Vector3 frameForward;
        private float orbSide;            // -1 when the orb is on Sam's left, +1 on his right
        private float poseWeight;
        private float spotGroundY;        // top of the ground Sam stands on during the suction
        private bool feetLocked;
        private float hipSink;            // how far the hips are lowered so both feet reach the ground
        private Vector3 lockedLeftFoot, lockedRightFoot;
        private Quaternion lockedLeftFootRotation, lockedRightFootRotation;
        private float poseTime;
        private float yank;               // 0..1, kicked to 1 by each counted press, fades out
        private float lastGauge;
        private float pullAmount;         // 0..1 this frame: how far back Sam is heaving (moves the bottle in)
        private MaterialPropertyBlock block;

        // Humanoid bones (null when the model has no Humanoid avatar: no pose, the bottle still shows).
        private Transform hips, spine, chest, leftUpperLeg, rightUpperLeg, leftLowerLeg, rightLowerLeg, leftFoot, rightFoot;
        private Transform leftUpperArm, leftLowerArm, leftHand, rightUpperArm, rightLowerArm, rightHand;

        /// <summary>True between <see cref="Begin"/> and the end of the run.</summary>
        public bool IsRunning => running;

        /// <summary>Where the mouth of the held bottle is (what the streaks flow into). Follows the bottle while Sam heaves.</summary>
        public Vector3 BottleMouth => bottlePosition;

        /// <summary>
        /// Boss fight entry: Sam keeps his place, turns to face <paramref name="target"/> and goes into the same
        /// pulling pose with the bottle in his hands; streaks flow from the target into the bottle. The follow
        /// camera is left alone. Call <see cref="End"/> when the suction is over. Ignored while already running.
        /// </summary>
        /// <param name="target">What is drawn in (the tired boss); followed every frame.</param>
        /// <param name="streakColor">Colour of the streaks.</param>
        /// <param name="targetRadius">Radius of the cloud of streaks that leave the target, metres.</param>
        public void BeginFacing(Transform target, Color streakColor, float targetRadius, Color? bottleColour = null)
        {
            heldBottleTint = bottleColour;
            if (running || config == null || player == null || target == null) return;
            Vector3 flat = target.position - player.transform.position;
            flat.y = 0f;
            float yaw = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat).eulerAngles.y : player.transform.eulerAngles.y;
            if (spot == null) spot = new GameObject("SuctionSpot").transform;
            spot.SetPositionAndRotation(player.transform.position, Quaternion.Euler(0f, yaw, 0f));
            orbFollow = target;
            // The follow camera can end up inside Tempest's huge body when he is between it and Sam (the view then shows
            // only floor and sky). For the suction the camera moves behind Sam, looking past him at the boss.
            moveCamera = followCamera != null && viewCamera != null;
            if (moveCamera)
            {
                Vector3 toBoss = flat.sqrMagnitude > 0.0001f ? flat.normalized : player.transform.forward;
                bossCameraPosition = player.transform.position - toBoss * BossCameraBack + Vector3.up * BossCameraUp;
                Vector3 look = Vector3.Lerp(player.transform.position + Vector3.up * 1.4f, target.position, 0.55f);
                bossCameraRotation = Quaternion.LookRotation(look - bossCameraPosition, Vector3.up);
                useBossCamera = true;
            }
            bossStreakColor = streakColor;
            bossTargetRadius = Mathf.Max(0.1f, targetRadius);
            Begin(null);
        }

        /// <summary>Counts one press or tap for the yank animation when no minigame drives the gauge (boss fight).</summary>
        public void NotifyPull()
        {
            yank = 1f;
        }

        /// <summary>Ends a suction started with <see cref="BeginFacing"/>: the bottle goes away, the streaks stop and Sam's animation takes over.</summary>
        public void End()
        {
            if (!running) return;
            running = false;
            finished = true;
            StopStreaks(false);
            if (heldBottle != null) Destroy(heldBottle.gameObject);
            heldBottle = null;
            if (orb != null) orb.gameObject.SetActive(false);
            orbFollow = null;
            moveCamera = true;
            heldBottleTint = null;
            if (useBossCamera && followCamera != null) followCamera.enabled = true; // back to the normal follow view
            useBossCamera = false;
        }

        private const float BossCameraBack = 6.5f;  // metres behind Sam during a boss suction
        private const float BossCameraUp = 3.2f;    // metres above his feet
        private bool useBossCamera;
        private Vector3 bossCameraPosition;
        private Quaternion bossCameraRotation;
        private Color? bossStreakColor;
        private Color? heldBottleTint;
        private float bossTargetRadius;

        private Vector3 idleOrbPosition;

        private void Awake()
        {
            if (orb != null) orb.gameObject.SetActive(false);
            StopStreaks(true);
        }

        private void Start()
        {
            // The weather waits, floating, where the suction will happen, so it is the visible goal.
            if (config == null || !config.orbVisibleBeforeSuction || orb == null || spot == null || running) return;
            idleOrbPosition = spot.TransformPoint(config.orbLocalPosition);
            orb.position = idleOrbPosition;
            orb.localScale = Vector3.one * config.orbStartDiameter;
            ApplyOrbColour(config.ForMap(sceneMapId).orbColor);
            orb.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (running || orb == null || !orb.gameObject.activeSelf || config == null) return;
            orb.position = idleOrbPosition + Vector3.up * (Mathf.Sin(Time.time * config.orbIdleBobHz * 2f * Mathf.PI) * config.orbIdleBob);
        }

        private void ApplyOrbColour(Color colour)
        {
            if (orbRenderer == null) return;
            block ??= new MaterialPropertyBlock();
            orbRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, colour);
            orbRenderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// Starts the in-place suction for <paramref name="mapId"/> (colours from the config). Call
        /// once the level is completed (player control is already off). Ignored when already started.
        /// </summary>
        public void Begin(string mapId)
        {
            if (running || config == null) return;
            running = true;
            finished = false;
            SuctionMapSettings look = config.ForMap(mapId);

            foreach (GameObject go in hideDuringSuction)
            {
                if (go != null) go.SetActive(false);
            }

            // Freeze Sam on the spot, facing the spot's forward.
            Transform frame = spot != null ? spot : (player != null ? player.transform : transform);
            if (player != null)
            {
                player.ControlEnabled = false;
                player.SetVirtualMove(Vector2.zero);
                player.TeleportTo(frame.position, frame.eulerAngles.y);
            }

            // Map suction: Sam may cross the finish mid-glide or mid-jump. End the glide and reset the animator to
            // standing idle so the pulling pose is layered on a clean pose, not on a glide or fall frame.
            if (player != null && orbFollow == null)
            {
                var glide = player.GetComponent<GlideMode>();
                if (glide != null && glide.IsGliding) glide.End(true);
                Animator animator = player.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
            }

            // Camera: stop following (this also un-hides a first-person model) and swing to the front.
            if (followCamera != null && moveCamera) followCamera.enabled = false;
            if (viewCamera != null && moveCamera)
            {
                cameraFromPosition = viewCamera.position;
                cameraFromRotation = viewCamera.rotation;
                if (useBossCamera)
                {
                    cameraToPosition = bossCameraPosition;
                    cameraToRotation = bossCameraRotation;
                }
                else
                {
                    cameraToPosition = frame.TransformPoint(config.cameraLocalPosition);
                    Vector3 look3 = frame.TransformPoint(config.cameraLookLocal) - cameraToPosition;
                    cameraToRotation = look3.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(look3, Vector3.up) : viewCamera.rotation;
                }
                cameraBlend = config.cameraMoveSeconds > 0f ? 0f : 1f;
                ApplyCamera();
            }

            frameYaw = frame.eulerAngles.y;
            spotGroundY = frame.position.y;
            feetLocked = false;
            hipSink = 0f;
            frameRotation = Quaternion.Euler(0f, frameYaw, 0f);
            frameRight = frameRotation * Vector3.right;
            frameForward = frameRotation * Vector3.forward;
            orbPosition = orbFollow != null ? orbFollow.position : frame.TransformPoint(config.orbLocalPosition);
            orbSide = frame.InverseTransformPoint(orbPosition).x < 0f ? -1f : 1f;
            FindBones();
            poseWeight = 0f;
            poseTime = 0f;
            yank = 0f;
            lastGauge = 0f;
            SpawnBottle();

            Color streakColour = bossStreakColor ?? look.streakColor;
            float orbRadius = orbFollow != null ? bossTargetRadius : config.orbStartDiameter * 0.5f;
            if (orb != null && orbFollow == null)
            {
                orb.position = orbPosition;
                orb.localScale = Vector3.one * config.orbStartDiameter;
                orb.gameObject.SetActive(true);
            }
            if (orbFollow == null) ApplyOrbColour(look.orbColor);

            float surroundLife = config.streakSurroundRadius / config.streakSpeed;
            SetupStreaks(surroundStreaks, Vector3.zero, config.streakSurroundRadius, config.streakSurroundRate, surroundLife, streakColour);
            float orbDistance = Vector3.Distance(orbPosition, bottlePosition);
            SetupStreaks(orbStreaks, orbPosition - bottlePosition, orbRadius, config.streakOrbRate,
                Mathf.Max(0.1f, orbDistance / config.streakSpeed), streakColour);

            if (minigame != null) minigame.Begin();
        }

        private void LateUpdate()
        {
            if (!running) return;

            if (orbFollow != null) orbPosition = orbFollow.position;
            KeepFacing();
            poseWeight = Mathf.Min(1f, poseWeight + Time.deltaTime / config.poseBlendSeconds);
            poseTime += Time.deltaTime;
            UpdateYank();
            ApplyBodyPose();
            PlaceBottle();
            ApplyArms();
            FollowBottleWithStreaks();

            if (cameraBlend < 1f && viewCamera != null)
            {
                cameraBlend = Mathf.Min(1f, cameraBlend + Time.deltaTime / config.cameraMoveSeconds);
                ApplyCamera();
            }

            if (finished || minigame == null) return;
            if (minigame.Phase == SuctionPhase.Collecting)
            {
                float t = minigame.Gauge.Normalized;
                if (orb != null) orb.localScale = Vector3.one * Mathf.Lerp(config.orbStartDiameter, config.orbEndDiameter, t);
            }
            else if (minigame.Phase != SuctionPhase.Idle)
            {
                // Gauge full: the weather is in the bottle.
                finished = true;
                if (orb != null) orb.gameObject.SetActive(false);
                StopStreaks(false);
            }
        }

        private void ApplyCamera()
        {
            float s = Mathf.SmoothStep(0f, 1f, cameraBlend);
            viewCamera.SetPositionAndRotation(
                Vector3.Lerp(cameraFromPosition, cameraToPosition, s),
                Quaternion.Slerp(cameraFromRotation, cameraToRotation, s));
        }

        private void SpawnBottle()
        {
            if (bottlePrefab != null)
            {
                GameObject bottle = Instantiate(bottlePrefab);
                bottle.name = "HeldBottle";
                foreach (Collider c in bottle.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (Renderer r in bottle.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
                bottle.transform.localScale = bottlePrefab.transform.localScale * config.bottleScale;
                heldBottle = bottle.transform;
                if (heldBottleTint.HasValue) TintBottle(bottle, heldBottleTint.Value); // purple glow in the boss fight only; maps use the plain empty bottle
            }
            PlaceBottle();
        }

        /// <summary>Colours the held bottle (the purple bottle of the boss fight) and gives it a soft glow.</summary>
        private static void TintBottle(GameObject bottle, Color colour)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, colour);
            block.SetColor("_Color", colour);
            block.SetColor("_EmissionColor", colour * 0.8f);
            foreach (Renderer r in bottle.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(block);
            var glowGo = new GameObject("BottleGlow");
            glowGo.transform.SetParent(bottle.transform, false);
            glowGo.transform.localPosition = Vector3.up * 0.2f;
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = colour;
            glow.intensity = 2.2f;
            glow.range = 4f;
        }

        private void FindBones()
        {
            Animator animator = player != null ? player.GetComponentInChildren<Animator>() : null;
            if (animator == null || !animator.isHuman) return;
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        /// <summary>Holds Sam on the spot's facing, whatever else touches his rotation.</summary>
        private void KeepFacing()
        {
            if (player != null) player.transform.rotation = frameRotation;
        }

        /// <summary>Legs braced wide (front leg toward the orb), upper body leaning back and away, shaking.</summary>
        private void ApplyBodyPose()
        {
            float w = Mathf.SmoothStep(0f, 1f, poseWeight);
            float phase = poseTime * config.poseShakeHz * 2f * Mathf.PI;
            float shake = (Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 1.7f + 1.3f)) * config.poseShakeDegrees;

            // Sam's idle clip stands turned about 30-45 degrees; square the body to the spot's facing
            // (shoulder line along Sam's right) so he faces the camera, not side-on.
            if (hips != null && leftUpperArm != null && rightUpperArm != null)
            {
                Vector3 shoulders = Vector3.ProjectOnPlane(rightUpperArm.position - leftUpperArm.position, Vector3.up);
                if (shoulders.sqrMagnitude > 0.0001f) Rotate(hips, Vector3.up, Vector3.SignedAngle(shoulders, frameRight, Vector3.up));
            }

            // Ankle heights from the idle animation (feet on the ground), before the legs are swung out:
            // the swing lifts the feet, so they are put back down to these heights below.
            float groundY = player != null ? player.transform.position.y : transform.position.y;
            float leftAnkle = leftFoot != null ? Mathf.Max(0f, leftFoot.position.y - groundY) : 0f;
            float rightAnkle = rightFoot != null ? Mathf.Max(0f, rightFoot.position.y - groundY) : 0f;

            Transform frontLeg = orbSide < 0f ? leftUpperLeg : rightUpperLeg;
            Transform backLeg = orbSide < 0f ? rightUpperLeg : leftUpperLeg;
            Rotate(leftUpperLeg, frameForward, -config.poseLegSpreadDegrees * w);
            Rotate(rightUpperLeg, frameForward, config.poseLegSpreadDegrees * w);
            Rotate(frontLeg, frameRight, -config.poseLegStrideDegrees * w);
            Rotate(backLeg, frameRight, config.poseLegStrideDegrees * w);

            // Tug of war: a slow heave back and forth, plus a yank on every press.
            float heave = Mathf.Sin(poseTime * config.poseHeaveHz * 2f * Mathf.PI);
            float lean = config.poseLeanBackDegrees + heave * config.poseHeaveDegrees + yank * config.poseYankDegrees + shake;
            float maxLean = config.poseLeanBackDegrees + config.poseHeaveDegrees + config.poseYankDegrees;
            pullAmount = maxLean > 0f ? Mathf.Clamp01((lean - (config.poseLeanBackDegrees - config.poseHeaveDegrees)) / (maxLean - (config.poseLeanBackDegrees - config.poseHeaveDegrees))) * w : 0f;

            // The feet stay planted: remember where they are before the body rocks.
            bool feet = leftFoot != null && rightFoot != null;
            Vector3 leftFootPosition = feet ? leftFoot.position : Vector3.zero;
            Vector3 rightFootPosition = feet ? rightFoot.position : Vector3.zero;
            // Planted: both feet on the ground (Sam stands on flat ground during the suction).
            // Measured against the root (the animation's ground), placed on the spot's surface: the
            // CharacterController stands a skin width above the real ground.
            leftFootPosition.y = spotGroundY + leftAnkle;
            rightFootPosition.y = spotGroundY + rightAnkle;
            Quaternion leftFootRotation = feet ? leftFoot.rotation : Quaternion.identity;
            Quaternion rightFootRotation = feet ? rightFoot.rotation : Quaternion.identity;

            // Locked: once the stance is fully in, the feet stay exactly there (the idle clip's foot
            // shuffle and the body rock never move them).
            if (feet)
            {
                if (feetLocked)
                {
                    leftFootPosition = lockedLeftFoot;
                    rightFootPosition = lockedRightFoot;
                    leftFootRotation = lockedLeftFootRotation;
                    rightFootRotation = lockedRightFootRotation;
                }
                else if (poseWeight >= 1f)
                {
                    feetLocked = true;
                    lockedLeftFoot = leftFootPosition;
                    lockedRightFoot = rightFootPosition;
                    lockedLeftFootRotation = leftFootRotation;
                    lockedRightFootRotation = rightFootRotation;
                }
            }

            // Negative about Sam's right leans back; orbSide's sign leans away from the orb.
            // Most of the lean rocks the whole body about the ankles; the rest bends the spine.
            float bodyShare = Mathf.Clamp01(config.poseBodyShare);
            float side = orbSide * config.poseLeanSideDegrees * w;
            if (hips != null && feet)
            {
                Vector3 pivot = (leftFootPosition + rightFootPosition) * 0.5f;
                Quaternion rock = Quaternion.AngleAxis(-lean * w * bodyShare, frameRight)
                                  * Quaternion.AngleAxis(side * bodyShare, frameForward);
                hips.SetPositionAndRotation(pivot + rock * (hips.position - pivot), rock * hips.rotation);
            }
            else
            {
                bodyShare = 0f;
            }
            float back = -lean * w * (1f - bodyShare) * 0.5f;
            float bend = side * (1f - bodyShare) * 0.5f;
            Rotate(spine, frameRight, back);
            Rotate(spine, frameForward, bend);
            Rotate(chest, frameRight, back);
            Rotate(chest, frameForward, bend);

            // Legs bend so the feet land back where they were, flat as before; knees point forward.
            if (feet)
            {
                // A leg that cannot reach its planted foot (body rocked back, legs spread) would leave the
                // foot hanging in the air: sink the hips just enough for both feet to reach, knees a little bent.
                // Only ever deepens (never springs back up), so the body does not bob as he rocks.
                // Capped: a wide stance would otherwise drop the hips to the floor; past the cap the feet
                // slide in toward the body (staying on the ground) so Sam keeps standing.
                hipSink = Mathf.Min(config.poseMaxHipSink, Mathf.Max(hipSink,
                    LegShortfall(leftUpperLeg, leftLowerLeg, leftFoot, leftFootPosition),
                    LegShortfall(rightUpperLeg, rightLowerLeg, rightFoot, rightFootPosition)));
                if (hipSink > 0f && hips != null) hips.position += Vector3.down * hipSink;

                Vector3 leftGoal = ReachableFoot(leftUpperLeg, leftLowerLeg, leftFoot, leftFootPosition);
                Vector3 rightGoal = ReachableFoot(rightUpperLeg, rightLowerLeg, rightFoot, rightFootPosition);
                SolveLimb(leftUpperLeg, leftLowerLeg, leftFoot, leftGoal, frameForward - frameRight * 0.3f, 1f);
                SolveLimb(rightUpperLeg, rightLowerLeg, rightFoot, rightGoal, frameForward + frameRight * 0.3f, 1f);
                leftFoot.rotation = leftFootRotation;
                rightFoot.rotation = rightFootRotation;
            }
        }

        /// <summary>
        /// How much lower the hip would have to be for this leg to reach <paramref name="goal"/> at 96% of its
        /// full length (0 when it already reaches). Vertical-only estimate; good enough for small corrections.
        /// </summary>
        private static float LegShortfall(Transform upper, Transform lower, Transform foot, Vector3 goal)
        {
            if (upper == null || lower == null || foot == null) return 0f;
            float length = (lower.position - upper.position).magnitude + (foot.position - lower.position).magnitude;
            float reach = length * 0.96f;
            Vector3 toGoal = goal - upper.position;
            float horizontal = new Vector2(toGoal.x, toGoal.z).magnitude;
            if (horizontal >= reach || toGoal.magnitude <= reach) return 0f;
            float verticalAllowed = Mathf.Sqrt(reach * reach - horizontal * horizontal);
            return Mathf.Max(0f, -toGoal.y - verticalAllowed);
        }

        /// <summary>
        /// The planted foot position, slid horizontally toward the hip (same height) when the leg is too short
        /// to reach it, so the foot stays on the ground instead of the hips sinking.
        /// </summary>
        private static Vector3 ReachableFoot(Transform upper, Transform lower, Transform foot, Vector3 goal)
        {
            float reach = ((lower.position - upper.position).magnitude + (foot.position - lower.position).magnitude) * 0.98f;
            Vector3 toGoal = goal - upper.position;
            if (toGoal.magnitude <= reach) return goal;
            float horizontalMax = Mathf.Sqrt(Mathf.Max(0f, reach * reach - toGoal.y * toGoal.y));
            Vector2 horizontal = new Vector2(toGoal.x, toGoal.z);
            if (horizontal.sqrMagnitude < 0.000001f) return goal;
            horizontal = horizontal.normalized * horizontalMax;
            return new Vector3(upper.position.x + horizontal.x, goal.y, upper.position.z + horizontal.y);
        }

        /// <summary>The bottle sits out from the chest toward the orb, tilted so its mouth leans at it.</summary>
        private void PlaceBottle()
        {
            Transform body = chest != null ? chest : spine;
            Vector3 chestPosition = body != null ? body.position
                : (player != null ? player.transform.position : transform.position) + Vector3.up * 0.9f;
            Vector3 toOrb = orbPosition - chestPosition;
            toOrb = toOrb.sqrMagnitude > 0.0001f ? toOrb.normalized : frameForward;

            float reach = Mathf.Max(0f, config.bottleHoldDistance - pullAmount * config.poseBottlePull);
            gripPosition = chestPosition + toOrb * reach + Vector3.down * config.bottleHoldDrop;
            Quaternion tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, toOrb), config.bottleTiltToOrb);
            Quaternion rotation = tilt * frameRotation * Quaternion.Euler(config.bottleLocalEuler);
            Vector3 axis = rotation * Vector3.up;
            bottlePosition = gripPosition + axis * config.bottleGripHeight;
            if (heldBottle != null) heldBottle.SetPositionAndRotation(gripPosition - axis * config.bottleGripHeight, rotation);
        }

        /// <summary>Both hands onto the sides of the bottle, elbows out and down.</summary>
        /// <summary>Each time the gauge goes up (a counted press or tap) Sam yanks back; the yank fades.</summary>
        private void UpdateYank()
        {
            yank = Mathf.Max(0f, yank - config.poseYankFade * Time.deltaTime * yank - 0.5f * Time.deltaTime);
            if (minigame == null || minigame.Gauge == null) return;
            float g = minigame.Gauge.Normalized;
            if (g > lastGauge + 0.0001f) yank = 1f;
            lastGauge = g;
        }

        private void ApplyArms()
        {
            float w = Mathf.SmoothStep(0f, 1f, poseWeight);
            Vector3 axis = heldBottle != null ? heldBottle.up : Vector3.up;
            Vector3 side = Vector3.ProjectOnPlane(frameRight, axis);
            side = side.sqrMagnitude > 0.0001f ? side.normalized : frameRight;
            // Elbows out and down.
            SolveLimb(leftUpperArm, leftLowerArm, leftHand, gripPosition - side * config.bottleGripHalfWidth,
                -frameRight * 0.7f + Vector3.down * 0.7f - frameForward * 0.2f, w);
            SolveLimb(rightUpperArm, rightLowerArm, rightHand, gripPosition + side * config.bottleGripHalfWidth,
                frameRight * 0.7f + Vector3.down * 0.7f - frameForward * 0.2f, w);
        }

        /// <summary>
        /// Two-bone IK: bends a limb so its end reaches <paramref name="goal"/> (blended by
        /// <paramref name="weight"/>), the middle joint bending toward <paramref name="poleHint"/>.
        /// </summary>
        private void SolveLimb(Transform upper, Transform lower, Transform hand, Vector3 goal, Vector3 poleHint, float weight)
        {
            if (upper == null || lower == null || hand == null || weight <= 0f) return;
            Vector3 a = upper.position;
            Vector3 b = lower.position;
            Vector3 c = hand.position;
            Vector3 target = Vector3.Lerp(c, goal, weight);
            float upperLength = (b - a).magnitude;
            float lowerLength = (c - b).magnitude;
            if (upperLength < 0.001f || lowerLength < 0.001f) return;

            Vector3 toTarget = target - a;
            float distance = Mathf.Clamp(toTarget.magnitude, 0.01f, (upperLength + lowerLength) * 0.999f);
            Vector3 dir = toTarget.sqrMagnitude > 0.000001f ? toTarget.normalized : frameForward;
            Vector3 pole = Vector3.ProjectOnPlane(poleHint, dir);
            pole = pole.sqrMagnitude > 0.0001f ? pole.normalized : Vector3.down;

            float cos = Mathf.Clamp((upperLength * upperLength + distance * distance - lowerLength * lowerLength)
                                    / (2f * upperLength * distance), -1f, 1f);
            float sin = Mathf.Sqrt(1f - cos * cos);
            Vector3 elbow = a + dir * upperLength * cos + pole * upperLength * sin;

            upper.rotation = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
            b = lower.position;
            c = hand.position;
            lower.rotation = Quaternion.FromToRotation(c - b, a + dir * distance - b) * lower.rotation;
        }

        private static void Rotate(Transform bone, Vector3 worldAxis, float degrees)
        {
            if (bone != null) bone.rotation = Quaternion.AngleAxis(degrees, worldAxis) * bone.rotation;
        }

        /// <summary>The bottle shakes with Sam, so the streak emitters follow its mouth.</summary>
        private void FollowBottleWithStreaks()
        {
            if (finished) return;
            if (surroundStreaks != null) surroundStreaks.transform.position = bottlePosition;
            if (orbStreaks != null)
            {
                orbStreaks.transform.position = bottlePosition;
                ParticleSystem.ShapeModule shape = orbStreaks.shape;
                shape.position = orbPosition - bottlePosition;
            }
        }

        /// <summary>
        /// Points one emitter at the bottle: particles start on a sphere of <paramref name="radius"/>
        /// around <paramref name="localCentre"/> (relative to the bottle) and are pulled to the bottle by
        /// a negative radial velocity, dying about when they arrive.
        /// </summary>
        private void SetupStreaks(ParticleSystem system, Vector3 localCentre, float radius, float rate, float lifetime, Color color)
        {
            if (system == null) return;
            system.transform.SetPositionAndRotation(bottlePosition, Quaternion.identity);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = lifetime * 0.95f;
            main.startSpeed = 0f;
            main.startSize = config.streakWidth;
            main.startColor = color;
            main.maxParticles = config.streakMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.01f, radius);
            shape.radiusThickness = 0f; // emit from the surface only
            shape.position = localCentre;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = new ParticleSystem.MinMaxCurve(-config.streakSpeed);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;

            system.Play(true);
        }

        private void StopStreaks(bool clear)
        {
            ParticleSystemStopBehavior how = clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
            if (surroundStreaks != null) surroundStreaks.Stop(true, how);
            if (orbStreaks != null) orbStreaks.Stop(true, how);
        }
    }
}
