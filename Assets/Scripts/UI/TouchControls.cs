using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WeatherForSale.Obby;

namespace WeatherForSale.UI
{
    /// <summary>
    /// On-screen touch controls: a floating virtual stick (left half of the screen), a jump button
    /// (right) and a camera-look area (rest of the right half; a second finger there turns the drag into
    /// a two-finger pinch zoom). Lives on one full-screen, transparent,
    /// raycast-receiving Image and sorts each touch by where it lands, so several fingers work at once.
    /// Visuals are shown only on touch devices (<c>Touchscreen.current != null</c>); on other devices the
    /// area stops receiving pointer events. Feeds the same <see cref="PlayerController"/> as the keyboard.
    /// Implements: story-001 criteria 1 and Implementation Notes ("touch buttons only on touch devices").
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class TouchControls : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [Header("Targets")]
        [SerializeField] private PlayerController player;
        [SerializeField] private ThirdPersonCamera playerCamera;

        [Header("Visuals (children; shown only on touch devices)")]
        [SerializeField] private GameObject visualsRoot;
        [SerializeField, Tooltip("Stick base image, moved to where the thumb lands.")]
        private RectTransform stickBase;
        [SerializeField, Tooltip("Stick knob image, a child of the base.")]
        private RectTransform stickKnob;
        [SerializeField, Tooltip("Jump button; a press inside this rect triggers a jump.")]
        private RectTransform jumpButton;
        [SerializeField, Tooltip("Where the stick base rests when not touched, in this rect's local space.")]
        private Vector2 stickRestPosition = new Vector2(-650f, -300f);

        [Header("Layout (canvas units)")]
        [SerializeField, Min(1f), Tooltip("Thumb travel that gives full stick deflection.")]
        private float stickRadius = 110f;
        [SerializeField, Range(0f, 0.9f), Tooltip("Fraction of stick travel ignored near the centre.")]
        private float stickDeadZone = 0.12f;

        [Header("Testing")]
        [SerializeField, Tooltip("Show and accept mouse input too, for testing in the editor without a touch screen.")]
        private bool forceVisible;

        private RectTransform rect;
        private Image raycastImage;
        private bool visible;

        private int stickPointer = NoPointer;
        private int jumpPointer = NoPointer;
        private int lookPointer = NoPointer;
        private int pinchPointer = NoPointer;
        private Vector2 lookPosition;
        private Vector2 pinchPosition;
        private Vector2 stickOrigin;

        private void Awake()
        {
            rect = (RectTransform)transform;
            raycastImage = GetComponent<Image>();
            ResetStickVisual();
            SetVisible(false);
        }

        private void Update()
        {
            // Cheap poll so controls also appear if a touch screen shows up after start (e.g. device simulator).
            bool shouldShow = forceVisible || Touchscreen.current != null;
            if (shouldShow != visible) SetVisible(shouldShow);
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private void SetVisible(bool show)
        {
            visible = show;
            if (visualsRoot != null) visualsRoot.SetActive(show);
            raycastImage.raycastTarget = show;
            if (!show) ReleaseAll();
        }

        private bool Accepts(PointerEventData eventData)
        {
            if (forceVisible) return true;
            // Ignore a mouse on hybrid devices; only real touches drive the controls.
            return eventData is not ExtendedPointerEventData extended || extended.pointerType == UIPointerType.Touch;
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!visible || !Accepts(eventData)) return;

            int id = eventData.pointerId;
            Camera cam = eventData.pressEventCamera;

            if (jumpButton != null && jumpPointer == NoPointer
                && RectTransformUtility.RectangleContainsScreenPoint(jumpButton, eventData.position, cam))
            {
                jumpPointer = id;
                player.RequestJump();
                player.SetVirtualJumpHeld(true); // held: the glide climbs
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, cam, out Vector2 local);

            if (local.x < 0f && stickPointer == NoPointer)
            {
                stickPointer = id;
                stickOrigin = local;
                if (stickBase != null) stickBase.anchoredPosition = local;
                if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
            }
            else if (local.x >= 0f && lookPointer == NoPointer)
            {
                lookPointer = id;
                lookPosition = eventData.position;
            }
            else if (local.x >= 0f && pinchPointer == NoPointer)
            {
                pinchPointer = id;
                pinchPosition = eventData.position;
            }
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            int id = eventData.pointerId;

            if (id == stickPointer)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out Vector2 local);

                Vector2 offset = Vector2.ClampMagnitude(local - stickOrigin, stickRadius);
                if (stickKnob != null) stickKnob.anchoredPosition = offset;

                Vector2 value = offset / stickRadius;
                float magnitude = value.magnitude;
                if (magnitude < stickDeadZone)
                {
                    value = Vector2.zero;
                }
                else
                {
                    // Re-scale so the output goes smoothly from 0 at the dead zone edge to 1.
                    value *= (magnitude - stickDeadZone) / (1f - stickDeadZone) / magnitude;
                }
                player.SetVirtualMove(value);
            }
            else if (id == lookPointer || id == pinchPointer)
            {
                if (pinchPointer == NoPointer)
                {
                    lookPosition = eventData.position;
                    if (playerCamera != null) playerCamera.AddTouchLook(eventData.delta);
                    return;
                }

                // Two fingers on the look area: the change in their gap zooms; no look while pinching.
                float before = Vector2.Distance(lookPosition, pinchPosition);
                if (id == lookPointer) lookPosition = eventData.position;
                else pinchPosition = eventData.position;
                if (playerCamera != null) playerCamera.AddPinchZoom(Vector2.Distance(lookPosition, pinchPosition) - before);
            }
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            int id = eventData.pointerId;

            if (id == stickPointer)
            {
                stickPointer = NoPointer;
                player.SetVirtualMove(Vector2.zero);
                ResetStickVisual();
            }
            else if (id == jumpPointer)
            {
                jumpPointer = NoPointer;
                player.SetVirtualJumpHeld(false);
            }
            else if (id == lookPointer)
            {
                // The finger left behind after a pinch carries on as the look finger.
                lookPointer = pinchPointer;
                lookPosition = pinchPosition;
                pinchPointer = NoPointer;
            }
            else if (id == pinchPointer)
            {
                pinchPointer = NoPointer;
            }
        }

        private void ReleaseAll()
        {
            stickPointer = NoPointer;
            jumpPointer = NoPointer;
            lookPointer = NoPointer;
            pinchPointer = NoPointer;
            if (player != null)
            {
                player.SetVirtualMove(Vector2.zero);
                player.SetVirtualJumpHeld(false);
            }
            ResetStickVisual();
        }

        private void ResetStickVisual()
        {
            if (stickBase != null) stickBase.anchoredPosition = stickRestPosition;
            if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
        }
    }
}
