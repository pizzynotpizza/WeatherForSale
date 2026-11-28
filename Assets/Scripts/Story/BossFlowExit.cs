using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherForSale.Boss;

namespace WeatherForSale.Story
{
    /// <summary>
    /// In the Tempest arena: after "Boss Sealed" and Continue, Story Mode plays the True Ending cutscene;
    /// Freeplay (the secret boss) goes back to Level Select without an ending.
    /// Implements: story-006 criterion 7, story-007 Implementation Notes.
    /// </summary>
    public sealed class BossFlowExit : MonoBehaviour
    {
        [SerializeField] private BossFightController fight;
        [SerializeField] private GameModeState state;

        private void OnEnable()
        {
            if (fight != null) fight.ContinueRequested += OnContinue;
        }

        private void OnDisable()
        {
            if (fight != null) fight.ContinueRequested -= OnContinue;
        }

        private void OnContinue()
        {
            if (state == null) return;
            string scene = state.cutsceneScene;
            if (state.IsFreeplay) scene = state.levelSelectScene;
            else
            {
                state.SetSecretStars(fight != null ? fight.SecretStars : 0); // for the Story Complete screen
                state.SetCutscene(CutsceneId.TrueEnding);
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning("[BossFlowExit] Scene '" + scene + "' is not in Build Settings.");
        }
    }
}
