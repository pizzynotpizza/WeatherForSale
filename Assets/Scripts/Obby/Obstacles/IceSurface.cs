using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Slippery ground: while the player stands on it their ground acceleration is multiplied by
    /// <c>traction</c>, so they are slow to speed up and slow to stop and keep their momentum (they slide).
    /// Put it on the platform's collider object (it is a <see cref="RideableSurface"/>, so it can also sit
    /// next to a <see cref="CollapsingPlatform"/> for a cracked ice slab).
    /// Implements: story-004, Glacier 1 and 2 (slippery ice sheets, icy slope).
    /// </summary>
    public sealed class IceSurface : RideableSurface
    {
        [SerializeField, Range(0.02f, 1f), Tooltip("Multiplier on the player's ground acceleration (1 = normal ground). Smaller = more slippery.")]
        private float traction = 0.12f;

        /// <inheritdoc />
        public override float Traction => traction;
    }
}
