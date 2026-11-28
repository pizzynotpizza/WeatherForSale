using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// One kind of boss attack. A pattern is an asset: <see cref="TempestBoss"/> plays the patterns of its
    /// <see cref="BossConfig"/> in turn, so a new attack is a new subclass plus an asset, with no change to the boss.
    /// Every pattern shows a warning shape for <see cref="telegraphSeconds"/> before it can hurt.
    /// Implements: story-008 criterion 2 (several patterns, each with a warning).
    /// </summary>
    public abstract class BossAttackPattern : ScriptableObject
    {
        [Tooltip("Name shown in the corner banner while the pattern runs.")]
        public string displayName = "Attack";

        [Min(0.1f), Tooltip("Seconds the warning shape shows before the hit lands, at difficulty 1.")]
        public float telegraphSeconds = 1.2f;

        [Min(0f), Tooltip("Speed the player is thrown away at when hit, metres per second. Near the arena edge it throws Sam off.")]
        public float knockback = 28f;

        /// <summary>Horizontal direction from <paramref name="origin"/> to the player; a random one when they are on top of each other.</summary>
        protected static Vector3 Away(IBossArena arena, Vector3 origin)
        {
            Vector3 d = arena.Player.position - origin;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f)
            {
                Vector2 r = Random.insideUnitCircle.normalized;
                return new Vector3(r.x, 0f, r.y);
            }
            return d.normalized;
        }

        /// <summary>
        /// Plays the attack. Yield until it is completely over (the last hit resolved and the shapes removed).
        /// </summary>
        /// <param name="arena">The arena to spawn shapes in and to hurt the player through.</param>
        /// <param name="difficulty">1 on the first round, higher on later rounds (more hits, shorter warnings).</param>
        public abstract IEnumerator Run(IBossArena arena, float difficulty);

        /// <summary>Warning time at <paramref name="difficulty"/>: shrinks a little each round, never below 0.45 s.</summary>
        protected float Telegraph(float difficulty)
        {
            return Mathf.Max(0.45f, telegraphSeconds / Mathf.Max(1f, difficulty));
        }
    }
}
