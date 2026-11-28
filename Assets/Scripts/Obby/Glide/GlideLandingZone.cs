using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// The air over a landing ledge: a gliding player who enters this trigger stops gliding
    /// (<see cref="GlideMode.End"/> as a landing) and drops onto the ledge under normal control. Make it reach
    /// above the leg's glide ceiling so nobody can fly over the ledge.
    /// Implements: story-004, Valley 1 "Canyon glide" (landing ledges end each glide leg).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GlideLandingZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other) => TryLand(other);

        private void OnTriggerStay(Collider other) => TryLand(other);

        private static void TryLand(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.TryGetComponent(out GlideMode glide) || !glide.IsGliding) return;
            glide.End(true);
        }
    }
}
