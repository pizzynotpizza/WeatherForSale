using System;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// Every tunable of the Tempest fight: the time limit, boss HP, the throw immunity, the attack cycle, the tired
    /// window, the suction and the words on screen. Attack patterns are separate assets listed here.
    /// Implements: story-008 criterion 9 (HP, damage, time, patterns and the tired window live in config).
    /// </summary>
    [CreateAssetMenu(fileName = "BossConfig", menuName = "Weather For Sale/Boss/Boss Config")]
    public sealed class BossConfig : ScriptableObject
    {
        [Header("Player")]
        [Min(0f), Tooltip("Seconds after a hit when the player cannot be thrown again (so overlapping attacks do not chain-throw Sam).")] public float hitImmuneSeconds = 0.6f;
        [Min(1f), Tooltip("How far below the arena floor Sam may fall before he is lost, metres.")] public float fallDeathDepth = 4f;

        [Header("Fight")]
        [Tooltip("Name shown on the boss bar.")] public string bossName = "Tempest";
        [Min(1f), Tooltip("Seconds to win before it is Game Over (3 minutes).")] public float fightSeconds = 180f;
        [Min(1), Tooltip("Boss hit points. One successful suction takes one; the phases follow from the HP left.")] public int bossMaxHp = 5;
        [Min(0f), Tooltip("Seconds of intro before the first attack.")] public float introSeconds = 2.5f;

        [Header("Phases (decided by the boss HP left)")]
        [Min(1), Tooltip("Phase 2 starts when the boss HP is this or lower.")] public int phase2AtHp = 4;
        [Min(1), Tooltip("Phase 3 starts when the boss HP is this or lower.")] public int phase3AtHp = 2;
        [Tooltip("Phase 1 plays only these patterns (one attack to start with).")] public BossAttackPattern[] phase1Patterns = Array.Empty<BossAttackPattern>();
        [Tooltip("Phases 2 and 3 play all of these patterns, once each per round, in random order.")] public BossAttackPattern[] patterns = Array.Empty<BossAttackPattern>();
        [Tooltip("How many attacks one round plays in phase 1, 2 and 3 (picked at random from the pool, so rounds differ). Keeps all 5 suctions inside the 3-minute clock.")] public int[] attacksPerRound = { 1, 2, 3 };
        [Tooltip("Difficulty of phase 1, 2 and 3: more hits, shorter warnings and shorter pauses.")] public float[] phaseDifficulty = { 1f, 1.5f, 2.2f };
        [Min(0f), Tooltip("Pause between two attacks at difficulty 1, seconds (divided by the difficulty).")] public float pauseBetweenAttacks = 1.0f;
        [Min(0.5f), Tooltip("Seconds the boss clutches its head after a successful suction.")] public float painSeconds = 2.2f;

        [Header("Storm Crystal (dropped from this phase on, after every successful suction)")]
        [Range(1, 3), Tooltip("Phase from which a successful suction drops the crystal.")] public int dropFromPhase = 3;
        [Min(1f), Tooltip("The crystal lands this far from the middle of the arena, metres.")] public float itemDropRadius = 9f;
        [Min(1f), Tooltip("Extra gain multiplier of the next suction after picking the crystal up.")] public float boostGainMultiplier = 1.8f;
        [Min(0f), Tooltip("Extra seconds of the next suction after picking the crystal up (and no gauge decay).")] public float boostExtraSeconds = 5f;

        [Header("Tired window")]
        [Min(0f), Tooltip("Seconds the boss sinks and shows the tired signal before the suction opens.")] public float tiredWarnSeconds = 0.9f;
        [Min(1f), Tooltip("Seconds the player has to fill the gauge before the boss recovers.")] public float suctionSeconds = 12f;

        [Header("Suction (keys, gauge size and steps come from the map SuctionConfig)")]
        [Min(0.1f), Tooltip("How much more one press adds than in the map suction. The boss suction is meant to be easier.")] public float suctionGainMultiplier = 2.2f;
        [Min(0f), Tooltip("How much of the map suction's gauge decay applies here (0 = none, 1 = the same).")] public float suctionDecayMultiplier = 0.3f;
        [Min(0.5f), Tooltip("Radius of the cloud of streaks that leave the boss, metres.")] public float suctionStreakRadius = 4f;
        [Tooltip("Colour of the suction streaks.")] public Color suctionStreakColor = new Color(0.8f, 0.5f, 1f, 0.9f);
        [Tooltip("Colour of the bottle Sam holds in the boss fight (the purple bottle).")] public Color bottleColour = new Color(0.72f, 0.45f, 1f, 1f);

        [Header("Arena")]
        [Min(4f), Tooltip("Walkable radius of the arena floor, metres.")] public float arenaRadius = 22f;
        [Tooltip("Show a rainbow in the arena when the boss is sealed (off: the sky only clears).")] public bool rainbowOnWin;

        [Header("Secret stars (by the time the fight took)")]
        [Min(1f), Tooltip("Seal Tempest within this many seconds for 3 secret stars.")] public float threeStarSeconds = 100f;
        [Min(1f), Tooltip("Seal Tempest within this many seconds for 2 secret stars; any win gets 1.")] public float twoStarSeconds = 140f;

        /// <summary>Secret stars (1-3) for a win that took <paramref name="seconds"/>.</summary>
        public int SecretStarsFor(float seconds) => seconds <= threeStarSeconds ? 3 : seconds <= twoStarSeconds ? 2 : 1;
        [Min(0f), Tooltip("Height of the boss body centre above the floor while it attacks, metres (the charge's low height: the 13 m body floats just off the floor, the whole of it on screen; the fight keeps this height).")] public float bossHoverHeight = 6.5f;
        [Min(0f), Tooltip("Height of the boss body centre above the floor while it is tired, metres (low enough to reach with the bottle).")] public float bossTiredHeight = 6.5f;

        [Header("Text")]
        public string introBanner = "Tempest is coming!";
        public string tiredBanner = "Tempest is tired! Collect it!";
        public string recoveredBanner = "It got away!";
        public string phase2Banner = "Tempest is angry!";
        public string phase3Banner = "Tempest is furious!";
        public string itemBanner = "Storm Crystal! The next suction is easier.";
        public string boostedTitle = "Boosted! Collect the Tempest!";
        public string promptFormat = "Press {0}!";
        public string tapLabel = "TAP!";
        public string collectingTitle = "Collect the Tempest!";
        public string gameOverTitle = "Game Over";
        public string timeUpTitle = "Time's Up!";
        public string restartLabel = "Try Again";
        public string victoryTitle = "Boss Sealed!";
        public string victoryButton = "Continue";
    }
}
