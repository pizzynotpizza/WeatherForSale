using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>Small helpers shared by the obstacle scripts.</summary>
    internal static class ObstacleUtility
    {
        /// <summary>
        /// Gives a moving obstacle a kinematic Rigidbody so Unity treats its colliders as dynamic
        /// (cheap to move every frame) instead of re-building the static collision tree.
        /// </summary>
        internal static void EnsureKinematicBody(GameObject target)
        {
            if (!target.TryGetComponent(out Rigidbody body)) body = target.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }
    }
}
