using System;
using System.Collections.Generic;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Hints for the current customer in tiers (vague, narrow, clear). The first wrong map choice
    /// draws from tier 1, the second from tier 2, the third and later from tier 3, so hints still get
    /// clearer. Within a tier the hint is picked at random and is not repeated until the tier has
    /// been used up. Plain C# (the random source is injected) so it can be unit-tested without a scene.
    /// Implements: story-002 criterion 4 and Implementation Notes (hints stored in data).
    /// </summary>
    public sealed class HintSystem
    {
        private readonly Func<int, int> nextIndex;
        private string[][] tiers = Array.Empty<string[]>();
        private List<int>[] pool = Array.Empty<List<int>>();

        /// <param name="randomIndex">Returns a random int in [0, n). Defaults to <see cref="System.Random"/>.</param>
        public HintSystem(Func<int, int> randomIndex = null)
        {
            var rng = new Random();
            nextIndex = randomIndex ?? (n => rng.Next(n));
        }

        /// <summary>Number of wrong choices registered since <see cref="Begin"/>, minus one (-1 before any).</summary>
        public int Level { get; private set; } = -1;

        /// <summary>Number of tiers for the current customer.</summary>
        public int LevelCount => tiers.Length;

        /// <summary>Starts over for a new customer with <paramref name="hintTiers"/> (vague to clear).</summary>
        public void Begin(string[][] hintTiers)
        {
            tiers = hintTiers ?? Array.Empty<string[]>();
            pool = new List<int>[tiers.Length];
            Level = -1;
        }

        /// <summary>
        /// Registers a wrong choice and returns the hint to show, or null when the customer has no hints.
        /// </summary>
        public string RegisterWrongChoice()
        {
            if (tiers.Length == 0) return null;
            Level++;
            int tier = Math.Min(Level, tiers.Length - 1);
            string[] hints = tiers[tier];
            if (hints == null || hints.Length == 0) return null;

            if (pool[tier] == null || pool[tier].Count == 0)
            {
                pool[tier] = new List<int>(hints.Length);
                for (int i = 0; i < hints.Length; i++) pool[tier].Add(i);
            }
            int pick = nextIndex(pool[tier].Count);
            int hintIndex = pool[tier][pick];
            pool[tier].RemoveAt(pick);
            return hints[hintIndex];
        }
    }
}
