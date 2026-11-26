using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Rotates this transform at a constant speed around a local axis: a slowly turning floating rock
    /// (with a <see cref="RideableSurface"/> so the rider is carried round) or a fan rotor whose blades
    /// carry <see cref="KillVolume"/> triggers. Driven by absolute time, so the angle is deterministic
    /// and frame-rate independent.
    /// Implements: story-004, Valley 1 (floating rocks that turn slowly) and Valley 2 (giant fans).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField, Tooltip("Local axis to rotate around.")]
        private Vector3 axis = Vector3.up;

        [SerializeField, Tooltip("Rotation speed, degrees per second (negative = the other way).")]
        private float degreesPerSecond = 15f;

        [SerializeField, Tooltip("Angle at time zero, degrees (offsets neighbouring spinners from each other).")]
        private float phaseDegrees;

        private Quaternion baseLocalRotation;

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            baseLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(phaseDegrees + degreesPerSecond * Time.time, axis.normalized);
        }
    }
}
