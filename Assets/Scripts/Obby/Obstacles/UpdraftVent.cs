using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A wind vent: while the player is inside this trigger volume they are lifted upward
    /// (<see cref="PlayerController.AddLift"/>): sustained lift up to a top speed, so they rise through the
    /// air column and can steer out of it onto a higher ledge. Put the visible air column and vent
    /// grate under this object.
    /// Implements: story-004, Valley 2 (wind vents that blow the player upward); reusable for Glacier.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class UpdraftVent : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Upward acceleration added while inside, metres per second squared. Must beat gravity (25) to lift: the net lift is this minus gravity.")]
        private float liftAcceleration = 55f;

        [SerializeField, Min(0f), Tooltip("Upward speed the lift will not push the player beyond, metres per second.")]
        private float maxUpSpeed = 8.5f;

        private PlayerController playerInside;

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) playerInside = player;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) playerInside = null;
        }

        private void Update()
        {
            if (playerInside != null) playerInside.AddLift(liftAcceleration, maxUpSpeed);
        }
    }
}
