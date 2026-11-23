using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeatherForSale.Obby
{
    /// <summary>The phase an obby level is in.</summary>
    public enum ObbyLevelState
    {
        /// <summary>Timer running, player in control.</summary>
        Playing,
        /// <summary>Finish reached before the timer ran out.</summary>
        Completed,
        /// <summary>Timer ran out before the finish was reached.</summary>
        GameOver,
    }

    /// <summary>
    /// Owns the rules of one obby level: the countdown timer, checkpoints, respawning, the
    /// fall counter and the Playing / Completed / GameOver state. Collaborators are wired by
    /// explicit serialized references (no singletons); the UI listens to the events only.
    /// Implements: story-001 criteria 2-6 (design/game-brief.md "Challenge", "Goal &amp; fail state").
    /// Transitions: Playing to Completed (finish reached), Playing to GameOver (timer expired);
    /// both end states are final until <see cref="RestartLevel"/> reloads the scene.
    /// </summary>
    public sealed class ObbyLevelManager : MonoBehaviour
    {
        [SerializeField] private ObbyLevelConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private ThirdPersonCamera playerCamera;
        [SerializeField, Tooltip("Initial spawn point, used until a checkpoint is touched.")]
        private Transform startPoint;
        [SerializeField] private Checkpoint[] checkpoints = Array.Empty<Checkpoint>();
        [SerializeField] private FinishTrigger finish;
        [SerializeField] private KillVolume[] killVolumes = Array.Empty<KillVolume>();

        private LevelTimer timer;
        private Vector3 respawnPosition;
        private float respawnYaw;

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<ObbyLevelState> StateChanged;

        /// <summary>
        /// Raised after every respawn with the new total of falls/deaths. Story 005 (Achievements)
        /// subscribes to this; nothing is persisted here.
        /// </summary>
        public event Action<int> DeathCountChanged;

        /// <summary>Raised when a checkpoint becomes the current respawn point.</summary>
        public event Action<Checkpoint> CheckpointReached;

        /// <summary>Current phase of the level.</summary>
        public ObbyLevelState State { get; private set; } = ObbyLevelState.Playing;

        /// <summary>
        /// Number of falls/deaths (respawns) in this attempt. Incremented on every respawn,
        /// whether from the kill height or a <see cref="KillVolume"/>. Reset when the level restarts.
        /// </summary>
        public int DeathCount { get; private set; }

        /// <summary>The level countdown timer.</summary>
        public LevelTimer Timer => timer;

        /// <summary>Seconds left on the level timer.</summary>
        public float RemainingSeconds => timer.Remaining;

        /// <summary>True while the countdown keeps running after the level was completed (see <see cref="ContinueTimerAfterCompletion"/>).</summary>
        public bool IsInOvertime => overtime;

        private bool overtime;

        /// <summary>
        /// Restarts the stopped countdown after the level was completed, from the time that was left:
        /// the suction at the end of a map's last level runs on it. Running out then ends the level in
        /// GameOver (the usual Restart replays the level). Ignored unless the level is completed.
        /// </summary>
        public void ContinueTimerAfterCompletion()
        {
            if (State != ObbyLevelState.Completed || timer.HasExpired) return;
            overtime = true;
            timer.Start();
        }

        /// <summary>Stops the countdown started by <see cref="ContinueTimerAfterCompletion"/>, keeping the time left.</summary>
        public void StopTimer()
        {
            overtime = false;
            timer.Stop();
        }

        private void Awake()
        {
            timer = new LevelTimer(config.levelTimeSeconds);
            timer.Expired += OnTimerExpired;

            respawnPosition = startPoint != null ? startPoint.position : player.transform.position;
            respawnYaw = startPoint != null ? startPoint.eulerAngles.y : player.transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint != null) checkpoint.Touched += OnCheckpointTouched;
            }
            foreach (KillVolume volume in killVolumes)
            {
                if (volume != null) volume.PlayerEntered += OnPlayerFell;
            }
            if (finish != null) finish.Reached += OnFinishReached;
        }

        private void OnDisable()
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint != null) checkpoint.Touched -= OnCheckpointTouched;
            }
            foreach (KillVolume volume in killVolumes)
            {
                if (volume != null) volume.PlayerEntered -= OnPlayerFell;
            }
            if (finish != null) finish.Reached -= OnFinishReached;
        }

        private void OnDestroy()
        {
            if (timer != null) timer.Expired -= OnTimerExpired;
        }

        private void Start()
        {
            timer.Start();
        }

        private void Update()
        {
            if (overtime && State == ObbyLevelState.Completed)
            {
                timer.Tick(Time.deltaTime);
                return;
            }
            if (State != ObbyLevelState.Playing) return;

            timer.Tick(Time.deltaTime);

            // Timer expiry may have just ended the level.
            if (State == ObbyLevelState.Playing && player.transform.position.y < config.killHeight)
            {
                Respawn();
            }
        }

        /// <summary>Reloads the active scene, restarting this level from the beginning.</summary>
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnCheckpointTouched(Checkpoint checkpoint)
        {
            if (State != ObbyLevelState.Playing) return;

            respawnPosition = checkpoint.RespawnPosition;
            respawnYaw = checkpoint.RespawnYaw;

            foreach (Checkpoint other in checkpoints)
            {
                if (other != null) other.SetCurrent(other == checkpoint);
            }
            CheckpointReached?.Invoke(checkpoint);
        }

        private void OnPlayerFell()
        {
            if (State == ObbyLevelState.Playing) Respawn();
        }

        private void Respawn()
        {
            // The timer is deliberately not touched: time keeps running across respawns.
            player.TeleportTo(respawnPosition + Vector3.up * config.respawnHeightOffset, respawnYaw);
            if (playerCamera != null) playerCamera.SetYaw(respawnYaw);

            DeathCount++;
            DeathCountChanged?.Invoke(DeathCount);
        }

        private void OnFinishReached()
        {
            if (State != ObbyLevelState.Playing) return;
            timer.Stop();
            SetState(ObbyLevelState.Completed);
        }

        private void OnTimerExpired()
        {
            bool inOvertime = overtime && State == ObbyLevelState.Completed;
            if (State != ObbyLevelState.Playing && !inOvertime) return;
            overtime = false;
            SetState(ObbyLevelState.GameOver);
        }

        private void SetState(ObbyLevelState newState)
        {
            State = newState;
            player.ControlEnabled = newState == ObbyLevelState.Playing;
            player.SetVirtualMove(Vector2.zero);
            StateChanged?.Invoke(newState);
        }
    }
}
