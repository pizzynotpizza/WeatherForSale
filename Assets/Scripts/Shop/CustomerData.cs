using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// One shop customer: who they are, which model to show, what weather they want, which map
    /// sells it, and every line they say (problem, hints in three tiers, thank-you). Pure data; the
    /// shop flow never hardcodes any of it.
    /// Implements: design/game-brief.md character block and story-002 criterion 2.
    /// </summary>
    [CreateAssetMenu(fileName = "Customer", menuName = "Weather For Sale/Shop/Customer")]
    public sealed class CustomerData : ScriptableObject
    {
        [Tooltip("Name shown in the Dialog box, e.g. \"Bob\".")]
        public string displayName = "Customer";

        [Tooltip("Static (unrigged) model prefab with its pivot at the base centre, real-world scale.")]
        public GameObject prefab;

        [Tooltip("The weather this customer wants (rain, wind, sun, snow). Informational; never shown as the map name.")]
        public string weatherWord = "rain";

        [Tooltip("Id of the MapCatalog entry that solves this customer's problem.")]
        public string correctMapId = "rainforest";

        [Tooltip("First line the customer says on arrival. Must not name the map.")]
        [TextArea(2, 4)] public string problemLine = "";

        [Tooltip("Tier 1 hints (first wrong choices): about the problem, no place named. Keep at least 3.")]
        [TextArea(2, 4)] public string[] hintsVague = new string[3];

        [Tooltip("Tier 2 hints (second wrong choice): about the kind of place. Keep at least 3.")]
        [TextArea(2, 4)] public string[] hintsNarrow = new string[3];

        [Tooltip("Tier 3 hints (third wrong choice and later): details to spot on the map. Never the map or weather name. Keep at least 4.")]
        [TextArea(2, 4)] public string[] hintsClear = new string[4];

        /// <summary>The three hint tiers in order, vague to clear, for <see cref="HintSystem"/>.</summary>
        public string[][] HintTiers => new[] { hintsVague, hintsNarrow, hintsClear };

        [Tooltip("Line said when the bottle is handed over.")]
        [TextArea(2, 4)] public string thankYouLine = "";
    }
}
