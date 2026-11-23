using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WeatherForSale.Suction
{
    /// <summary>Which prompt the suction minigame shows.</summary>
    public enum SuctionInputMode
    {
        /// <summary>Tap button when a touchscreen is present, otherwise the keyboard key. A touch during the key prompt switches to the tap button.</summary>
        Auto,
        /// <summary>Always the random keyboard key (PC).</summary>
        Keyboard,
        /// <summary>Always the tap button (mobile; also clickable with the mouse for testing on PC).</summary>
        Touch,
    }

    /// <summary>Per-map colours and star thresholds of the suction stage and Complete screen.</summary>
    [Serializable]
    public sealed class SuctionMapSettings
    {
        [Tooltip("MapCatalog id, e.g. \"rainforest\".")]
        public string mapId = "";

        [Tooltip("Colour of the translucent weather orb (alpha = see-through amount).")]
        public Color orbColor = new Color(0.35f, 0.80f, 0.75f, 0.55f);

        [Tooltip("Colour of the suction streaks flowing into the bottle.")]
        public Color streakColor = new Color(0.75f, 1.00f, 0.95f, 0.85f);

        [Tooltip("Total run time (both obby levels + suction), seconds, at or under which the player earns 3 stars.")]
        [Min(0f)] public float threeStarSeconds = 120f;

        [Tooltip("At or under this many seconds: 2 stars.")]
        [Min(0f)] public float twoStarSeconds = 180f;

        [Tooltip("At or under this many seconds: 1 star. Slower runs earn 0 stars.")]
        [Min(0f)] public float oneStarSeconds = 270f;
    }

    /// <summary>
    /// Designer-tunable values for the suction minigame (Quick Time Event) and its in-place stage at
    /// the end of each map's level 2: gauge amounts, decay, re-randomise step, PC key set, touch button
    /// size and safe margins, the camera / bottle / orb placement, suction streaks and per-map stars.
    /// Positions marked "spot-local" are in the frame of the SuctionSpot where Sam stands:
    /// +X = Sam's right, +Y = up, +Z = the way Sam faces.
    /// Implements: story-003 criteria 2-7 (design/game-brief.md Features 4, "Goal &amp; fail state": stars).
    /// </summary>
    [CreateAssetMenu(fileName = "SuctionConfig", menuName = "Weather For Sale/Suction/Suction Config")]
    public sealed class SuctionConfig : ScriptableObject
    {
        [Header("Gauge")]
        [Tooltip("Gauge top. Reaching it collects the weather.")]
        [Min(1f)] public float gaugeMax = 100f;

        [Tooltip("Gauge added by one correct key press or tap.")]
        [Min(0.01f)] public float amountPerPress = 3.5f;

        [Tooltip("Gauge lost per second while not full. 0 = no decay.")]
        [Min(0f)] public float decayPerSecond = 7f;

        [Tooltip("A new key is drawn and the prompt moves each time the gauge reaches another multiple of this (10 = every 10%).")]
        [Min(0f)] public float stepSize = 10f;

        [Tooltip("The key and its position (or the tap button's position) also change after this many seconds without a change. 0 = only on gauge steps.")]
        [Min(0f)] public float changeEverySeconds = 1.5f;

        [Header("PC keys")]
        [Tooltip("Keys the prompt is drawn from (physical key positions). Only the shown key adds gauge; the same key is never drawn twice in a row. W/A/S/D are also the movement keys, but movement is frozen during suction, so they do not clash.")]
        public Key[] keys = { Key.W, Key.A, Key.S, Key.D };

        [Header("Touch")]
        public SuctionInputMode inputMode = SuctionInputMode.Auto;

        [Tooltip("Tap button size in canvas units (reference 1920x1080).")]
        [Min(80f)] public float tapButtonSize = 240f;

        [Tooltip("Gap kept between the moving prompt (tap button or key) and the left/right screen edges, canvas units.")]
        [Min(0f)] public float safeMarginSides = 140f;

        [Tooltip("Gap kept below the top edge (keeps the button clear of the title and gauge at the top), canvas units.")]
        [Min(0f)] public float safeMarginTop = 360f;

        [Tooltip("Gap kept above the bottom edge, canvas units.")]
        [Min(0f)] public float safeMarginBottom = 140f;

        [Tooltip("A new tap position is at least this far from the old one when possible, canvas units.")]
        [Min(0f)] public float minJumpDistance = 300f;

        [Header("Screens")]
        [Tooltip("Seconds before the Continue / Next buttons accept input, so rapid tapping does not skip the screens.")]
        [Min(0f)] public float panelInputDelaySeconds = 0.8f;

        [Header("Stage camera (spot-local)")]
        [Tooltip("Camera position: in front of Sam, a little low and to his right.")]
        public Vector3 cameraLocalPosition = new Vector3(0.35f, 0.65f, 2.6f);

        [Tooltip("Point the camera looks at. Shifted to the camera's right of Sam so Sam sits left of centre and the orb fits on the right.")]
        public Vector3 cameraLookLocal = new Vector3(-0.7f, 0.9f, 0.3f);

        [Tooltip("Seconds the camera takes to swing from the follow view to the front view.")]
        [Min(0f)] public float cameraMoveSeconds = 0.8f;

        [Header("Bottle (held in both hands, pointed at the orb)")]
        [Tooltip("How far out from Sam's chest, toward the orb, the bottle is held, metres.")]
        [Min(0f)] public float bottleHoldDistance = 0.42f;

        [Tooltip("How far below the chest bone the bottle is held, metres.")]
        public float bottleHoldDrop = 0.04f;

        [Tooltip("0 = bottle upright, 1 = its mouth points straight at the orb.")]
        [Range(0f, 1f)] public float bottleTiltToOrb = 0.6f;

        [Tooltip("Where along the bottle (from its pivot, along its axis) the hands grip, metres.")]
        public float bottleGripHeight = 0.1f;

        [Tooltip("Half the gap between the two hands on the bottle, metres.")]
        [Min(0f)] public float bottleGripHalfWidth = 0.09f;

        [Tooltip("Extra bottle rotation after the tilt, degrees.")]
        public Vector3 bottleLocalEuler = Vector3.zero;

        [Tooltip("Bottle scale (the prefab is real size, about 0.2 m).")]
        [Min(0.01f)] public float bottleScale = 1.7f;

        [Header("Sam's pulling pose (added on top of his idle animation)")]
        [Tooltip("Upper body leans back, away from the orb, degrees.")]
        public float poseLeanBackDegrees = 12f;

        [Tooltip("Upper body leans sideways away from the orb, degrees.")]
        public float poseLeanSideDegrees = 10f;

        [Tooltip("Each leg swings outward this much (wide stance), degrees.")]
        public float poseLegSpreadDegrees = 16f;

        [Tooltip("Front leg forward, back leg back (braced stance), degrees.")]
        public float poseLegStrideDegrees = 18f;

        [Tooltip("Strain shake of the upper body, degrees.")]
        [Min(0f)] public float poseShakeDegrees = 1.6f;

        [Tooltip("Strain shake speed, cycles per second.")]
        [Min(0f)] public float poseShakeHz = 9f;

        [Tooltip("Tug-of-war heave: extra lean back and forth, degrees either way.")]
        [Min(0f)] public float poseHeaveDegrees = 16f;

        [Tooltip("Heaves per second.")]
        [Min(0f)] public float poseHeaveHz = 1.1f;

        [Tooltip("Extra lean back on each counted press or tap (a yank), degrees; it fades out.")]
        [Min(0f)] public float poseYankDegrees = 10f;

        [Tooltip("How fast a yank fades, per second (higher = snappier).")]
        [Min(0.1f)] public float poseYankFade = 7f;

        [Tooltip("The bottle is pulled toward Sam by this much at full lean, metres.")]
        [Min(0f)] public float poseBottlePull = 0.08f;

        [Tooltip("Share of the lean done by rocking the whole body about the ankles (feet stay planted); the rest bends the spine. 0..1.")]
        [Range(0f, 1f)] public float poseBodyShare = 0.65f;

        [Tooltip("Seconds to blend from the idle pose into the pull.")]
        [Min(0.01f)] public float poseBlendSeconds = 0.35f;

        [Header("Weather orb (spot-local)")]
        [Tooltip("Orb centre: in front of Sam and to the camera's right.")]
        public Vector3 orbLocalPosition = new Vector3(-1.1f, 1.1f, 0.9f);

        [Tooltip("Orb diameter at an empty gauge, metres.")]
        [Min(0f)] public float orbStartDiameter = 1.3f;

        [Tooltip("Orb diameter at a full gauge, metres (it is hidden once full).")]
        [Min(0f)] public float orbEndDiameter = 0.1f;

        [Header("Suction streaks")]
        [Tooltip("Radius around the bottle that the surrounding streaks start from, metres.")]
        [Min(0.1f)] public float streakSurroundRadius = 2.4f;

        [Tooltip("Surrounding streaks per second.")]
        [Min(0f)] public float streakSurroundRate = 22f;

        [Tooltip("Streaks per second leaving the orb toward the bottle.")]
        [Min(0f)] public float streakOrbRate = 28f;

        [Tooltip("Speed streaks fly toward the bottle, metres per second.")]
        [Min(0.1f)] public float streakSpeed = 4.5f;

        [Tooltip("Streak width, metres.")]
        [Min(0.001f)] public float streakWidth = 0.03f;

        [Tooltip("Most streaks alive at once per emitter (keep low for WebGL on mobile).")]
        [Min(1)] public int streakMaxParticles = 60;

        [Header("Maps")]
        [Tooltip("Used for a map missing from the list below.")]
        public SuctionMapSettings defaultMap = new SuctionMapSettings();

        public SuctionMapSettings[] maps = Array.Empty<SuctionMapSettings>();

        /// <summary>Settings for <paramref name="mapId"/>, or <see cref="defaultMap"/> when it has none.</summary>
        public SuctionMapSettings ForMap(string mapId)
        {
            if (maps != null && !string.IsNullOrEmpty(mapId))
            {
                foreach (SuctionMapSettings settings in maps)
                {
                    if (settings != null && settings.mapId == mapId) return settings;
                }
            }
            return defaultMap ?? new SuctionMapSettings();
        }

        /// <summary>Stars (0-3) for a run of <paramref name="totalSeconds"/> on <paramref name="mapId"/>.</summary>
        public int StarsFor(string mapId, float totalSeconds)
        {
            return CountStars(ForMap(mapId), totalSeconds);
        }

        /// <summary>Stars (0-3) for <paramref name="totalSeconds"/> against <paramref name="settings"/>; faster is better.</summary>
        public static int CountStars(SuctionMapSettings settings, float totalSeconds)
        {
            if (settings == null) return 0;
            if (totalSeconds <= settings.threeStarSeconds) return 3;
            if (totalSeconds <= settings.twoStarSeconds) return 2;
            if (totalSeconds <= settings.oneStarSeconds) return 1;
            return 0;
        }
    }
}
