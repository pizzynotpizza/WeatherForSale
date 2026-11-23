using System;

namespace WeatherForSale.Suction
{
    /// <summary>
    /// The "Collecting" gauge of the suction minigame. Plain C# (no Unity dependency) so it can be
    /// tested without a scene. Value runs 0..<see cref="Max"/>, starts at 0 and is clamped on every
    /// change. Optional decay pulls it down over time until it is full; once full it stays full.
    /// Every time the value first reaches a new multiple of <see cref="StepSize"/> below the top,
    /// <see cref="StepReached"/> fires (the minigame draws a new key / moves the tap button). Decay
    /// below a step never makes it fire again for the same step.
    /// Implements: story-003 criteria 2-6 and Implementation Notes ("gauge logic in a plain C# class").
    /// </summary>
    public sealed class SuctionGauge
    {
        /// <summary>Raised with the 1-based step number when a new step is reached (not raised for the full gauge).</summary>
        public event Action<int> StepReached;

        /// <summary>Raised once, when the gauge reaches <see cref="Max"/>.</summary>
        public event Action Filled;

        /// <summary>Top of the gauge (100 by default).</summary>
        public float Max { get; }

        /// <summary>Gauge units between re-randomise steps; 0 or less disables steps.</summary>
        public float StepSize { get; }

        /// <summary>Gauge units lost per second while not full; 0 disables decay.</summary>
        public float DecayPerSecond { get; }

        /// <summary>Current value, always within 0..<see cref="Max"/>.</summary>
        public float Value { get; private set; }

        /// <summary>Value as a fraction 0..1.</summary>
        public float Normalized => Max > 0f ? Value / Max : 1f;

        /// <summary>True once the gauge reached <see cref="Max"/>; it no longer changes after that.</summary>
        public bool IsFull { get; private set; }

        /// <summary>Highest step reached so far (0 = none).</summary>
        public int HighestStep { get; private set; }

        /// <summary>Creates an empty gauge.</summary>
        /// <param name="max">Top value; values below 1 become 1.</param>
        /// <param name="stepSize">Units per re-randomise step (e.g. 25 for every 25%); 0 = no steps.</param>
        /// <param name="decayPerSecond">Units lost per second; 0 = no decay.</param>
        public SuctionGauge(float max = 100f, float stepSize = 25f, float decayPerSecond = 0f)
        {
            Max = Math.Max(1f, max);
            StepSize = Math.Max(0f, stepSize);
            DecayPerSecond = Math.Max(0f, decayPerSecond);
        }

        /// <summary>Adds <paramref name="amount"/> (one correct press/tap). Ignored when not positive or already full.</summary>
        public void Add(float amount)
        {
            if (IsFull || amount <= 0f) return;
            Value = Math.Min(Max, Value + amount);

            if (Value >= Max)
            {
                IsFull = true;
                Filled?.Invoke();
                return;
            }

            if (StepSize <= 0f) return;
            int step = (int)Math.Floor(Value / StepSize);
            while (HighestStep < step)
            {
                HighestStep++;
                StepReached?.Invoke(HighestStep);
            }
        }

        /// <summary>Applies decay for <paramref name="deltaSeconds"/>. Never goes below 0; does nothing once full.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsFull || DecayPerSecond <= 0f || deltaSeconds <= 0f) return;
            Value = Math.Max(0f, Value - DecayPerSecond * deltaSeconds);
        }
    }
}
