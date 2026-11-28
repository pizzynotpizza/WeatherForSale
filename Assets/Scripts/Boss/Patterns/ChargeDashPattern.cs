using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// The boss leaps out past the edge of the arena, hovers there at its usual height and charges for several seconds, then drops and
    /// dashes across it in a straight line. A red strip on the floor shows the lane the whole time. The strip follows the player at first,
    /// then locks for the last part of the charge, so there is time to step out of the lane. Anyone in the lane when
    /// the boss comes through is hit and thrown sideways.
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern_ChargeDash", menuName = "Weather For Sale/Boss/Charge Dash")]
    public sealed class ChargeDashPattern : BossAttackPattern
    {
        [Min(1f), Tooltip("Seconds of charging (the warning strip is on screen) before the dive, at difficulty 1.")]
        public float chargeSeconds = 5f;

        [Min(0.3f), Tooltip("Last seconds of the charge in which the strip no longer follows the player.")]
        public float lockedSeconds = 1.6f;

        [Min(1f), Tooltip("Half the width of the lane, metres (about the boss's body).")]
        public float laneHalfWidth = 4f;

        [Min(5f), Tooltip("Speed of the dive, metres per second.")]
        public float dashSpeed = 52f;


        [Min(0f), Tooltip("How far outside the arena edge the boss lands, metres.")]
        public float startOutside = 4f;

        [Min(0.1f), Tooltip("Seconds the boss takes to drop from the sky to the floor before it dashes.")]
        public float landSeconds = 0.8f;

        [Min(0f), Tooltip("Seconds the boss stays on the floor, shaking, after landing and before the dash.")]
        public float landedPauseSeconds = 0.4f;

        [Min(0f), Tooltip("Seconds the boss needs to get back to the middle after the dive.")]
        public float recoverSeconds = 1.2f;

        /// <inheritdoc />
        public override IEnumerator Run(IBossArena arena, float difficulty)
        {
            float charge = Mathf.Max(3f, chargeSeconds / Mathf.Sqrt(difficulty));
            float locked = Mathf.Min(lockedSeconds, charge * 0.5f);
            Transform boss = arena.Boss;
            arena.SetBossMoving(true);
            float floorY = arena.Center.y;
            float edge = arena.Radius + startOutside;

            // The dive starts outside the arena, high up, on the side the lane comes from.
            Vector3 dir = Flat(arena.Player.position - boss.position, Vector3.forward);
            float length = edge + arena.Radius + startOutside;
            Vector3 laneFloor = arena.Center + Vector3.up * arena.WarningHeight;
            GameObject lane = arena.SpawnBox(laneFloor, new Vector3(laneHalfWidth * 2f, 0.04f, length), Quaternion.LookRotation(dir), new Color(1f, 0.1f, 0.08f, 0.45f));
            GameObject rim = arena.SpawnBox(laneFloor + Vector3.up * 0.02f, new Vector3(laneHalfWidth * 2f + 0.6f, 0.03f, length + 0.6f), Quaternion.LookRotation(dir), new Color(1f, 0.1f, 0.08f, 0.35f));

            for (float t = 0f; t < charge; t += Time.deltaTime)
            {
                bool lockedNow = t >= charge - locked;
                if (!lockedNow)
                {
                    // Aim from the edge point through the player.
                    Vector3 start = arena.Center - dir * edge;
                    Vector3 want = Flat(arena.Player.position - start, dir);
                    dir = Vector3.Slerp(dir, want, 1f - Mathf.Exp(-3f * Time.deltaTime)).normalized;
                }
                Vector3 startPoint = arena.Center - dir * edge;
                PlaceLane(lane, startPoint, dir, length, floorY + arena.WarningHeight);
                PlaceLane(rim, startPoint, dir, length + 0.6f, floorY + arena.WarningHeight + 0.02f);
                float k = Mathf.Clamp01(t / charge);
                float blink = lockedNow ? 0.6f + 0.35f * Mathf.Abs(Mathf.Sin(t * 16f)) : 0.40f + 0.15f * k;
                SetColor(lane, new Color(1f, lockedNow ? 0.08f : 0.2f, 0.08f, blink));

                // The boss flies up to the start of the lane and shudders there.
                Vector3 hover = new Vector3(startPoint.x, floorY + arena.BossRestHeight, startPoint.z);
                boss.position = Vector3.Lerp(boss.position, hover, 1f - Mathf.Exp(-2.2f * Time.deltaTime))
                                + new Vector3(Mathf.Sin(t * 45f), Mathf.Cos(t * 38f), Mathf.Sin(t * 31f)) * (0.04f + 0.12f * k);
                yield return null;
            }

            // Land: the boss drops from the sky to the floor at the start of the lane, and waits there a moment.
            Vector3 origin = arena.Center - dir * edge;
            Vector3 landFrom = boss.position;
            Vector3 landTo = new Vector3(origin.x, floorY + arena.BossRestHeight, origin.z);
            for (float t = 0f; t < landSeconds; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / landSeconds);
                boss.position = Vector3.Lerp(landFrom, landTo, k * k); // gathers speed as it falls
                yield return null;
            }
            boss.position = landTo;
            arena.Flash();
            for (float t = 0f; t < landedPauseSeconds; t += Time.deltaTime)
            {
                boss.position = landTo + new Vector3(Mathf.Sin(t * 60f), 0f, Mathf.Cos(t * 53f)) * 0.15f;
                yield return null;
            }

            // Dash: straight along the floor, through the arena and out the far side.
            float distance = length;
            float duration = distance / dashSpeed;
            bool hit = false;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float travelled = distance * Mathf.Clamp01(t / duration);
                Vector3 pos = origin + dir * travelled;
                boss.position = new Vector3(pos.x, floorY + arena.BossRestHeight, pos.z);
                if (!hit)
                {
                    Vector3 rel = arena.Player.position - boss.position;
                    rel.y = 0f;
                    float along = Vector3.Dot(rel, dir);
                    float across = Vector3.Dot(rel, Vector3.Cross(Vector3.up, dir));
                    if (Mathf.Abs(across) < laneHalfWidth && along > -laneHalfWidth && along < laneHalfWidth)
                    {
                        hit = true;
                        Vector3 side = across >= 0f ? Vector3.Cross(Vector3.up, dir) : -Vector3.Cross(Vector3.up, dir);
                        arena.HitPlayer(side + dir * 0.4f, knockback * 1.3f);
                    }
                }
                yield return null;
            }
            arena.Release(lane);
            arena.Release(rim);

            // Recover: rise and glide back to the middle.
            arena.SetBossMoving(false);
            yield return new WaitForSeconds(recoverSeconds);
        }

        private static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.01f ? v.normalized : new Vector3(fallback.x, 0f, fallback.z).normalized;
        }

        private static void PlaceLane(GameObject strip, Vector3 start, Vector3 dir, float length, float y)
        {
            if (strip == null) return;
            Vector3 centre = start + dir * (length * 0.5f);
            strip.transform.SetPositionAndRotation(new Vector3(centre.x, y, centre.z), Quaternion.LookRotation(dir));
        }

        private static void SetColor(GameObject shape, Color c)
        {
            if (shape == null) return;
            var r = shape.GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", c);
            r.SetPropertyBlock(block);
        }
    }
}
