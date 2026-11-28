using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// A wall of wind sweeps across the arena. A strip on the floor shows where it starts and which way it
    /// goes; jump over it when it reaches you (the wall is low, so a jump clears it).
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern_WindSweep", menuName = "Weather For Sale/Boss/Wind Sweep")]
    public sealed class WindSweepPattern : BossAttackPattern
    {
        [Min(1), Tooltip("Sweeps in a row at difficulty 1.")]
        public int sweeps = 1;

        [Min(1f), Tooltip("Seconds a sweep needs to cross the arena.")]
        public float crossSeconds = 3.4f;

        [Min(0.2f), Tooltip("Thickness of the wall along its way, metres.")]
        public float thickness = 1.2f;

        [Min(0.3f), Tooltip("Height of the wall, metres. The player is safe with feet higher than this.")]
        public float height = 0.9f;

        /// <inheritdoc />
        public override IEnumerator Run(IBossArena arena, float difficulty)
        {
            int count = Mathf.RoundToInt(sweeps + (difficulty - 1f) * 0.8f);
            float warning = Telegraph(difficulty);
            float cross = Mathf.Max(1.6f, crossSeconds / Mathf.Sqrt(difficulty));
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f);
                yield return Sweep(arena, angle, warning, cross);
            }
        }

        private IEnumerator Sweep(IBossArena arena, float angleDegrees, float warning, float cross)
        {
            Quaternion rot = Quaternion.Euler(0f, angleDegrees, 0f);
            Vector3 dir = rot * Vector3.forward;       // the way the wall travels
            float reach = arena.Radius + thickness;
            float width = arena.Radius * 2.4f;
            Vector3 floor = arena.Center + Vector3.up * arena.WarningHeight;

            // Warning: only a thin strip at the starting edge (no big rectangle over the floor).
            GameObject start = arena.SpawnBox(floor - dir * reach, new Vector3(width, 0.05f, thickness), rot, new Color(0.55f, 0.9f, 1f, 0.8f));
            for (float t = 0f; t < warning; t += Time.deltaTime)
            {
                float pulse = 0.55f + 0.4f * Mathf.Abs(Mathf.Sin(t * 10f));
                SetAlpha(start, pulse);
                yield return null;
            }
            arena.Release(start);

            GameObject wall = arena.SpawnBox(floor - dir * reach + Vector3.up * (height * 0.5f), new Vector3(width, height, thickness), rot, new Color(0.55f, 0.9f, 1f, 0.8f));
            for (float t = 0f; t < cross; t += Time.deltaTime)
            {
                float along = Mathf.Lerp(-reach, reach, t / cross);
                Vector3 pos = arena.Center + dir * along;
                wall.transform.position = new Vector3(pos.x, arena.Center.y + height * 0.5f, pos.z);

                Vector3 rel = arena.Player.position - arena.Center;
                float playerAlong = Vector3.Dot(new Vector3(rel.x, 0f, rel.z), dir);
                bool inside = Mathf.Abs(playerAlong - along) < thickness * 0.5f + 0.3f;
                bool low = arena.Player.position.y < arena.Center.y + height;
                if (inside && low) arena.HitPlayer(dir, knockback);
                yield return null;
            }
            arena.Release(wall);
        }

        private static void SetAlpha(GameObject box, float alpha)
        {
            if (box == null) return;
            var r = box.GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(0.55f, 0.9f, 1f, alpha));
            r.SetPropertyBlock(block);
        }
    }
}
