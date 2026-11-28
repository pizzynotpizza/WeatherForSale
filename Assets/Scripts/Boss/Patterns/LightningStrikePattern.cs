using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// Lightning on single spots: a red circle appears on the floor and fills up, then a bolt lands in it.
    /// Step out of the circle before it fills. The bolts hunt the player: most land on or near where the
    /// player stands now, and no two circles overlap, so there is always room to run. A hit throws Sam; a throw near
    /// the edge of the arena is a fall.
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern_Lightning", menuName = "Weather For Sale/Boss/Lightning Strike")]
    public sealed class LightningStrikePattern : BossAttackPattern
    {
        [Min(1), Tooltip("Strikes in a volley at difficulty 1.")]
        public int strikes = 4;

        [Min(0.1f), Tooltip("Seconds between the start of two strikes.")]
        public float intervalSeconds = 0.6f;

        [Min(0.5f), Tooltip("Radius of the hit circle, metres.")]
        public float strikeRadius = 2.6f;

        [Range(0f, 1f), Tooltip("Chance that a strike lands right on the player (the rest land near the player).")]
        public float onPlayerChance = 0.6f;

        [Min(0f), Tooltip("How far from the player the other strikes may land, metres.")]
        public float scatterRadius = 7f;

        [Min(0f), Tooltip("Extra gap kept between two circles that are on screen together, metres.")]
        public float circleGap = 0.6f;

        /// <inheritdoc />
        public override IEnumerator Run(IBossArena arena, float difficulty)
        {
            int count = Mathf.RoundToInt(strikes + (difficulty - 1f) * 2f);
            float warning = Telegraph(difficulty);
            var active = new List<Vector3>(); // centres of circles that have not landed yet
            Coroutine last = null;
            for (int i = 0; i < count; i++)
            {
                if (TryPickSpot(arena, active, i == 0, out Vector3 spot))
                {
                    last = arena.StartRoutine(Strike(arena, spot, warning, active));
                }
                yield return new WaitForSeconds(intervalSeconds / Mathf.Sqrt(difficulty));
            }
            if (last != null) yield return last;
            while (active.Count > 0) yield return null;
        }

        private bool TryPickSpot(IBossArena arena, List<Vector3> active, bool forcePlayer, out Vector3 spot)
        {
            float minDistance = strikeRadius * 2f + circleGap;
            for (int attempt = 0; attempt < 14; attempt++)
            {
                Vector3 p = arena.Player.position;
                bool onPlayer = forcePlayer || (attempt < 4 && Random.value < onPlayerChance);
                if (!onPlayer)
                {
                    Vector2 jitter = Random.insideUnitCircle.normalized * Random.Range(strikeRadius * 1.5f, scatterRadius);
                    p += new Vector3(jitter.x, 0f, jitter.y);
                }
                p = ClampToArena(arena, p);
                bool free = true;
                foreach (Vector3 other in active)
                {
                    Vector3 d = other - p;
                    d.y = 0f;
                    if (d.magnitude < minDistance) { free = false; break; }
                }
                if (!free) continue;
                spot = p;
                return true;
            }
            spot = default;
            return false;
        }

        private IEnumerator Strike(IBossArena arena, Vector3 target, float warning, List<Vector3> active)
        {
            active.Add(target);
            Vector3 floor = new Vector3(target.x, arena.Center.y + arena.WarningHeight, target.z);
            GameObject outline = arena.SpawnDisc(floor, strikeRadius, new Color(1f, 0.05f, 0.05f, 0.65f));
            GameObject fill = arena.SpawnDisc(floor + Vector3.up * 0.03f, 0.05f, new Color(1f, 0.35f, 0.2f, 0.95f));
            // A pale column over the circle so it can be seen from far away and from a low camera.
            GameObject column = arena.SpawnDisc(floor + Vector3.up * 4f, strikeRadius, new Color(1f, 0.15f, 0.1f, 0.16f));
            column.transform.localScale = new Vector3(strikeRadius * 2f, 4f, strikeRadius * 2f);
            for (float t = 0f; t < warning; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / warning);
                SetDiscRadius(fill, Mathf.Lerp(0.05f, strikeRadius, k));
                // The outline blinks faster as the strike gets closer.
                float blink = 0.55f + 0.35f * Mathf.Abs(Mathf.Sin(t * (6f + 14f * k)));
                SetAlpha(outline, blink);
                yield return null;
            }
            arena.Release(fill);
            arena.Release(column);

            arena.Flash();
            GameObject bolt = arena.SpawnBox(floor + Vector3.up * 10f, new Vector3(0.5f, 20f, 0.5f), Quaternion.identity, new Color(0.9f, 0.96f, 1f, 0.95f));
            Vector3 flat = arena.Player.position - floor;
            flat.y = 0f;
            if (flat.magnitude <= strikeRadius) arena.HitPlayer(Away(arena, floor), knockback);
            yield return new WaitForSeconds(0.18f);
            arena.Release(bolt);
            arena.Release(outline);
            active.Remove(target);
        }

        private static Vector3 ClampToArena(IBossArena arena, Vector3 p)
        {
            Vector3 d = p - arena.Center;
            d.y = 0f;
            float max = arena.Radius - 1f;
            if (d.magnitude > max) d = d.normalized * max;
            return arena.Center + d;
        }

        private static void SetDiscRadius(GameObject disc, float radius)
        {
            if (disc != null) disc.transform.localScale = new Vector3(radius * 2f, disc.transform.localScale.y, radius * 2f);
        }

        private static void SetAlpha(GameObject shape, float alpha)
        {
            if (shape == null) return;
            var r = shape.GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 0.05f, 0.05f, alpha));
            r.SetPropertyBlock(block);
        }
    }
}
