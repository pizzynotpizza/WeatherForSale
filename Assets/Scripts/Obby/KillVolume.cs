using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Trigger volume that counts as a fall (lava, void). Raises <see cref="PlayerEntered"/> when
    /// the player touches it; <see cref="ObbyLevelManager"/> then respawns the player.
    /// Implements: story-001 criterion 2 (falling respawns at the last checkpoint).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KillVolume : MonoBehaviour
    {
        /// <summary>Raised when the player enters the volume.</summary>
        public event Action PlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            PlayerEntered?.Invoke();
        }
    }
}
