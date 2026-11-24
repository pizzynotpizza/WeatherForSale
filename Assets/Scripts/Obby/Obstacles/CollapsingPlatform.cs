using System.Collections.Generic;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A platform that gives way after the player stands on it: shakes during a warning, then sinks /
    /// falls away (collider off once it has dropped a little), stays gone for a while and rises back.
    /// Two modes: stepping on it starts a fixed countdown (collapsing mushroom), or the countdown only
    /// runs while the player keeps standing on it (rotten branch, see <c>requireContinuousStand</c>).
    /// Needs a <see cref="RideableSurface"/> on the same GameObject; put the visual meshes under
    /// <c>visual</c> so only they shake.
    /// Implements: story-004, Rainforest 1 (mushrooms that collapse 1 s after being stepped on) and
    /// Rainforest 2 (rotten branches that break when stood on too long); reused by Desert / Glacier.
    /// Transitions: Solid -> Warning -> Falling -> Gone -> Returning -> Solid.
    /// </summary>
    [RequireComponent(typeof(RideableSurface))]
    [DefaultExecutionOrder(-50)]
    public sealed class CollapsingPlatform : MonoBehaviour
    {
        private enum Phase { Solid, Warning, Falling, Gone, Returning }

        [Header("Trigger")]
        [SerializeField, Min(0.05f), Tooltip("Seconds between the first step and the platform giving way.")]
        private float standSeconds = 1f;

        [SerializeField, Tooltip("False: the countdown starts at the first step and runs even if the player leaves. True: it only runs while the player stands on it and drains when they step off.")]
        private bool requireContinuousStand;

        [SerializeField, Min(0f), Tooltip("When continuous: how many times faster than real time the countdown drains while nobody stands on it.")]
        private float drainRate = 2f;

        [Header("Warning")]
        [SerializeField, Tooltip("Visual shaken during the warning (and tilted while falling). Optional.")]
        private Transform visual;

        [SerializeField, Min(0f), Tooltip("Peak shake offset of the visual, metres.")]
        private float shakeAmplitude = 0.06f;

        [SerializeField, Min(0f), Tooltip("Shake speed, radians per second.")]
        private float shakeFrequency = 45f;

        [Header("Fall")]
        [SerializeField, Min(0.05f), Tooltip("Seconds the fall takes (accelerating).")]
        private float fallSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("How far the platform drops, metres. Make it deep enough to vanish under the water.")]
        private float fallDistance = 6f;

        [SerializeField, Min(0f), Tooltip("The collider switches off once the platform has dropped this far, metres, so a rider falls with it.")]
        private float colliderOffDepth = 0.4f;

        [SerializeField, Tooltip("Degrees the visual tilts around its local Z axis by the end of the fall (0 = none).")]
        private float tiltDegrees;

        [Header("Reset")]
        [SerializeField, Min(0f), Tooltip("Seconds the platform stays gone before it comes back.")]
        private float goneSeconds = 3f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the platform takes to rise back to its place.")]
        private float returnSeconds = 0.8f;

        private RideableSurface surface;
        private Collider[] solidColliders;
        private Renderer[] renderers;
        private Vector3 homeLocalPosition;
        private Vector3 visualHomePosition;
        private Quaternion visualHomeRotation = Quaternion.identity;
        private Phase phase = Phase.Solid;
        private float timer;
        private bool collidersOn = true;

        private void Awake()
        {
            surface = GetComponent<RideableSurface>();
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
            if (visual != null)
            {
                visualHomePosition = visual.localPosition;
                visualHomeRotation = visual.localRotation;
            }

            var colliders = new List<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                if (!c.isTrigger) colliders.Add(c);
            }
            solidColliders = colliders.ToArray();
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (phase)
            {
                case Phase.Solid:
                    if (surface.HasRider)
                    {
                        phase = Phase.Warning;
                        timer = 0f;
                    }
                    break;

                case Phase.Warning:
                    if (!requireContinuousStand || surface.HasRider) timer += dt;
                    else timer = Mathf.Max(0f, timer - dt * drainRate);

                    ApplyShake(Mathf.Clamp01(timer / standSeconds));
                    if (timer >= standSeconds)
                    {
                        phase = Phase.Falling;
                        timer = 0f;
                    }
                    else if (requireContinuousStand && timer <= 0f && !surface.HasRider)
                    {
                        ResetVisual();
                        phase = Phase.Solid;
                    }
                    break;

                case Phase.Falling:
                {
                    timer += dt;
                    float f = Mathf.Clamp01(timer / fallSeconds);
                    float depth = fallDistance * f * f;
                    transform.localPosition = homeLocalPosition + Vector3.down * depth;
                    if (visual != null) visual.localRotation = visualHomeRotation * Quaternion.Euler(0f, 0f, tiltDegrees * f);
                    if (collidersOn && depth >= colliderOffDepth) SetColliders(false);
                    if (f >= 1f)
                    {
                        if (collidersOn) SetColliders(false);
                        SetRenderers(false);
                        phase = Phase.Gone;
                        timer = 0f;
                    }
                    break;
                }

                case Phase.Gone:
                    timer += dt;
                    if (timer >= goneSeconds)
                    {
                        ResetVisual();
                        SetRenderers(true);
                        phase = Phase.Returning;
                        timer = 0f;
                    }
                    break;

                case Phase.Returning:
                {
                    timer += dt;
                    float f = Mathf.Clamp01(timer / returnSeconds);
                    float eased = f * f * (3f - 2f * f);
                    transform.localPosition = homeLocalPosition + Vector3.down * (fallDistance * (1f - eased));
                    if (f >= 1f)
                    {
                        transform.localPosition = homeLocalPosition;
                        SetColliders(true);
                        phase = Phase.Solid;
                    }
                    break;
                }
            }
        }

        private void ApplyShake(float strength)
        {
            if (visual == null) return;
            float t = Time.time * shakeFrequency;
            Vector3 offset = new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t * 1.3f)) * (shakeAmplitude * strength);
            visual.localPosition = visualHomePosition + offset;
        }

        private void ResetVisual()
        {
            if (visual == null) return;
            visual.localPosition = visualHomePosition;
            visual.localRotation = visualHomeRotation;
        }

        private void SetColliders(bool on)
        {
            collidersOn = on;
            foreach (Collider c in solidColliders)
            {
                if (c != null) c.enabled = on;
            }
        }

        private void SetRenderers(bool on)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = on;
            }
        }
    }
}
