using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Drifts its child objects (thin white streaks) along a local direction and wraps them around, so a
    /// zone looks like it has air moving through it. Can be hidden and shown (<see cref="SetVisible"/>);
    /// <see cref="WindGust"/> shows the streaks only while it blows. Each child's start position should
    /// have no component along <c>localDirection</c>.
    /// Implements: story-004, Valley (wind streaks, air columns of wind vents).
    /// </summary>
    public sealed class StreakDrift : MonoBehaviour
    {
        [SerializeField, Tooltip("Local direction the streaks travel.")]
        private Vector3 localDirection = Vector3.right;

        [SerializeField, Min(0f), Tooltip("Travel speed, metres per second.")]
        private float speed = 12f;

        [SerializeField, Min(0.1f), Tooltip("Length of the loop, metres: a streak that travels this far wraps back to the start.")]
        private float loopLength = 12f;

        private Transform[] items;
        private Vector3[] homes;
        private float[] offsets;
        private Renderer[] renderers;
        private bool visible = true;

        /// <summary>Shows or hides all streaks.</summary>
        public void SetVisible(bool value)
        {
            if (visible == value) return;
            visible = value;
            if (renderers == null) return;
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = value;
            }
        }

        private void Awake()
        {
            int count = transform.childCount;
            items = new Transform[count];
            homes = new Vector3[count];
            offsets = new float[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = transform.GetChild(i);
                homes[i] = items[i].localPosition;
                offsets[i] = loopLength * i / Mathf.Max(1, count);
            }
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (!visible) return;
            Vector3 dir = localDirection.normalized;
            float travelled = Time.time * speed;
            for (int i = 0; i < items.Length; i++)
            {
                float d = Mathf.Repeat(travelled + offsets[i], loopLength) - loopLength * 0.5f;
                items[i].localPosition = homes[i] + dir * d;
            }
        }
    }
}
