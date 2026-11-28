using System.Collections.Generic;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A hazard that rolls along a straight path again and again (tumbleweed, snowballs): it starts at
    /// its home position, travels <c>travel</c> at <c>speed</c>, disappears, waits, and starts again. Put a
    /// trigger collider and a <see cref="KillVolume"/> on the same object so touching it respawns the
    /// player. The visual child rolls to match the distance travelled. Colliders and renderers are
    /// switched off while it waits. Driven by absolute time: every run has the same rhythm.
    /// Optional: with <c>stopAtBlockers</c> the run is cut short where a sphere swept along the path first
    /// touches solid scenery (a cover boulder), so it never passes through it; and a separate
    /// <c>burstVisual</c> (a snow puff) pops where the run ends (and where it starts). With
    /// <c>deflectAtBlockers</c> as well, a blocked hazard does not end there: it rolls sideways along the
    /// blocker's face until clear (staying within <c>deflectLimit</c>) and carries on, so the path becomes a
    /// zigzag around every blocker in the way.
    /// Implements: story-004, Desert 1 and 2 (tumbleweeds rolling toward the player); Glacier 1 and 2
    /// (snowballs that stop at the cover boulders' uphill faces).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RollingHazard : MonoBehaviour
    {
        [SerializeField, Tooltip("Path from the home position, in the parent's space, metres. The hazard rolls along it.")]
        private Vector3 travel = new Vector3(0f, 0f, -15f);

        [SerializeField, Min(0.1f), Tooltip("Rolling speed, metres per second.")]
        private float speed = 5f;

        [SerializeField, Min(0f), Tooltip("Seconds it stays away after reaching the end, before it starts again.")]
        private float restartDelaySeconds = 2f;

        [SerializeField, Tooltip("Time offset of the cycle, seconds (to space several hazards on one path).")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Optional bent path: points relative to the home position, in the parent's space, rolled through in order (replaces travel; e.g. down a chute, across a pad, off its edge).")]
        private Vector3[] waypoints;

        [SerializeField, Tooltip("Visual that rolls (optional).")]
        private Transform spinVisual;

        [SerializeField, Tooltip("Local axis the visual rolls around: for travel toward -Z use (-1,0,0); toward +Z use (1,0,0).")]
        private Vector3 spinAxis = new Vector3(-1f, 0f, 0f);

        [SerializeField, Min(0.05f), Tooltip("Radius of the rolling visual, metres (sets how fast it spins).")]
        private float rollRadius = 0.6f;

        [Header("Blockers (optional)")]
        [SerializeField, Tooltip("When on, the run ends where a sphere swept along the path first touches a solid (non-trigger) collider that is not the player, e.g. a cover boulder. Computed once at start.")]
        private bool stopAtBlockers;

        [SerializeField, Min(0.05f), Tooltip("Radius of the swept sphere, metres. A little under the visual radius, so the sweep does not graze the ground the hazard rolls on.")]
        private float blockerProbeRadius = 0.5f;

        [SerializeField, Min(0f), Tooltip("The run ends this far (metres, along the path) before the touch point, so the visual never overlaps the blocker.")]
        private float blockerGap = 0.15f;

        [SerializeField, Tooltip("With stopAtBlockers: instead of ending at a blocker, roll sideways round it (along deflectAxis) and carry on.")]
        private bool deflectAtBlockers;

        [SerializeField, Tooltip("Sideways direction for deflecting, in the parent's space (across the slope).")]
        private Vector3 deflectAxis = Vector3.right;

        [SerializeField, Tooltip("Preferred deflect side along deflectAxis: +1 or -1 (the other side is used when this one is out of bounds or blocked).")]
        private float deflectPreferSign = 1f;

        [SerializeField, Min(0f), Tooltip("Deflecting never takes the hazard's centre further than this from the parent origin along deflectAxis, metres (keeps it on the slope).")]
        private float deflectLimit = 5f;

        [SerializeField, Min(0f), Tooltip("The veer round a blocker starts this many times its sideways shift uphill of the blocker (bigger = gentler).")]
        private float deflectBendRatio = 1.8f;

        [SerializeField, Min(0f), Tooltip("Corners of the deflected path are rounded off over up to this distance each side, metres.")]
        private float deflectCornerRadius = 1.2f;

        [SerializeField, Min(0), Tooltip("Most blockers one run can deflect around.")]
        private int maxDeflections = 6;

        [SerializeField, Tooltip("Normal of the ground it rolls on, in the parent's space (sets the roll axis on each leg of a deflected path).")]
        private Vector3 groundNormal = Vector3.up;

        [Header("Burst (optional)")]
        [SerializeField, Tooltip("A puff shown briefly where the run ends. Must NOT be a child of this object (it stays put while this one resets).")]
        private Transform burstVisual;

        [SerializeField, Min(0.05f), Tooltip("Seconds the puff stays visible, growing.")]
        private float burstSeconds = 0.35f;

        [SerializeField, Tooltip("Also show the puff where each run starts (the hazard appears out of it).")]
        private bool burstAtStart;

        private Vector3 homeLocalPosition;
        private Quaternion baseSpinRotation = Quaternion.identity;
        private Collider[] colliders;
        private Renderer[] renderers;
        private Renderer[] burstRenderers;
        private Vector3 burstBaseScale = Vector3.one;
        private bool burstShown = true;
        private bool active = true;
        private float runLength;
        // Path legs in the parent's space: points and the cumulative distance at each point.
        private const int CornerSamples = 6;
        // Extra width when checking a veer for other blockers (room for the rounded corners).
        private const float BendSweepMargin = 0.2f;
        private Vector3[] pathPoints;
        private float[] pathDistances;
        private Quaternion spinRotation = Quaternion.identity;
        private float lastDistance;

        /// <summary>Length of one run in the parent's space, metres (shorter than <c>travel</c> when a blocker cuts it).</summary>
        public float RunLength => runLength;

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
            if (spinVisual != null) baseSpinRotation = spinVisual.localRotation;
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            runLength = travel.magnitude;

            if (burstVisual != null)
            {
                burstRenderers = burstVisual.GetComponentsInChildren<Renderer>(true);
                burstBaseScale = burstVisual.localScale;
                SetBurstShown(false);
            }
        }

        private void Start()
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                var points = new List<Vector3> { homeLocalPosition };
                foreach (Vector3 w in waypoints) points.Add(homeLocalPosition + w);
                SetPath(points);
            }
            else if (stopAtBlockers && deflectAtBlockers)
            {
                BuildDeflectedPath();
            }
            else
            {
                if (stopAtBlockers) runLength = MeasureRunToBlocker();
                SetPath(new List<Vector3> { homeLocalPosition, homeLocalPosition + travel.normalized * runLength });
            }
        }

        private void SetPath(List<Vector3> points)
        {
            pathPoints = points.ToArray();
            pathDistances = new float[pathPoints.Length];
            for (int i = 1; i < pathPoints.Length; i++)
            {
                pathDistances[i] = pathDistances[i - 1] + Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            }
            runLength = pathDistances[pathDistances.Length - 1];
        }

        /// <summary>
        /// Builds the zigzag path: down <c>travel</c> until a blocker, sideways along its face until clear, then down
        /// again for what is left of the travel; repeated for each blocker. A hazard that cannot get round (both sides
        /// out of bounds or blocked) ends at the blocker.
        /// </summary>
        private void BuildDeflectedPath()
        {
            var points = new List<Vector3> { homeLocalPosition };
            Vector3 down = travel.normalized;
            float remaining = travel.magnitude;
            Vector3 p = homeLocalPosition;
            Physics.SyncTransforms();
            for (int i = 0; remaining > 0.01f; i++)
            {
                if (!Sweep(p, down, remaining, out float hitDistance, out Collider blocker))
                {
                    p += down * remaining;
                    points.Add(p);
                    break;
                }
                float advance = Mathf.Max(0f, hitDistance - blockerGap);
                Vector3 face = p + down * advance;
                remaining -= advance;
                if (i >= maxDeflections || !TryDeflect(face, blocker, out Vector3 shifted))
                {
                    points.Add(face);
                    break;
                }
                // Veer off gradually as it nears the blocker (like being pushed aside), not a sharp sideways step:
                // the bend starts deflectBendRatio x the sideways shift uphill of the face.
                // A bend that would clip another blocker on the way is shortened (down to a straight sideways step).
                float bend = Mathf.Min(advance, Vector3.Distance(face, shifted) * deflectBendRatio);
                for (int tries = 0; tries < 4 && bend > 0.01f; tries++)
                {
                    Vector3 bendStart = face - down * bend;
                    Vector3 diagonal = shifted - bendStart;
                    if (!SweepBlocked(bendStart, diagonal.normalized, diagonal.magnitude, blockerProbeRadius + BendSweepMargin)) break;
                    bend = tries < 2 ? bend * 0.5f : 0f;
                }
                if (bend > 0.01f && advance - bend > 0.01f) points.Add(face - down * bend);
                else points.Add(face);
                p = shifted;
                points.Add(p);
            }
            SetPath(RoundCorners(points));
        }

        /// <summary>True when a sphere of <paramref name="radius"/> swept from a point (parent space) hits a solid collider that is not this hazard, the player or the ground it starts on.</summary>
        private bool SweepBlocked(Vector3 fromLocal, Vector3 dirLocal, float lengthLocal, float radius)
        {
            Vector3 origin = ToWorld(fromLocal);
            Vector3 worldTravel = transform.parent != null ? transform.parent.TransformVector(dirLocal * lengthLocal) : dirLocal * lengthLocal;
            float worldLength = worldTravel.magnitude;
            if (worldLength < 0.0001f) return false;
            // The ground under the start is not in the way; anything else already touching the sphere is.
            Collider ground = Physics.Raycast(origin, Vector3.down, out RaycastHit below, radius * 3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? below.collider : null;
            foreach (RaycastHit hit in Physics.SphereCastAll(origin, radius, worldTravel / worldLength, worldLength,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Collider other = hit.collider;
                if (other == null || other == ground) continue;
                if (other.transform.IsChildOf(transform)) continue;
                if (other is CharacterController || other.GetComponentInParent<PlayerController>() != null) continue;
                return true;
            }
            return false;
        }

        /// <summary>Replaces each inner corner of a polyline with a short curve (quadratic, up to deflectCornerRadius each side).</summary>
        private List<Vector3> RoundCorners(List<Vector3> points)
        {
            if (points.Count < 3 || deflectCornerRadius <= 0f) return points;
            var rounded = new List<Vector3> { points[0] };
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 inLeg = points[i] - points[i - 1];
                Vector3 outLeg = points[i + 1] - points[i];
                float r = Mathf.Min(deflectCornerRadius, 0.45f * Mathf.Min(inLeg.magnitude, outLeg.magnitude));
                if (r < 0.01f)
                {
                    rounded.Add(points[i]);
                    continue;
                }
                Vector3 a = points[i] - inLeg.normalized * r;
                Vector3 b = points[i] + outLeg.normalized * r;
                for (int k = 0; k <= CornerSamples; k++)
                {
                    float t = k / (float)CornerSamples;
                    rounded.Add((1f - t) * (1f - t) * a + 2f * (1f - t) * t * points[i] + t * t * b);
                }
            }
            rounded.Add(points[points.Count - 1]);
            return rounded;
        }

        /// <summary>Finds the clear sideways spot beside <paramref name="blocker"/>, preferred side first.</summary>
        private bool TryDeflect(Vector3 from, Collider blocker, out Vector3 shifted)
        {
            Vector3 side = deflectAxis.sqrMagnitude > 0.0001f ? deflectAxis.normalized : Vector3.right;
            Transform parent = transform.parent;
            Bounds b = blocker.bounds;
            Vector3 sideWorld = parent != null ? parent.TransformDirection(side) : side;
            float centre = Vector3.Dot(b.center, sideWorld);
            float half = Mathf.Abs(b.extents.x * sideWorld.x) + Mathf.Abs(b.extents.y * sideWorld.y) + Mathf.Abs(b.extents.z * sideWorld.z);
            float here = Vector3.Dot(ToWorld(from), sideWorld);
            // Extra room so the rounded corners never graze the blocker.
            float clearance = blockerProbeRadius + blockerGap + 0.35f;

            float first = deflectPreferSign >= 0f ? 1f : -1f;
            foreach (float sign in new[] { first, -first })
            {
                float shift = sign > 0f ? (centre + half + clearance) - here : here - (centre - half - clearance);
                if (shift <= 0f) shift = 0.01f;
                Vector3 candidate = from + side * (sign * shift);
                if (Mathf.Abs(Vector3.Dot(candidate, side)) > deflectLimit) continue;
                if (Sweep(from, side * sign, shift, out _, out _)) continue;
                shifted = candidate;
                return true;
            }
            shifted = from;
            return false;
        }

        /// <summary>Sphere sweep in the parent's space; true when a solid collider (not this hazard or the player) is in the way.</summary>
        private bool Sweep(Vector3 fromLocal, Vector3 dirLocal, float lengthLocal, out float hitDistanceLocal, out Collider blocker)
        {
            hitDistanceLocal = lengthLocal;
            blocker = null;
            Vector3 origin = ToWorld(fromLocal);
            Vector3 worldTravel = transform.parent != null ? transform.parent.TransformVector(dirLocal * lengthLocal) : dirLocal * lengthLocal;
            float worldLength = worldTravel.magnitude;
            if (worldLength < 0.0001f) return false;
            float toLocal = lengthLocal / worldLength;

            RaycastHit[] hits = Physics.SphereCastAll(origin, blockerProbeRadius, worldTravel / worldLength, worldLength,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                Collider other = hit.collider;
                if (other == null || hit.distance <= 0f) continue;
                if (other.transform.IsChildOf(transform)) continue;
                if (other is CharacterController || other.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    blocker = other;
                }
            }
            if (blocker == null) return false;
            hitDistanceLocal = nearest * toLocal;
            return true;
        }

        /// <summary>Position along the path (parent space) and the direction of the leg it is on.</summary>
        private Vector3 PointAt(float distance, out Vector3 legDirection)
        {
            for (int i = 1; i < pathPoints.Length; i++)
            {
                if (distance <= pathDistances[i] || i == pathPoints.Length - 1)
                {
                    float legLength = pathDistances[i] - pathDistances[i - 1];
                    legDirection = legLength > 0.0001f ? (pathPoints[i] - pathPoints[i - 1]) / legLength : travel.normalized;
                    float f = legLength > 0.0001f ? Mathf.Clamp01((distance - pathDistances[i - 1]) / legLength) : 1f;
                    return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], f);
                }
            }
            legDirection = travel.normalized;
            return pathPoints[0];
        }

        /// <summary>
        /// Sweeps a sphere from home along the path and returns the run length (parent space) up to the first
        /// solid collider that is not this hazard or the player, minus <c>blockerGap</c>.
        /// </summary>
        private float MeasureRunToBlocker()
        {
            float localLength = travel.magnitude;
            if (localLength < 0.0001f) return 0f;

            Physics.SyncTransforms();
            Vector3 origin = ToWorld(homeLocalPosition);
            Vector3 worldTravel = transform.parent != null ? transform.parent.TransformVector(travel) : travel;
            float worldLength = worldTravel.magnitude;
            if (worldLength < 0.0001f) return localLength;

            RaycastHit[] hits = Physics.SphereCastAll(origin, blockerProbeRadius, worldTravel / worldLength, worldLength,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = worldLength;
            foreach (RaycastHit hit in hits)
            {
                Collider other = hit.collider;
                if (other == null || hit.distance <= 0f) continue; // overlapping at the start: not in the way
                if (other.transform.IsChildOf(transform)) continue;
                if (other is CharacterController || other.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.distance < nearest) nearest = hit.distance;
            }

            float worldRun = Mathf.Clamp(nearest - blockerGap, 0f, worldLength);
            return worldRun * (localLength / worldLength);
        }

        private void Update()
        {
            float runSeconds = runLength / speed;
            float cycle = runSeconds + restartDelaySeconds;
            if (cycle <= 0.0001f) return;
            float t = Mathf.Repeat(Time.time + phaseSeconds, cycle);
            bool running = t < runSeconds;

            if (running != active)
            {
                SetActive(running);
                spinRotation = Quaternion.identity;
                lastDistance = 0f;
            }
            UpdateBurst(t, runSeconds);
            if (!running) return;

            float distance = t * speed;
            transform.localPosition = PointAt(distance, out Vector3 leg);
            if (spinVisual != null)
            {
                if (!deflectAtBlockers && (waypoints == null || waypoints.Length == 0))
                {
                    spinVisual.localRotation = baseSpinRotation * Quaternion.AngleAxis(distance / rollRadius * Mathf.Rad2Deg, spinAxis.normalized);
                }
                else
                {
                    // Bent paths roll about (ground normal x leg), in this object's space, step by step.
                    Vector3 axis = Vector3.Cross(groundNormal.normalized, leg);
                    if (axis.sqrMagnitude < 0.0001f) axis = spinAxis;
                    float step = Mathf.Max(0f, distance - lastDistance);
                    lastDistance = distance;
                    spinRotation = Quaternion.AngleAxis(step / rollRadius * Mathf.Rad2Deg, axis.normalized) * spinRotation;
                    spinVisual.localRotation = spinRotation * baseSpinRotation;
                }
            }
        }

        /// <summary>Shows the puff (growing) just after the run ends, and optionally just after it starts.</summary>
        private void UpdateBurst(float t, float runSeconds)
        {
            if (burstVisual == null) return;
            float sinceEnd = t - runSeconds;
            bool atEnd = sinceEnd >= 0f && sinceEnd < burstSeconds;
            bool atStart = burstAtStart && !atEnd && t < burstSeconds;
            bool show = atEnd || atStart;
            if (show != burstShown) SetBurstShown(show);
            if (!show) return;

            float f = Mathf.Clamp01((atEnd ? sinceEnd : t) / burstSeconds);
            Vector3 local = atEnd ? pathPoints[pathPoints.Length - 1] : homeLocalPosition;
            burstVisual.position = ToWorld(local);
            burstVisual.localScale = burstBaseScale * Mathf.Lerp(0.5f, 1.3f, f);
        }

        private Vector3 ToWorld(Vector3 parentLocal)
        {
            return transform.parent != null ? transform.parent.TransformPoint(parentLocal) : parentLocal;
        }

        private void SetBurstShown(bool on)
        {
            burstShown = on;
            if (burstRenderers == null) return;
            foreach (Renderer r in burstRenderers)
            {
                if (r != null) r.enabled = on;
            }
        }

        private void SetActive(bool on)
        {
            active = on;
            foreach (Collider c in colliders)
            {
                if (c != null) c.enabled = on;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = on;
            }
            if (!on) transform.localPosition = homeLocalPosition;
        }
    }
}
