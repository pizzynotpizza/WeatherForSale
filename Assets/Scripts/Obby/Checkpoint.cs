using System;
using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A checkpoint trigger volume. When the player touches it, it becomes the respawn point
    /// (see <see cref="ObbyLevelManager"/>). Needs a trigger collider on the same GameObject.
    /// Implements: story-001 criterion 2 (at least 2 checkpoints, respawn at last touched).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Where the player reappears. Defaults to this transform; its Y rotation is the facing.")]
        private Transform respawnPoint;

        [SerializeField, Tooltip("Renderer tinted to show whether the checkpoint has been reached (optional).")]
        private Renderer tintRenderer;

        [SerializeField, Tooltip("Tint before the checkpoint is reached.")]
        private Color inactiveColor = new Color(1f, 0.85f, 0.2f);

        [SerializeField, Tooltip("Tint once the checkpoint is reached.")]
        private Color activeColor = new Color(0.2f, 0.9f, 0.4f);

        private MaterialPropertyBlock propertyBlock;

        /// <summary>Raised when the player first touches this checkpoint, and each later time they touch it.</summary>
        public event Action<Checkpoint> Touched;

        /// <summary>True after the player has touched this checkpoint at least once.</summary>
        public bool IsActivated { get; private set; }

        /// <summary>World position the player respawns at (the surface point, before any height offset).</summary>
        public Vector3 RespawnPosition => (respawnPoint != null ? respawnPoint : transform).position;

        /// <summary>World yaw in degrees the player faces when respawning here.</summary>
        public float RespawnYaw => (respawnPoint != null ? respawnPoint : transform).eulerAngles.y;

        /// <summary>Marks this checkpoint as the current one and updates its tint.</summary>
        public void SetCurrent(bool isCurrent)
        {
            if (isCurrent) IsActivated = true;
            ApplyTint(IsActivated);
        }

        private void Awake()
        {
            ApplyTint(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Touched?.Invoke(this);
        }

        private void ApplyTint(bool active)
        {
            if (tintRenderer == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            tintRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, active ? activeColor : inactiveColor);
            tintRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
