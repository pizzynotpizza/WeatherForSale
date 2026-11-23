using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Designer-tunable values for one obby level: player movement, level timer,
    /// kill height and third-person camera. Create one asset per level
    /// (Assets/Create/Weather For Sale/Obby Level Config).
    /// Implements: design/game-brief.md (Features 2, Challenge) and
    /// production/epics/weather-for-sale/story-001-obby-first-level.md.
    /// </summary>
    [CreateAssetMenu(fileName = "ObbyLevelConfig", menuName = "Weather For Sale/Obby Level Config")]
    public sealed class ObbyLevelConfig : ScriptableObject
    {
        [Header("Player movement")]
        [Tooltip("Top horizontal speed in metres per second.")]
        [Min(0f)] public float moveSpeed = 6f;

        [Tooltip("Horizontal acceleration on the ground, metres per second squared.")]
        [Min(0f)] public float groundAcceleration = 45f;

        [Tooltip("Horizontal acceleration in the air as a fraction of ground acceleration (0..1).")]
        [Range(0f, 1f)] public float airControl = 0.6f;

        [Tooltip("How fast the character turns toward the move direction, degrees per second.")]
        [Min(0f)] public float turnSpeed = 720f;

        [Tooltip("Upward speed at the moment of a jump, metres per second.")]
        [Min(0f)] public float jumpSpeed = 9f;

        [Tooltip("Downward acceleration, metres per second squared (positive number).")]
        [Min(0f)] public float gravity = 25f;

        [Tooltip("Terminal falling speed, metres per second (positive number).")]
        [Min(0f)] public float maxFallSpeed = 40f;

        [Tooltip("Seconds after leaving a ledge during which a jump is still allowed.")]
        [Min(0f)] public float coyoteTime = 0.1f;

        [Tooltip("Seconds a jump press is remembered before landing.")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Tooltip("Small downward speed applied while grounded so the controller stays glued to the floor.")]
        [Min(0f)] public float groundedStickSpeed = 2f;

        [Header("Level rules")]
        [Tooltip("Time limit for the level in seconds. The timer keeps running across respawns.")]
        [Min(1f)] public float levelTimeSeconds = 180f;

        [Tooltip("Remaining seconds at or below which the HUD timer is shown in the warning colour.")]
        [Min(0f)] public float lowTimeWarningSeconds = 30f;

        [Tooltip("World Y below which the player counts as fallen and respawns at the last checkpoint.")]
        public float killHeight = -15f;

        [Tooltip("Metres above a checkpoint's respawn point at which the player is placed.")]
        [Min(0f)] public float respawnHeightOffset = 0.25f;

        [Header("Camera")]
        [Tooltip("Distance from the pivot to the camera, metres.")]
        [Min(0.1f)] public float cameraDistance = 2.75f;

        [Tooltip("Closest the player can zoom in, metres. 0 = first person.")]
        [Min(0f)] public float cameraMinDistance = 0f;

        [Tooltip("Farthest the player can zoom out, metres.")]
        [Min(0.1f)] public float cameraMaxDistance = 4f;

        [Tooltip("Zooming closer than this (metres) snaps to first person.")]
        [Min(0f)] public float firstPersonEnterDistance = 0.75f;

        [Tooltip("The player model is hidden (shadow kept) when the camera is closer than this, metres.")]
        [Min(0f)] public float firstPersonHideDistance = 0.5f;

        [Tooltip("Camera height above the player's feet in first person, metres (eye level).")]
        public float firstPersonEyeHeight = 1.25f;

        [Tooltip("Lowest pitch in first person, degrees (negative looks up).")]
        public float firstPersonMinPitch = -60f;

        [Tooltip("Metres zoomed per mouse-wheel notch.")]
        [Min(0f)] public float scrollZoomStep = 0.5f;

        [Tooltip("Metres zoomed when two fingers spread apart by one full screen height.")]
        [Min(0f)] public float pinchZoomPerScreen = 8f;

        [Tooltip("Higher = the camera reaches a new zoom distance faster.")]
        [Min(0f)] public float zoomSharpness = 12f;

        [Tooltip("Height of the camera pivot above the player's feet, metres.")]
        public float cameraPivotHeight = 1.1f;

        [Tooltip("Pitch the camera starts at, degrees (positive looks down).")]
        public float cameraStartPitch = 18f;

        [Tooltip("Lowest pitch, degrees (negative looks up).")]
        public float cameraMinPitch = -10f;

        [Tooltip("Highest pitch, degrees.")]
        public float cameraMaxPitch = 65f;

        [Tooltip("Mouse look speed in degrees per pixel (hold right mouse button).")]
        [Min(0f)] public float mouseLookSensitivity = 0.15f;

        [Tooltip("Touch look speed in degrees per screen pixel dragged.")]
        [Min(0f)] public float touchLookSensitivity = 0.2f;

        [Tooltip("Gamepad right-stick look speed in degrees per second.")]
        [Min(0f)] public float gamepadLookSpeed = 140f;

        [Tooltip("Higher = the camera pivot catches up with the player faster.")]
        [Min(0f)] public float cameraFollowSharpness = 14f;
    }
}
