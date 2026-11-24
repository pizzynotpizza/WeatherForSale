using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Trigger volume marking the end of the level. Raises <see cref="Reached"/> when the player
    /// enters; <see cref="ObbyLevelManager"/> decides whether that still counts (timer not expired).
    /// Implements: story-001 criterion 4 (reaching the finish = level passed).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class FinishTrigger : MonoBehaviour
    {
        /// <summary>Raised when the player enters the finish volume.</summary>
        public event Action Reached;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Reached?.Invoke();
        }
    }
}
