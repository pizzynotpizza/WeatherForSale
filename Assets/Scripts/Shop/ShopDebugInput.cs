using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Development shortcut: F9 simulates coming back from the map with a filled bottle
    /// (<see cref="ShopSession.ReturnWithBottle"/>, then <see cref="ShopFlowController.ResumeFromSession"/>),
    /// so delivery can be tested before story 003 wires the real loop. Compiled only in the
    /// Editor and development builds; in release builds the component does nothing.
    /// Implements: story-002 criterion 7 (testing aid).
    /// </summary>
    public sealed class ShopDebugInput : MonoBehaviour
    {
        [SerializeField] private ShopSession session;
        [SerializeField] private ShopFlowController flow;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) SimulateReturnWithBottle();
        }

        /// <summary>Marks the session as returned with a bottle and rebuilds the shop from it.</summary>
        public void SimulateReturnWithBottle()
        {
            if (session == null || flow == null) return;
            // A scene load already in flight cannot be cancelled; ignore the key then.
            if (flow.State == ShopState.Loading) return;
            session.ReturnWithBottle();
            flow.ResumeFromSession();
        }
#endif
    }
}
