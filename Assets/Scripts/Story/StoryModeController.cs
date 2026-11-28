using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherForSale.Progress;
using WeatherForSale.Shop;

namespace WeatherForSale.Story
{
    /// <summary>
    /// In the Shop scene, Story Mode: after the last of the four customers leaves, Uncle John walks up to
    /// the counter and says a few lines in the dialog box; then the ending is picked from the Shards
    /// collected (all four: the Secret / True path through the Tempest fight; fewer: the Normal Ending)
    /// and the Cutscene scene loads.
    /// Implements: story-006 criteria 4, 6, 7.
    /// </summary>
    public sealed class StoryModeController : MonoBehaviour
    {
        [SerializeField] private ShopFlowController flow;
        [SerializeField] private GameModeState state;
        [SerializeField] private ProgressService progress;
        [SerializeField] private FrontEndStrings strings;
        [SerializeField] private CustomerController customers;
        [SerializeField] private DialogBox dialog;
        [SerializeField, Tooltip("Uncle John as a customer (model only; his lines are in FrontEndStrings).")]
        private CustomerData uncleJohn;
        [SerializeField, Min(1), Tooltip("Shards needed for the Secret Ending (one per map).")] private int shardsForTrueEnding = 4;
        [SerializeField, Min(0f), Tooltip("Seconds of the empty shop before Uncle John walks in.")] private float delaySeconds = 1.0f;

        private bool started;
        private bool arrived;
        private bool advanced;

        private void OnEnable()
        {
            if (flow != null) flow.DayEnded += OnDayEnded;
            if (customers != null) customers.Arrived += OnArrived;
            if (dialog != null) dialog.Advanced += OnAdvanced;
        }

        private void OnDisable()
        {
            if (flow != null) flow.DayEnded -= OnDayEnded;
            if (customers != null) customers.Arrived -= OnArrived;
            if (dialog != null) dialog.Advanced -= OnAdvanced;
        }

        private void OnArrived() => arrived = true;
        private void OnAdvanced() => advanced = true;

        private void OnDayEnded()
        {
            if (started || state == null || state.IsFreeplay) return;
            started = true;
            StartCoroutine(EndOfDay());
        }

        private IEnumerator EndOfDay()
        {
            yield return new WaitForSeconds(delaySeconds);

            // Uncle John comes back to the counter and talks.
            if (customers != null && uncleJohn != null)
            {
                arrived = false;
                customers.Arrive(uncleJohn);
                float waited = 0f;
                while (!arrived && waited < 5f) { waited += Time.deltaTime; yield return null; }
            }
            if (dialog != null && strings != null && strings.uncleReturnLines != null)
            {
                foreach (string line in strings.uncleReturnLines)
                {
                    advanced = false;
                    dialog.Show(strings.uncleName, line, true);
                    while (!advanced) yield return null;
                }
                dialog.Hide();
            }

            int shards = progress != null ? progress.Data.shardMaps.Count : 0;
            state.SetCutscene(shards >= shardsForTrueEnding ? CutsceneId.TrueIntro : CutsceneId.NormalEnding);
            if (Application.CanStreamedLevelBeLoaded(state.cutsceneScene)) SceneManager.LoadScene(state.cutsceneScene);
            else Debug.LogWarning("[StoryModeController] Scene '" + state.cutsceneScene + "' is not in Build Settings.");
        }
    }
}
