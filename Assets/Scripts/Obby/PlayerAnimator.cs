using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Drives the player character's Animator from <see cref="PlayerController"/> state:
    /// a 0..1 "Speed" float (idle → walk → run blend), a "Grounded" bool, a "VerticalSpeed" float
    /// and a "Jump" trigger fired when a jump starts. Purely visual; it never moves the player.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpId = Animator.StringToHash("Jump");

        [SerializeField, Tooltip("Movement source.")]
        private PlayerController player;

        [SerializeField, Tooltip("Animator on the character model (child of the player).")]
        private Animator animator;

        [SerializeField, Tooltip("How quickly the Speed parameter follows the real speed (higher = snappier).")]
        private float speedDamping = 12f;

        private float smoothedSpeed;

        private void OnEnable()
        {
            if (player != null) player.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            if (player != null) player.Jumped -= OnJumped;
        }

        private void Update()
        {
            if (player == null || animator == null) return;

            Vector3 v = player.Velocity;
            float horizontal = new Vector2(v.x, v.z).magnitude / Mathf.Max(0.01f, player.MaxMoveSpeed);
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, Mathf.Clamp01(horizontal), 1f - Mathf.Exp(-speedDamping * Time.deltaTime));

            animator.SetFloat(SpeedId, smoothedSpeed);
            animator.SetBool(GroundedId, player.IsGrounded);
            animator.SetFloat(VerticalSpeedId, v.y);
        }

        private void OnJumped()
        {
            if (animator != null) animator.SetTrigger(JumpId);
        }
    }
}
