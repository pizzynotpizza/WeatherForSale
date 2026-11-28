using System;
using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>Phases of the boss fight, in the order the cycle goes through them.</summary>
    public enum BossState
    {
        /// <summary>Not started.</summary>
        Idle,
        /// <summary>Opening seconds, no attack yet.</summary>
        Intro,
        /// <summary>Playing an attack pattern.</summary>
        Attack,
        /// <summary>Sinking, showing the tired signal; the suction opens next.</summary>
        Tired,
        /// <summary>The player fills the suction gauge.</summary>
        Suction,
        /// <summary>A suction landed: the boss clutches its head in pain.</summary>
        Pain,
        /// <summary>HP is 0: drawn into the bottle.</summary>
        Defeated,
        /// <summary>The fight ended (player down or time up).</summary>
        Stopped,
    }

    /// <summary>
    /// Tempest, the secret boss. A small state machine: Attack(pattern) -> Tired -> Suction -> Attack ... until
    /// HP is 0 (Defeated). The attacks are <see cref="BossAttackPattern"/> assets, so this class knows none of them.
    /// A successful suction takes one HP; when the window ends before the gauge is full the boss recovers.
    /// Implements: story-008 criteria 2, 5, 6, 7 and the Implementation Notes (state machine, patterns as assets).
    /// </summary>
    public sealed class TempestBoss : MonoBehaviour
    {
        [SerializeField] private BossConfig config;
        [SerializeField, Tooltip("The Tempest model; turns to face the player and bobs.")] private Transform visual;
        [SerializeField] private BossSuction suction;
        [SerializeField, Tooltip("Where the boss is drawn to when defeated (the purple bottle).")] private Transform absorbTarget;
        [SerializeField, Min(0.2f), Tooltip("Seconds the defeat animation takes.")] private float defeatSeconds = 2.4f;
        [SerializeField, Tooltip("Optional rigged model animator. When set, the pain moment triggers \"Pain\"; without one the boss shakes and squashes instead.")] private Animator painAnimator;

        private IBossArena arena;
        private float targetHeight;
        private Vector3 baseScale = Vector3.one;
        private Vector3 baseLocalPosition;  // the model's centring offset under the boss root (kept through the pain shake)
        private Coroutine loop;
        private float painStarted;

        /// <summary>Current state.</summary>
        public BossState State { get; private set; } = BossState.Idle;

        /// <summary>Current hit points.</summary>
        public int Hp { get; private set; }

        /// <summary>Maximum hit points.</summary>
        public int MaxHp => config != null ? config.bossMaxHp : 1;

        /// <summary>Completed rounds (one round = every pattern of the phase once, then the tired window).</summary>
        public int Round { get; private set; }

        /// <summary>Phase 1-3, decided by the HP left (see <see cref="BossConfig"/>).</summary>
        public int Phase => PhaseForHp(Hp);

        /// <summary>Raised on every state change.</summary>
        public event Action<BossState> StateChanged;

        /// <summary>Raised with (hp, max) whenever HP changes.</summary>
        public event Action<int, int> HpChanged;

        /// <summary>Raised with the pattern's display name when an attack starts.</summary>
        public event Action<string> PatternStarted;

        /// <summary>Raised when a suction succeeded and took HP.</summary>
        public event Action Hit;

        /// <summary>Raised with the new phase when it changes (2 or 3).</summary>
        public event Action<int> PhaseChanged;

        /// <summary>Raised after a successful suction when the boss drops a Storm Crystal.</summary>
        public event Action ItemDropped;

        /// <summary>Raised when the suction window ended before the gauge was full.</summary>
        public event Action SuctionFailed;

        /// <summary>Raised when the defeat animation starts (the bottle may appear now).</summary>
        public event Action DefeatStarted;

        /// <summary>Raised once the boss is gone.</summary>
        public event Action Defeated;

        private int PhaseForHp(int hp)
        {
            if (config == null) return 1;
            if (hp <= config.phase3AtHp) return 3;
            if (hp <= config.phase2AtHp) return 2;
            return 1;
        }

        /// <summary>Sets the height of the body centre above the floor; the boss glides there. Reset by every new attack.</summary>
        /// <summary>Height of the body centre above the floor at the start of the fight (where the scene places the boss). Every round returns here.</summary>
        public float RestHeight { get; private set; }

        /// <summary>Sets the height of the body centre above the floor (reset to <see cref="RestHeight"/> by every new attack).</summary>
        public void SetTargetHeight(float centreHeight)
        {
            targetHeight = centreHeight;
        }

        /// <summary>Gives the boss the arena it attacks in. Call before <see cref="Begin"/>.</summary>
        public void Initialize(IBossArena bossArena)
        {
            arena = bossArena;
            Hp = config.bossMaxHp;
            // One height for the whole fight: where the boss stands at the start (the scene places it at bossHoverHeight).
            RestHeight = transform.position.y - arena.Center.y;
            if (RestHeight < 1f) RestHeight = config.bossHoverHeight;
            targetHeight = RestHeight;
            if (visual != null)
            {
                baseScale = visual.localScale;
                baseLocalPosition = visual.localPosition;
            }
            HpChanged?.Invoke(Hp, config.bossMaxHp);
        }

        /// <summary>Starts the attack cycle.</summary>
        public void Begin()
        {
            if (loop != null || arena == null) return;
            loop = StartCoroutine(Cycle());
        }

        /// <summary>Stops the cycle (player down or time up). The boss stays where it is.</summary>
        public void Stop()
        {
            if (loop != null) StopCoroutine(loop);
            loop = null;
            if (suction != null) suction.End();
            if (State != BossState.Defeated) SetState(BossState.Stopped);
        }

        private void Update()
        {
            if (arena == null || State == BossState.Defeated) return;
            float floor = arena.Center.y;
            float bob = Mathf.Sin(Time.time * 1.6f) * 0.25f;
            if (!attackMovesBoss)
            {
                // Between attacks the boss drifts back to the middle of the arena and to its hover height.
                // While a pattern moves the boss itself (the charge) this component leaves the position alone.
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, floor + targetHeight + bob, 1f - Mathf.Exp(-4f * Time.deltaTime));
                p.x = Mathf.Lerp(p.x, arena.Center.x, 1f - Mathf.Exp(-2.5f * Time.deltaTime));
                p.z = Mathf.Lerp(p.z, arena.Center.z, 1f - Mathf.Exp(-2.5f * Time.deltaTime));
                transform.position = p;
            }

            if (State == BossState.Pain && visual != null && painAnimator == null)
            {
                // No rig yet: flinch with a shake and a squash that eases out.
                float k = Mathf.Clamp01(1f - (Time.time - painStarted) / config.painSeconds);
                visual.localPosition = baseLocalPosition + new Vector3(Mathf.Sin(Time.time * 55f), Mathf.Cos(Time.time * 47f) * 0.5f, 0f) * (0.35f * k);
                visual.localScale = baseScale * (1f - 0.12f * Mathf.Sin(Mathf.Clamp01(1f - k) * Mathf.PI));
            }

            if (visual != null && arena.Player != null)
            {
                Vector3 to = arena.Player.position - visual.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                {
                    visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-5f * Time.deltaTime));
                }
            }
        }

        /// <summary>True while a pattern moves the boss sideways itself (the charge); the boss then stays out of the way.</summary>
        public bool attackMovesBoss;

        private IEnumerator Cycle()
        {
            SetState(BossState.Intro);
            yield return new WaitForSeconds(config.introSeconds);

            while (true)
            {
                int phase = Phase;
                float difficulty = config.phaseDifficulty != null && config.phaseDifficulty.Length >= phase ? config.phaseDifficulty[phase - 1] : 1f + 0.7f * (phase - 1);
                BossAttackPattern[] pool = phase == 1 && config.phase1Patterns != null && config.phase1Patterns.Length > 0 ? config.phase1Patterns : config.patterns;
                int attacks = config.attacksPerRound != null && config.attacksPerRound.Length >= phase ? Mathf.Max(1, config.attacksPerRound[phase - 1]) : pool.Length;
                int played = 0;
                foreach (BossAttackPattern pattern in Shuffled(pool))
                {
                    if (pattern == null) continue;
                    if (played++ >= attacks) break;
                    targetHeight = RestHeight;
                    attackMovesBoss = false;
                    SetState(BossState.Attack);
                    PatternStarted?.Invoke(pattern.displayName);
                    yield return StartCoroutine(pattern.Run(arena, difficulty));
                    attackMovesBoss = false;  // never left "moved by the attack": back to the middle at the start height
                    targetHeight = RestHeight;
                    yield return new WaitForSeconds(config.pauseBetweenAttacks / difficulty);
                }

                // Tired: sink down, show the signal, then open the suction.
                SetState(BossState.Tired);
                targetHeight = RestHeight; // same height while tired (the bottle reaches it)
                yield return new WaitForSeconds(config.tiredWarnSeconds);

                SetState(BossState.Suction);
                suction.Begin();
                float left = suction.WindowSeconds;
                while (left > 0f && !suction.IsFull)
                {
                    left -= Time.deltaTime;
                    yield return null;
                }
                bool success = suction.IsFull;
                bool finalBlow = success && Hp <= 1;
                suction.End(finalBlow); // the final blow keeps Sam posed with the bottle: the boss is drawn into it

                if (success)
                {
                    int phaseBefore = Phase;
                    Hp = Mathf.Max(0, Hp - 1);
                    HpChanged?.Invoke(Hp, config.bossMaxHp);
                    Hit?.Invoke();
                    if (Hp == 0)
                    {
                        yield return StartCoroutine(Defeat());
                        loop = null;
                        yield break;
                    }

                    yield return StartCoroutine(Pain());
                    if (Phase != phaseBefore) PhaseChanged?.Invoke(Phase);
                    if (Phase >= config.dropFromPhase) ItemDropped?.Invoke();
                    yield return new WaitForSeconds(0.6f);
                }
                else
                {
                    SuctionFailed?.Invoke();
                    yield return new WaitForSeconds(0.8f);
                }
                Round++;
            }
        }

        private IEnumerator Pain()
        {
            SetState(BossState.Pain);
            painStarted = Time.time;
            if (painAnimator != null) painAnimator.SetTrigger("Pain");
            yield return new WaitForSeconds(config.painSeconds);
            if (visual != null)
            {
                visual.localPosition = baseLocalPosition; // not zero: that dropped the centring offset and lifted the model after the first hit
                visual.localScale = baseScale;
            }
        }

        private IEnumerator Defeat()
        {
            SetState(BossState.Defeated);
            DefeatStarted?.Invoke();
            Vector3 from = transform.position;
            for (float t = 0f; t < defeatSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / defeatSeconds);
                // Into the bottle in Sam's hands (it moves as he heaves), or the floor bottle when there is no pose.
                Vector3 to = suction != null && suction.BottleMouth != suction.transform.position ? suction.BottleMouth
                    : absorbTarget != null ? absorbTarget.position + Vector3.up * 0.6f : from;
                transform.position = Vector3.Lerp(from, to, k);
                if (visual != null)
                {
                    visual.localScale = baseScale * Mathf.Max(0.02f, 1f - k);
                    visual.Rotate(0f, 540f * Time.deltaTime * k, 0f, Space.World);
                }
                yield return null;
            }
            if (visual != null) visual.gameObject.SetActive(false);
            Defeated?.Invoke();
        }

        private void SetState(BossState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        private static BossAttackPattern[] Shuffled(BossAttackPattern[] source)
        {
            var copy = (BossAttackPattern[])source.Clone();
            for (int i = copy.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
