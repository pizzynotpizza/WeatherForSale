using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// The shop's sky through the Story day: bright and clear through every customer, grey only once the day is over and Uncle John
    /// comes back (something is still wrong with the weather). Sets the camera's
    /// sky colour, the fog, the ambient light and the sun from the customer index in <see cref="ShopSession"/>.
    /// Implements: story-006 (the user's request: a bright sky all day, grey when Uncle John returns).
    /// </summary>
    public sealed class ShopSkyMood : MonoBehaviour
    {
        [SerializeField] private ShopSession session;
        [SerializeField] private ShopFlowController flow;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Light sun;

        [Header("One entry per customer served (0, 1, 2, 3) and a last one for the end of the day")]
        [SerializeField] private Color[] skyColours =
        {
            new Color(0.36f, 0.70f, 1.00f), // bright, clear blue all day...
            new Color(0.36f, 0.70f, 1.00f),
            new Color(0.36f, 0.70f, 1.00f),
            new Color(0.36f, 0.70f, 1.00f),
            new Color(0.45f, 0.46f, 0.49f), // ...until Uncle John comes back: overcast
        };
        [SerializeField] private float[] sunIntensities = { 1.25f, 1.25f, 1.25f, 1.25f, 0.6f };
        [SerializeField] private Color[] ambientColours =
        {
            new Color(0.68f, 0.66f, 0.62f),
            new Color(0.68f, 0.66f, 0.62f),
            new Color(0.68f, 0.66f, 0.62f),
            new Color(0.68f, 0.66f, 0.62f),
            new Color(0.43f, 0.43f, 0.43f),
        };

        private void OnEnable()
        {
            if (flow != null) flow.DayEnded += OnDayEnded;
        }

        private void OnDisable()
        {
            if (flow != null) flow.DayEnded -= OnDayEnded;
        }

        private void Start()
        {
            Apply(session != null ? session.CustomerIndex : 0);
        }

        private void OnDayEnded() => Apply(int.MaxValue);

        /// <summary>Shows the sky for <paramref name="customersServed"/> (clamped to the last step).</summary>
        public void Apply(int customersServed)
        {
            int i = Mathf.Clamp(customersServed, 0, skyColours.Length - 1);
            Color sky = skyColours[i];
            if (viewCamera != null) viewCamera.backgroundColor = sky;
            RenderSettings.fogColor = sky;
            if (i < ambientColours.Length) RenderSettings.ambientLight = ambientColours[i];
            if (sun != null && i < sunIntensities.Length) sun.intensity = sunIntensities[i];
        }
    }
}
