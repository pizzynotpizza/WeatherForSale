using System;
using System.Collections.Generic;
using UnityEngine;
using WeatherForSale.Shop;

namespace WeatherForSale.Progress
{
    /// <summary>Thresholds of the counting achievements (data, set on the ProgressService asset).</summary>
    [Serializable]
    public sealed class AchievementRules
    {
        /// <summary>Deaths needed for <see cref="AchievementId.Die10Times"/>.</summary>
        [Min(1)] public int deathsNeeded = 10;

        /// <summary>Total best stars needed for <see cref="AchievementId.Earn12Stars"/>.</summary>
        [Min(1)] public int starsNeeded = 12;
    }

    /// <summary>
    /// Pure achievement logic over <see cref="ProgressData"/>: which conditions are met and which
    /// achievements are newly earned. An achievement is never unlocked twice.
    /// Implements: story-005 criteria 5 and 6 (the four conditions, no repeat unlock).
    /// </summary>
    public sealed class AchievementService
    {
        /// <summary>Every achievement, in book order.</summary>
        public static readonly AchievementId[] All =
        {
            AchievementId.Die10Times,
            AchievementId.Earn12Stars,
            AchievementId.NormalEnding,
            AchievementId.TrueEnding,
        };

        private readonly AchievementRules rules;

        /// <summary>Creates the service with <paramref name="rules"/> (defaults when null).</summary>
        public AchievementService(AchievementRules rules)
        {
            this.rules = rules ?? new AchievementRules();
        }

        /// <summary>True when <paramref name="data"/> already records <paramref name="id"/> as unlocked.</summary>
        public static bool IsUnlocked(ProgressData data, AchievementId id)
        {
            return data != null && data.achievements != null && data.achievements.Contains(id.ToString());
        }

        /// <summary>True when the condition of <paramref name="id"/> holds for <paramref name="data"/>.</summary>
        public bool IsConditionMet(ProgressData data, AchievementId id)
        {
            if (data == null) return false;
            switch (id)
            {
                case AchievementId.Die10Times: return data.totalDeaths >= rules.deathsNeeded;
                case AchievementId.Earn12Stars: return data.TotalStars >= rules.starsNeeded;
                case AchievementId.NormalEnding: return data.normalEndingSeen;
                case AchievementId.TrueEnding: return data.trueEndingSeen;
                default: return false;
            }
        }

        /// <summary>
        /// Unlocks, in <paramref name="data"/>, every achievement whose condition holds and that is not
        /// unlocked yet. Returns only the newly unlocked ones (empty when none).
        /// </summary>
        public List<AchievementId> Evaluate(ProgressData data)
        {
            var newlyUnlocked = new List<AchievementId>();
            if (data == null) return newlyUnlocked;
            data.FillMissing();
            foreach (AchievementId id in All)
            {
                if (IsUnlocked(data, id) || !IsConditionMet(data, id)) continue;
                data.achievements.Add(id.ToString());
                newlyUnlocked.Add(id);
            }
            return newlyUnlocked;
        }
    }
}
