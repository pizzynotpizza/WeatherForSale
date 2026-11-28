using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// Hail falls in full rings around the boss, one ring after the other. A flat blue ring on the floor warns first;
    /// then the ring drops as a low wall. Stand in the empty lane between two rings, inside the first ring, or jump
    /// over the wall (it is low). A hit throws Sam outwards or inwards, away from the ring.
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern_HailRing", menuName = "Weather For Sale/Boss/Hail Ring")]
    public sealed class HailRingPattern : BossAttackPattern
    {
        [Min(1), Tooltip("Rings per volley at difficulty 1.")]
        public int rings = 3;

        [Min(1f), Tooltip("Distance between two rings, metres.")]
        public float ringSpacing = 6.5f;

        [Min(1f), Tooltip("Radius of the first ring, metres.")]
        public float firstRingRadius = 6f;

        [Min(0.5f), Tooltip("Width of one ring, metres. The lane between two rings is the spacing minus this.")]
        public float ringWidth = 3f;

        [Min(0.3f), Tooltip("Height of the dropped ring, metres. A jump clears it.")]
        public float wallHeight = 1.2f;

        [Range(0f, 120f), Tooltip("Width of a gap in the ring, degrees. 0 = a full, closed ring.")]
        public float gapDegrees = 0f;

        [Min(0.1f), Tooltip("Seconds between the start of two rings.")]
        public float ringIntervalSeconds = 1.0f;

        /// <inheritdoc />
        public override IEnumerator Run(IBossArena arena, float difficulty)
        {
            int count = Mathf.Min(Mathf.RoundToInt(rings + (difficulty - 1f)), 4);
            float warning = Telegraph(difficulty);
            Coroutine last = null;
            for (int i = 0; i < count; i++)
            {
                float radius = firstRingRadius + ringSpacing * i;
                if (radius + ringWidth * 0.5f > arena.Radius + 1f) break;
                last = arena.StartRoutine(Ring(arena, radius, Random.Range(0f, 360f), warning));
                yield return new WaitForSeconds(ringIntervalSeconds / Mathf.Sqrt(difficulty));
            }
            if (last != null) yield return last;
        }

        private IEnumerator Ring(IBossArena arena, float radius, float gapCentre, float warning)
        {
            float inner = radius - ringWidth * 0.5f, outer = radius + ringWidth * 0.5f;
            Vector3 floor = arena.Center + Vector3.up * arena.WarningHeight;

            // Warning: the ring lies flat and blinks faster as it nears.
            GameObject flat = arena.SpawnMesh(BossShapes.BuildRing(inner, outer, 0.04f, gapCentre, gapDegrees, 96), floor, new Color(0.25f, 0.7f, 1f, 0.6f));
            for (float t = 0f; t < warning; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / warning);
                SetAlpha(flat, 0.45f + 0.4f * Mathf.Abs(Mathf.Sin(t * (5f + 14f * k))));
                yield return null;
            }
            arena.Release(flat);

            // Hit: the ring drops as a low wall for a moment.
            arena.Flash();
            GameObject wall = arena.SpawnMesh(BossShapes.BuildRing(inner, outer, wallHeight, gapCentre, gapDegrees, 96), arena.Center + Vector3.up * 0.1f, new Color(0.75f, 0.95f, 1f, 0.85f));
            Vector3 rel = arena.Player.position - arena.Center;
            Vector3 flatRel = new Vector3(rel.x, 0f, rel.z);
            float dist = flatRel.magnitude;
            bool inside = dist >= inner && dist <= outer;
            if (inside && gapDegrees > 0f)
            {
                float angle = Mathf.Atan2(flatRel.x, flatRel.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, gapCentre)) < gapDegrees * 0.5f) inside = false;
            }
            bool low = rel.y < wallHeight;
            if (inside && low)
            {
                // Thrown to the nearer side of the ring: out past the edge or in towards the boss.
                Vector3 outward = dist > 0.01f ? flatRel.normalized : Vector3.forward;
                arena.HitPlayer(dist >= radius ? outward : -outward, knockback);
            }
            yield return new WaitForSeconds(0.3f);
            arena.Release(wall);
        }

        private static void SetAlpha(GameObject shape, float alpha)
        {
            if (shape == null) return;
            var r = shape.GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(0.25f, 0.7f, 1f, alpha));
            r.SetPropertyBlock(block);
        }
    }
}
