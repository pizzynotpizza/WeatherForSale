using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Orbiting third-person camera. Desktop: hold the right mouse button and move the mouse
    /// (or use the gamepad right stick); the mouse wheel zooms. Touch: <see cref="AddTouchLook"/> and
    /// <see cref="AddPinchZoom"/> are fed by the on-screen look area. Values come from
    /// <see cref="ObbyLevelConfig"/>: the camera starts at <c>cameraDistance</c> and zooms between
    /// <c>cameraMinDistance</c> and <c>cameraMaxDistance</c>. Zooming closer than
    /// <c>firstPersonEnterDistance</c> snaps to first person: the camera sits at eye height and the
    /// player model is hidden (its shadow stays).
    /// Implements: story-001 criterion 1 (third person camera).
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("Tunable camera values.")]
        private ObbyLevelConfig config;

        [SerializeField, Tooltip("The transform to follow (the player).")]
        private Transform target;

        private float yaw;
        private float pitch;
        private Vector3 smoothedPivot;
        private float targetDistance;
        private float currentDistance;
        private bool distanceSet;
        private bool initialised;
        private Renderer[] playerRenderers = System.Array.Empty<Renderer>();
        private bool modelHidden;

        /// <summary>True while the camera is in first person (zoomed all the way in).</summary>
        public bool IsFirstPerson => distanceSet && targetDistance <= 0f;

        /// <summary>Current camera-to-pivot distance in metres (eases toward the zoom target).</summary>
        public float Distance => currentDistance;

        /// <summary>Current horizontal camera angle in degrees.</summary>
        public float Yaw => yaw;

        /// <summary>Instantly turns the camera to look along the given yaw (degrees), e.g. after a respawn.</summary>
        public void SetYaw(float yawDegrees)
        {
            yaw = yawDegrees;
        }

        // Assisted follow (glide): the camera eases toward a fixed heading, pitch and distance.
        private bool autoFollow;
        private float autoFollowYaw;
        private float autoFollowPitch;
        private float autoFollowSharpness;
        private float savedTargetDistance;

        /// <summary>True while an assisted follow (<see cref="BeginAutoFollow"/>) is active.</summary>
        public bool IsAutoFollowing => autoFollow;

        /// <summary>
        /// Starts an assisted follow (used while gliding): the camera eases its yaw and pitch toward the given
        /// angles (degrees) at <paramref name="sharpness"/> per second and moves out to <paramref name="distance"/>
        /// metres. Mouse / stick look still works and eases back. <see cref="EndAutoFollow"/> restores the zoom.
        /// </summary>
        public void BeginAutoFollow(float yawDegrees, float pitchDegrees, float distance, float sharpness)
        {
            if (config == null) return;
            EnsureDistance();
            if (!autoFollow) savedTargetDistance = targetDistance;
            autoFollow = true;
            autoFollowYaw = yawDegrees;
            autoFollowPitch = pitchDegrees;
            autoFollowSharpness = Mathf.Max(0.01f, sharpness);
            targetDistance = Mathf.Max(0f, distance);
        }

        /// <summary>Ends the assisted follow and returns to the zoom used before it. Does nothing if none is active.</summary>
        public void EndAutoFollow()
        {
            if (!autoFollow) return;
            autoFollow = false;
            targetDistance = savedTargetDistance;
        }

        /// <summary>Adds a touch drag, in screen pixels, to the camera angles.</summary>
        public void AddTouchLook(Vector2 deltaPixels)
        {
            if (config == null) return;
            ApplyLook(deltaPixels * config.touchLookSensitivity);
        }

        /// <summary>Zooms by the given metres: positive moves the camera out, negative moves it in.</summary>
        public void AddZoom(float metres)
        {
            if (config == null) return;
            EnsureDistance();
            float min = Mathf.Max(0f, Mathf.Min(config.cameraMinDistance, config.cameraMaxDistance));
            float max = Mathf.Max(config.cameraMinDistance, config.cameraMaxDistance);
            float next = Mathf.Clamp(targetDistance + metres, min, max);
            // Below the first-person threshold there is no useful third-person view: snap in or out.
            if (next < config.firstPersonEnterDistance) next = metres < 0f ? 0f : Mathf.Min(config.firstPersonEnterDistance, max);
            targetDistance = next;
        }

        private void EnsureDistance()
        {
            if (distanceSet) return;
            distanceSet = true;
            targetDistance = config.cameraDistance;
        }

        /// <summary>
        /// Adds a two-finger pinch: <paramref name="spreadPixels"/> is how much the gap between the
        /// fingers grew this frame (negative when they close). Spreading zooms in, closing zooms out.
        /// </summary>
        public void AddPinchZoom(float spreadPixels)
        {
            if (config == null || Screen.height <= 0) return;
            AddZoom(-spreadPixels / Screen.height * config.pinchZoomPerScreen);
        }

        private void ApplyLook(Vector2 deltaDegrees)
        {
            yaw += deltaDegrees.x;
            float minPitch = IsFirstPerson ? config.firstPersonMinPitch : config.cameraMinPitch;
            pitch = Mathf.Clamp(pitch - deltaDegrees.y, minPitch, config.cameraMaxPitch);
        }

        private void LateUpdate()
        {
            if (config == null || target == null) return;

            if (!initialised)
            {
                initialised = true;
                yaw = target.eulerAngles.y;
                pitch = config.cameraStartPitch;
                smoothedPivot = target.position + Vector3.up * config.cameraPivotHeight;
                EnsureDistance();
                currentDistance = targetDistance;
                playerRenderers = target.GetComponentsInChildren<Renderer>(true);
            }

            float dt = Time.deltaTime;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                ApplyLook(mouse.delta.ReadValue() * config.mouseLookSensitivity);
            }

            // Wheel values per notch differ by platform and browser, so count notches by sign only.
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f) AddZoom(-Mathf.Sign(scroll) * config.scrollZoomStep);
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                ApplyLook(pad.rightStick.ReadValue() * (config.gamepadLookSpeed * dt));
            }

            if (autoFollow)
            {
                float ease = 1f - Mathf.Exp(-autoFollowSharpness * dt);
                yaw = Mathf.LerpAngle(yaw, autoFollowYaw, ease);
                pitch = Mathf.Lerp(pitch, autoFollowPitch, ease);
            }

            // Follow horizontally with no lag: a lagging pivot changes the camera-to-player distance
            // while running (6 m/s × lag ≈ 0.4 m at a 4 m distance), which reads as constant zooming.
            // Only the height is smoothed, so jumps and landings do not jolt the view.
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-config.zoomSharpness * dt));
            if (Mathf.Abs(currentDistance - targetDistance) < 0.001f) currentDistance = targetDistance;

            // The pivot rises from shoulder to eye height over the last stretch of zoom.
            float eyeBlend = config.firstPersonEnterDistance > 0f
                ? 1f - Mathf.Clamp01(currentDistance / config.firstPersonEnterDistance)
                : (currentDistance <= 0f ? 1f : 0f);
            float pivotHeight = Mathf.Lerp(config.cameraPivotHeight, config.firstPersonEyeHeight, eyeBlend);
            SetModelHidden(currentDistance < config.firstPersonHideDistance);
            if (!IsFirstPerson && pitch < config.cameraMinPitch) pitch = config.cameraMinPitch;

            Vector3 desiredPivot = target.position + Vector3.up * pivotHeight;
            float t = 1f - Mathf.Exp(-config.cameraFollowSharpness * dt);
            smoothedPivot = new Vector3(desiredPivot.x, Mathf.Lerp(smoothedPivot.y, desiredPivot.y, t), desiredPivot.z);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(smoothedPivot - rotation * Vector3.forward * currentDistance, rotation);
        }

        private void OnDisable()
        {
            SetModelHidden(false);
        }

        private void SetModelHidden(bool hide)
        {
            if (hide == modelHidden) return;
            modelHidden = hide;
            foreach (Renderer r in playerRenderers)
            {
                if (r != null) r.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }
    }
}
