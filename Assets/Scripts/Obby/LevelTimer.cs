using System;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Countdown timer for an obby level. Plain C# class (no Unity dependency) so it can be
    /// ticked by any owner with a delta time. Respawning never touches it: it only ever
    /// counts down until <see cref="Stop"/> or expiry.
    /// Implements: story-001 "time limit from config, keeps running across respawns".
    /// </summary>
    public sealed class LevelTimer
    {
        /// <summary>Raised once, when the remaining time reaches zero.</summary>
        public event Action Expired;

        /// <summary>Total duration this timer was created with, in seconds.</summary>
        public float Duration { get; }

        /// <summary>Seconds left, never below zero.</summary>
        public float Remaining { get; private set; }

        /// <summary>True while the timer is counting down.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the timer has run out.</summary>
        public bool HasExpired { get; private set; }

        /// <summary>Creates a stopped timer with the given duration in seconds.</summary>
        public LevelTimer(float durationSeconds)
        {
            Duration = Math.Max(0f, durationSeconds);
            Remaining = Duration;
        }

        /// <summary>Starts (or resumes) counting down. Has no effect once expired.</summary>
        public void Start()
        {
            if (!HasExpired) IsRunning = true;
        }

        /// <summary>Stops counting down, keeping the remaining time (used when the level is passed).</summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>Advances the timer by <paramref name="deltaSeconds"/> if it is running.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsRunning || deltaSeconds <= 0f) return;

            Remaining -= deltaSeconds;
            if (Remaining > 0f) return;

            Remaining = 0f;
            IsRunning = false;
            HasExpired = true;
            Expired?.Invoke();
        }
    }
}
