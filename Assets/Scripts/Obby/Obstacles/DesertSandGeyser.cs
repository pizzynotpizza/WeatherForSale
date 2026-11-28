using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A sand geyser: a vent pad over the quicksand that erupts on a fixed rhythm (absolute time, so every run has
    /// the same timing). Each cycle is dormant (a plain pad), then a warning (the pad rumbles and a sand puff swells
    /// over the vent), then an eruption (a sand column shoots up and anyone standing on the pad is launched with
    /// <c>launchVelocity</c>). It is the only way up to a ledge too high to jump to, so the player reads the
    /// warning and stands on the vent in time. Needs a <see cref="RideableSurface"/> on the same object (the pad).
    /// Implements: story-004, Desert 1 (timed sand geysers that launch the player up to a higher ledge).
    /// Transitions: Dormant -> Warning -> Erupting -> Dormant.
    /// </summary>
    [RequireComponent(typeof(RideableSurface))]
    [DefaultExecutionOrder(-50)]
    public sealed class DesertSandGeyser : MonoBehaviour
    {
        /// <summary>Where a geyser is in its cycle.</summary>
        public enum GeyserPhase { Dormant, Warning, Erupting }

        [Header("Rhythm")]
        [SerializeField, Min(0.1f), Tooltip("Seconds of one full cycle (dormant + warning + eruption). Raised to warning + eruption if shorter.")]
        private float periodSeconds = 3f;

        [SerializeField, Min(0f), Tooltip("Seconds of rumbling before each eruption.")]
        private float warningSeconds = 0.9f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the eruption lasts; a rider standing on the pad at any moment of it is launched.")]
        private float eruptSeconds = 0.6f;

        [SerializeField, Tooltip("Time offset of the cycle, seconds (to stagger neighbouring geysers).")]
        private float phaseSeconds;

        [Header("Launch")]
        [SerializeField, Tooltip("Velocity given to the rider, world space, metres per second. Y sets the height: apex = y^2 / (2 * gravity).")]
        private Vector3 launchVelocity = new Vector3(0f, 14f, 0f);

        [SerializeField, Min(0f), Tooltip("Seconds after a launch during which the geyser does not launch again.")]
        private float launchCooldownSeconds = 0.3f;

        [Header("Visuals (optional)")]
        [SerializeField, Tooltip("Sand column, pivot at the vent; its Y scale grows from 0 to its built scale while erupting.")]
        private Transform column;

        [SerializeField, Min(0.01f), Tooltip("Seconds the column takes to shoot up (and to fall back at the end).")]
        private float columnRiseSeconds = 0.12f;

        [SerializeField, Tooltip("Sand puff over the vent, shown and swelling during the warning, full size while erupting.")]
        private Transform warningPuff;

        [SerializeField, Tooltip("Visual shaken during the warning and the eruption (the pad's body; not the collider).")]
        private Transform rumbleVisual;

        [SerializeField, Min(0f), Tooltip("Peak rumble offset of the visual, metres.")]
        private float rumbleAmplitude = 0.05f;

        [SerializeField, Min(0f), Tooltip("Rumble speed, radians per second.")]
        private float rumbleFrequency = 40f;

        private RideableSurface surface;
        private Renderer[] columnRenderers;
        private Renderer[] puffRenderers;
        private Vector3 columnBaseScale = Vector3.one;
        private Vector3 puffBaseScale = Vector3.one;
        private Vector3 rumbleHome;
        private bool columnShown = true;
        private bool puffShown = true;
        private float nextLaunchTime;

        /// <summary>The geyser's phase this frame.</summary>
        public GeyserPhase Phase { get; private set; }

        /// <summary>
        /// The phase of a geyser cycle at an absolute time, and how far through that phase it is (0 - 1).
        /// The cycle is dormant first, then the warning, then the eruption. Pure, so it can be tested without a scene.
        /// </summary>
        public static GeyserPhase PhaseAt(float time, float periodSeconds, float warningSeconds, float eruptSeconds, float phaseSeconds,
            out float progress)
        {
            float erupt = Mathf.Max(0.01f, eruptSeconds);
            float warning = Mathf.Max(0f, warningSeconds);
            float period = Mathf.Max(periodSeconds, warning + erupt);
            float t = Mathf.Repeat(time + phaseSeconds, period);
            float dormant = period - warning - erupt;
            if (t < dormant)
            {
                progress = dormant > 0f ? t / dormant : 1f;
                return GeyserPhase.Dormant;
            }
            if (t < dormant + warning)
            {
                progress = warning > 0f ? (t - dormant) / warning : 1f;
                return GeyserPhase.Warning;
            }
            progress = Mathf.Clamp01((t - dormant - warning) / erupt);
            return GeyserPhase.Erupting;
        }

        private void Awake()
        {
            surface = GetComponent<RideableSurface>();
            if (column != null)
            {
                columnBaseScale = column.localScale;
                columnRenderers = column.GetComponentsInChildren<Renderer>(true);
            }
            if (warningPuff != null)
            {
                puffBaseScale = warningPuff.localScale;
                puffRenderers = warningPuff.GetComponentsInChildren<Renderer>(true);
            }
            if (rumbleVisual != null) rumbleHome = rumbleVisual.localPosition;
            SetShown(columnRenderers, ref columnShown, false);
            SetShown(puffRenderers, ref puffShown, false);
        }

        private void Update()
        {
            Phase = PhaseAt(Time.time, periodSeconds, warningSeconds, eruptSeconds, phaseSeconds, out float progress);

            if (rumbleVisual != null)
            {
                float strength = Phase == GeyserPhase.Warning ? progress : Phase == GeyserPhase.Erupting ? 1f : 0f;
                float w = Time.time * rumbleFrequency;
                rumbleVisual.localPosition = rumbleHome + new Vector3(Mathf.Sin(w), 0f, Mathf.Cos(w * 1.3f)) * (rumbleAmplitude * strength);
            }

            if (warningPuff != null)
            {
                bool show = Phase != GeyserPhase.Dormant;
                SetShown(puffRenderers, ref puffShown, show);
                if (show) warningPuff.localScale = puffBaseScale * (Phase == GeyserPhase.Warning ? Mathf.Lerp(0.35f, 1f, progress) : 1f);
            }

            if (column != null)
            {
                float k = 0f;
                if (Phase == GeyserPhase.Erupting)
                {
                    float erupt = Mathf.Max(0.01f, eruptSeconds);
                    float elapsed = progress * erupt;
                    k = Mathf.Clamp01(Mathf.Min(elapsed, erupt - elapsed) / columnRiseSeconds);
                }
                SetShown(columnRenderers, ref columnShown, k > 0.001f);
                column.localScale = new Vector3(columnBaseScale.x, columnBaseScale.y * Mathf.Max(k, 0.001f), columnBaseScale.z);
            }

            if (Phase == GeyserPhase.Erupting && surface.HasRider && Time.time >= nextLaunchTime)
            {
                nextLaunchTime = Time.time + launchCooldownSeconds;
                surface.Rider.Launch(launchVelocity);
            }
        }

        private static void SetShown(Renderer[] renderers, ref bool shown, bool on)
        {
            if (shown == on) return;
            shown = on;
            if (renderers == null) return;
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = on;
            }
        }
    }
}
