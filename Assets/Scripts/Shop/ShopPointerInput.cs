using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WeatherForSale.Shop
{
    /// <summary>The 3D props on the counter the player can click or tap.</summary>
    public enum ShopClickTarget
    {
        /// <summary>The map board: opens the map-select panel.</summary>
        MapBoard,
        /// <summary>The filled bottle: hands it to the customer.</summary>
        Bottle,
        /// <summary>The Achievement book.</summary>
        AchievementBook,
    }

    /// <summary>
    /// Mouse and touch picking of the 3D counter props through <see cref="Pointer.current"/> (the
    /// Input System's shared mouse/pen/touch device) and a physics raycast. Pointers over UI are
    /// ignored. A click fires on release over the same prop it was pressed on. The hovered (mouse)
    /// or pressed (touch, which has no hover) prop is highlighted by a small scale-up and a warm
    /// tint. The flow controller decides which props are enabled.
    /// Implements: story-002 criteria 3, 7 and 9 (mouse and touch).
    /// </summary>
    public sealed class ShopPointerInput : MonoBehaviour
    {
        /// <summary>One clickable prop: its collider and the visual that gets the hover cue.</summary>
        [Serializable]
        public sealed class Target
        {
            /// <summary>Which prop this is.</summary>
            public ShopClickTarget id;
            /// <summary>Collider hit by the pointer ray.</summary>
            public Collider collider;
            /// <summary>Transform scaled for the hover cue (usually the prop root).</summary>
            public Transform visual;

            [NonSerialized] public bool enabled;
            [NonSerialized] public Vector3 baseScale;
            [NonSerialized] public float highlight;
            [NonSerialized] public Renderer[] renderers;
            [NonSerialized] public Color[] baseColors;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Camera viewCamera;
        [SerializeField] private ShopUiConfig config;
        [SerializeField] private Target[] targets = Array.Empty<Target>();
        [SerializeField, Min(0.1f), Tooltip("Longest pointer ray, metres.")]
        private float maxRayDistance = 30f;
        [SerializeField, Min(0f), Tooltip("Ignore a second click within this many seconds (guards against a browser sending touch and emulated mouse for one tap).")]
        private float repeatGuardSeconds = 0.25f;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
        private PointerEventData uiPointerData;
        private EventSystem uiPointerDataOwner;
        private MaterialPropertyBlock block;
        private Target pressedTarget;
        private float lastClickTime = -10f;

        /// <summary>Raised when an enabled prop is clicked or tapped.</summary>
        public event Action<ShopClickTarget> Clicked;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (Target target in targets)
            {
                if (target == null) continue;
                if (target.visual == null && target.collider != null) target.visual = target.collider.transform;
                if (target.visual == null) continue;
                target.baseScale = target.visual.localScale;
                target.renderers = target.visual.GetComponentsInChildren<Renderer>(true);
                target.baseColors = new Color[target.renderers.Length];
                for (int i = 0; i < target.renderers.Length; i++)
                {
                    Material mat = target.renderers[i].sharedMaterial;
                    target.baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
                }
            }
        }

        /// <summary>Enables or disables clicking (and the hover cue) on one prop.</summary>
        public void SetEnabled(ShopClickTarget id, bool isEnabled)
        {
            foreach (Target target in targets)
            {
                if (target != null && target.id == id) target.enabled = isEnabled;
            }
        }

        private void Update()
        {
            Target hovered = null;
            Pointer pointer = Pointer.current;
            bool isTouch = false;

            if (pointer != null && viewCamera != null)
            {
                isTouch = pointer is Touchscreen;
                Vector2 position = pointer.position.ReadValue();
                if (!IsOverUi(position)) hovered = RaycastTarget(position);

                if (pointer.press.wasPressedThisFrame) pressedTarget = hovered;
                if (pointer.press.wasReleasedThisFrame)
                {
                    if (pressedTarget != null && pressedTarget == hovered && pressedTarget.enabled
                        && Time.unscaledTime - lastClickTime >= repeatGuardSeconds)
                    {
                        lastClickTime = Time.unscaledTime;
                        Clicked?.Invoke(pressedTarget.id);
                    }
                    pressedTarget = null;
                }
                // Touch has no hover: highlight only while the finger is down on the prop.
                if (isTouch && !pointer.press.isPressed) hovered = null;
            }

            float step = config != null ? config.hoverEaseSpeed * Time.unscaledDeltaTime : 1f;
            foreach (Target target in targets)
            {
                if (target == null || target.visual == null) continue;
                float goal = target == hovered && target.enabled ? 1f : 0f;
                if (Mathf.Approximately(goal, target.highlight)) continue;
                target.highlight = Mathf.MoveTowards(target.highlight, goal, step);
                ApplyHighlight(target);
            }
        }

        private Target RaycastTarget(Vector2 screenPosition)
        {
            Ray ray = viewCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) return null;
            foreach (Target target in targets)
            {
                if (target != null && target.collider == hit.collider && target.enabled) return target;
            }
            return null;
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            // PointerEventData does not expose its EventSystem publicly, so remember which one it was made for.
            if (uiPointerData == null || uiPointerDataOwner != eventSystem)
            {
                uiPointerData = new PointerEventData(eventSystem);
                uiPointerDataOwner = eventSystem;
            }
            uiPointerData.position = screenPosition;
            uiHits.Clear();
            eventSystem.RaycastAll(uiPointerData, uiHits);
            return uiHits.Count > 0;
        }

        private void ApplyHighlight(Target target)
        {
            float scale = config != null ? config.hoverScale : 1f;
            Color tint = config != null ? config.hoverTint : Color.white;
            target.visual.localScale = target.baseScale * Mathf.Lerp(1f, scale, target.highlight);

            for (int i = 0; i < target.renderers.Length; i++)
            {
                Renderer r = target.renderers[i];
                if (r == null) continue;
                if (target.highlight <= 0f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                Color baseColor = target.baseColors[i];
                Color lit = new Color(baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, baseColor.a);
                block.Clear();
                block.SetColor(BaseColorId, Color.LerpUnclamped(baseColor, lit, target.highlight));
                r.SetPropertyBlock(block);
            }
        }
    }
}
