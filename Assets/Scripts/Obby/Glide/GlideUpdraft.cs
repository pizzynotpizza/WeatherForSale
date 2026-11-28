using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A rising air column for gliders: while a gliding player is inside this trigger's bounds, their glide is
    /// lifted (<see cref="GlideMode.AddUpdraft"/>). Lift stops at the top of the volume, so make the top no
    /// higher than the leg's glide ceiling. Has no effect on a walking player.
    /// Membership is checked against the collider's bounds every frame (not trigger exit), so a respawn from
    /// inside the column never leaves a stale lift behind.
    /// Implements: story-004, Valley 1 "Canyon glide" (updraft after the Shard alcove).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GlideUpdraft : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Upward speed the column pulls the glider toward, metres per second.")]
        private float liftSpeed = 8f;

        [SerializeField, Min(0f), Tooltip("How quickly the glider's vertical speed reaches the lift, metres per second squared.")]
        private float liftAcceleration = 20f;

        [SerializeField, Tooltip("Height above the player's feet tested against the column, metres (about the body centre).")]
        private float probeHeight = 0.7f;

        private Collider column;
        private PlayerController player;
        private GlideMode glide;

        private void Awake()
        {
            column = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController entered = other.GetComponentInParent<PlayerController>();
            if (entered != null) player = entered;
        }

        private void Update()
        {
            if (player == null) return;
            if (glide == null && !player.TryGetComponent(out glide)) return;
            if (!glide.IsGliding) return;
            if (!column.bounds.Contains(player.transform.position + Vector3.up * probeHeight)) return;
            glide.AddUpdraft(liftSpeed, liftAcceleration);
        }
    }
}
