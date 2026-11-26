using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Marks a collider (or a hierarchy of colliders) as ground the player can ride. While the player
    /// stands on it the <see cref="PlayerController"/> carries them along with the surface's movement
    /// (translation and rotation of this transform) plus <see cref="SurfaceVelocity"/>, and tells the
    /// surface who is standing on it. Obstacles that react to a rider (bounce pad, collapsing
    /// platform) subscribe to <see cref="RiderLanded"/> / <see cref="RiderLeft"/> or poll <see cref="HasRider"/>.
    /// Plain static ground never needs this component.
    /// Implements: story-004 shared obstacle toolkit.
    /// </summary>
    public class RideableSurface : MonoBehaviour
    {
        /// <summary>
        /// Extra drift in world space (metres per second) applied to a rider on top of the carry from
        /// moving this transform. Zero for ordinary platforms; a rolling log overrides it.
        /// </summary>
        public virtual Vector3 SurfaceVelocity => Vector3.zero;

        /// <summary>
        /// Multiplier on the player's ground acceleration while standing on this surface (1 = normal grip,
        /// below 1 = slippery: slow to start, slow to stop). An ice surface overrides it.
        /// </summary>
        public virtual float Traction => 1f;

        /// <summary>The player standing on this surface right now, or null.</summary>
        public PlayerController Rider { get; private set; }

        /// <summary>True while the player is grounded on this surface.</summary>
        public bool HasRider => Rider != null;

        /// <summary>Raised on the frame the player becomes grounded on this surface.</summary>
        public event Action<PlayerController> RiderLanded;

        /// <summary>Raised on the frame the player stops being grounded on this surface (jump, fall, respawn).</summary>
        public event Action<PlayerController> RiderLeft;

        /// <summary>Called by <see cref="PlayerController"/> only. Pass null when the player leaves.</summary>
        internal void SetRider(PlayerController player)
        {
            if (player == Rider) return;
            PlayerController previous = Rider;
            Rider = player;
            if (player != null) RiderLanded?.Invoke(player);
            else RiderLeft?.Invoke(previous);
        }
    }
}
