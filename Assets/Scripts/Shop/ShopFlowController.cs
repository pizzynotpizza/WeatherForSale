using UnityEngine;
using UnityEngine.UI;
using WeatherForSale.UI;

namespace WeatherForSale.Shop
{
    /// <summary>The phase the shop is in.</summary>
    public enum ShopState
    {
        /// <summary>Customer sliding in to the counter.</summary>
        Arriving,
        /// <summary>Dialog typing the problem line or a hint.</summary>
        Talking,
        /// <summary>Waiting for the player to click the map board.</summary>
        Idle,
        /// <summary>Map-select panel open.</summary>
        MapOpen,
        /// <summary>Loading screen up, map scene loading.</summary>
        Loading,
        /// <summary>Back with a bottle: bottle on the counter, then hop and thank-you.</summary>
        Delivering,
        /// <summary>Served customer sliding away.</summary>
        Leaving,
        /// <summary>All customers served (placeholder screen until story 006).</summary>
        EndOfDay,
    }

    /// <summary>
    /// Runs the first-person shop: a customer arrives and tells their problem, the player clicks the
    /// map board, a wrong map gives the next hint, the right map shows the Loading screen and loads
    /// the map, and coming back with a bottle (<see cref="ShopSession.ReturnedWithBottle"/>) lets the
    /// player hand it over before the next customer arrives. After the last customer, an
    /// "End of day" placeholder shows. There is no guide pixie.
    /// Transitions are limited to <see cref="Allowed"/>; <see cref="ResumeFromSession"/> is the only
    /// entry point that may jump to Arriving, Delivering or EndOfDay from any state.
    /// Implements: story-002 (design/game-brief.md Features 1, Core loop, First minute, Avoid).
    /// </summary>
    public sealed class ShopFlowController : MonoBehaviour
    {
        // Explicit transition table (from, to).
        private static readonly ShopState[,] Allowed =
        {
            { ShopState.Arriving, ShopState.Talking },
            { ShopState.Talking, ShopState.Idle },
            { ShopState.Talking, ShopState.MapOpen },   // impatient player clicks the map mid-line
            { ShopState.Idle, ShopState.MapOpen },
            { ShopState.MapOpen, ShopState.Idle },      // closed without choosing
            { ShopState.MapOpen, ShopState.Talking },   // wrong map: next hint
            { ShopState.MapOpen, ShopState.Loading },   // right map
            { ShopState.Loading, ShopState.Idle },      // scene missing from Build Settings
            { ShopState.Delivering, ShopState.Leaving },
            { ShopState.Leaving, ShopState.Arriving },
            { ShopState.Leaving, ShopState.EndOfDay },
        };

        [Header("Data")]
        [SerializeField] private CustomerRoster roster;
        [SerializeField] private MapCatalog catalog;
        [SerializeField] private ShopSession session;
        [SerializeField] private ShopStrings strings;

        [Header("Scene")]
        [SerializeField] private CustomerController customers;
        [SerializeField] private ShopPointerInput pointer;
        [SerializeField, Tooltip("Filled bottle on the counter; hidden until the player returns with one.")]
        private GameObject bottleOnCounter;

        [Header("UI")]
        [SerializeField] private DialogBox dialog;
        [SerializeField] private MapSelectPanel mapPanel;
        [SerializeField] private LoadingScreen loadingScreen;
        [SerializeField] private AchievementBookPanel achievementBook;
        [SerializeField, Tooltip("Saved progress (story 005): the Achievement book reads its unlocks.")]
        private WeatherForSale.Progress.ProgressService progress;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField, Tooltip("Short instruction shown at the top (open the map / hand over the bottle).")]
        private Text promptText;
        [SerializeField] private GameObject endOfDayPanel;
        [SerializeField] private Text endOfDayTitle;
        [SerializeField] private Text endOfDayBody;

        private readonly HintSystem hints = new HintSystem();
        private bool handedOver;
        private bool hopDone;
        private bool thanksDone;
        private CanvasGroup dialogGroup, promptGroup;

        /// <summary>Raised when every customer of the day has been served (the End of day shows).</summary>
        public event System.Action DayEnded;

        /// <summary>Current phase.</summary>
        public ShopState State { get; private set; } = ShopState.Arriving;

        /// <summary>The customer being served, or null after the last one.</summary>
        public CustomerData CurrentCustomer => roster != null && session != null ? roster.Get(session.CustomerIndex) : null;

        /// <summary>Current hint level (-1 before any wrong choice).</summary>
        public int HintLevel => hints.Level;

        private void OnEnable()
        {
            if (customers != null)
            {
                customers.Arrived += OnCustomerArrived;
                customers.HopFinished += OnHopFinished;
                customers.Left += OnCustomerLeft;
            }
            if (pointer != null) pointer.Clicked += OnPropClicked;
            if (dialog != null)
            {
                dialog.LineCompleted += OnLineCompleted;
                dialog.Advanced += OnDialogAdvanced;
            }
            if (mapPanel != null)
            {
                mapPanel.MapChosen += OnMapChosen;
                mapPanel.CloseRequested += OnMapPanelClosed;
            }
            if (loadingScreen != null) loadingScreen.LoadFailed += OnLoadFailed;
        }

        private void OnDisable()
        {
            if (customers != null)
            {
                customers.Arrived -= OnCustomerArrived;
                customers.HopFinished -= OnHopFinished;
                customers.Left -= OnCustomerLeft;
            }
            if (pointer != null) pointer.Clicked -= OnPropClicked;
            if (dialog != null)
            {
                dialog.LineCompleted -= OnLineCompleted;
                dialog.Advanced -= OnDialogAdvanced;
            }
            if (mapPanel != null)
            {
                mapPanel.MapChosen -= OnMapChosen;
                mapPanel.CloseRequested -= OnMapPanelClosed;
            }
            if (loadingScreen != null) loadingScreen.LoadFailed -= OnLoadFailed;
        }

        private void Start()
        {
            if (endOfDayTitle != null) endOfDayTitle.text = strings.endOfDayTitle;
            if (endOfDayBody != null) endOfDayBody.text = strings.endOfDayBody;
            if (achievementBook != null && progress != null) achievementBook.SetSource(progress);
            ResumeFromSession();
        }

        private void Update()
        {
            bool modal = (pauseMenu != null && pauseMenu.IsOpen)
                         || (achievementBook != null && achievementBook.IsOpen)
                         || (mapPanel != null && mapPanel.IsOpen)
                         || State == ShopState.Loading;
            if (dialog != null) dialog.InputBlocked = modal;
            // A panel on top (map, book, pause) hides the dialog box and the prompt, so neither shows through it.
            SetHidden(ref dialogGroup, dialog != null ? dialog.gameObject : null, modal);
            SetHidden(ref promptGroup, promptText != null ? promptText.gameObject : null, modal);

            if (pointer == null) return;
            pointer.SetEnabled(ShopClickTarget.MapBoard, !modal && (State == ShopState.Idle || State == ShopState.Talking));
            pointer.SetEnabled(ShopClickTarget.Bottle, !modal && State == ShopState.Delivering && !handedOver);
            pointer.SetEnabled(ShopClickTarget.AchievementBook, !modal && State != ShopState.MapOpen);
        }

        // ------------------------------------------------------------------ public entry points

        /// <summary>
        /// Rebuilds the shop from <see cref="ShopSession"/>: End of day when every customer is
        /// served, the bottle on the counter when the player came back with one, otherwise the
        /// current customer arrives. Called on scene start; the F9 debug key calls it too.
        /// </summary>
        public void ResumeFromSession()
        {
            if (mapPanel != null) mapPanel.Close();
            if (loadingScreen != null) loadingScreen.Hide();
            if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
            if (bottleOnCounter != null) bottleOnCounter.SetActive(false);
            if (dialog != null) dialog.Hide();
            SetPrompt(null);
            handedOver = hopDone = thanksDone = false;

            CustomerData customer = CurrentCustomer;
            if (customer == null)
            {
                customers.Clear();
                ForceState(ShopState.EndOfDay);
                ShowEndOfDay();
                return;
            }

            hints.Begin(customer.HintTiers);
            if (session.ReturnedWithBottle)
            {
                customers.PlaceAtCounter(customer);
                ForceState(ShopState.Delivering);
                if (bottleOnCounter != null) bottleOnCounter.SetActive(true);
                SetPrompt(strings.deliverPrompt);
                return;
            }

            ForceState(ShopState.Arriving);
            customers.Arrive(customer);
        }

        /// <summary>Same as clicking the map board (for tools and screenshots).</summary>
        public void OpenMapPanel()
        {
            if (State != ShopState.Idle && State != ShopState.Talking) return;
            if (dialog != null) dialog.CompleteLine();
            if (!TrySetState(ShopState.MapOpen)) return;
            SetPrompt(null);
            mapPanel.Open();
        }

        /// <summary>Same as pressing the map with <paramref name="mapId"/> in the open panel.</summary>
        public void ChooseMap(string mapId)
        {
            if (State != ShopState.MapOpen) OpenMapPanel();
            MapEntry entry = catalog != null ? catalog.Find(mapId) : null;
            if (entry != null) OnMapChosen(entry);
        }

        /// <summary>Same as clicking the bottle on the counter.</summary>
        public void HandOverBottle()
        {
            if (State != ShopState.Delivering || handedOver) return;
            handedOver = true;
            if (bottleOnCounter != null) bottleOnCounter.SetActive(false);
            SetPrompt(null);
            CustomerData customer = CurrentCustomer;
            dialog.Show(customer.displayName, customer.thankYouLine, true);
            customers.Hop();
        }

        /// <summary>Opens the Achievement book (same as clicking it on the counter).</summary>
        public void OpenAchievementBook()
        {
            if (achievementBook != null) achievementBook.Open();
        }

        // ------------------------------------------------------------------ event handlers

        private void OnPropClicked(ShopClickTarget target)
        {
            switch (target)
            {
                case ShopClickTarget.MapBoard: OpenMapPanel(); break;
                case ShopClickTarget.Bottle: HandOverBottle(); break;
                case ShopClickTarget.AchievementBook: OpenAchievementBook(); break;
            }
        }

        private void OnCustomerArrived()
        {
            if (State != ShopState.Arriving) return;
            CustomerData customer = CurrentCustomer;
            if (!TrySetState(ShopState.Talking)) return;
            dialog.Show(customer.displayName, customer.problemLine, false);
        }

        private void OnLineCompleted()
        {
            if (State == ShopState.Talking && TrySetState(ShopState.Idle)) SetPrompt(strings.idlePrompt);
        }

        private void OnMapChosen(MapEntry map)
        {
            if (State != ShopState.MapOpen || map == null) return;
            CustomerData customer = CurrentCustomer;
            mapPanel.Close();

            if (map.id == customer.correctMapId)
            {
                if (!TrySetState(ShopState.Loading)) return;
                session.RecordDeparture(session.CustomerIndex, map.id);
                loadingScreen.Begin(map, catalog.minLoadingSeconds);
                return;
            }

            // Wrong map: no confirmation, no level; the next hint and choose again.
            if (!TrySetState(ShopState.Talking)) return;
            string hint = hints.RegisterWrongChoice();
            dialog.Show(customer.displayName, hint ?? customer.problemLine, false);
        }

        private void OnMapPanelClosed()
        {
            if (State != ShopState.MapOpen) return;
            mapPanel.Close();
            if (TrySetState(ShopState.Idle)) SetPrompt(strings.idlePrompt);
        }

        private void OnLoadFailed(MapEntry map)
        {
            if (State == ShopState.Loading && TrySetState(ShopState.Idle)) SetPrompt(strings.idlePrompt);
        }

        private void OnDialogAdvanced()
        {
            if (State != ShopState.Delivering || !handedOver) return;
            thanksDone = true;
            TryLeave();
        }

        private void OnHopFinished()
        {
            if (State != ShopState.Delivering || !handedOver) return;
            hopDone = true;
            TryLeave();
        }

        private void TryLeave()
        {
            if (!hopDone || !thanksDone) return;
            if (!TrySetState(ShopState.Leaving)) return;
            dialog.Hide();
            session.CompleteDelivery();
            customers.Leave();
        }

        private void OnCustomerLeft()
        {
            if (State != ShopState.Leaving) return;
            handedOver = hopDone = thanksDone = false;
            CustomerData next = CurrentCustomer;
            if (next == null)
            {
                if (TrySetState(ShopState.EndOfDay)) ShowEndOfDay();
                return;
            }
            if (!TrySetState(ShopState.Arriving)) return;
            hints.Begin(next.HintTiers);
            customers.Arrive(next);
        }

        // ------------------------------------------------------------------ state helpers

        private void ShowEndOfDay()
        {
            if (dialog != null) dialog.Hide();
            SetPrompt(null);
            // Story Mode (story 006) takes over the end of the day; the panel is the fallback.
            if (DayEnded != null) DayEnded.Invoke();
            else if (endOfDayPanel != null) endOfDayPanel.SetActive(true);
        }

        private static void SetHidden(ref CanvasGroup group, GameObject target, bool hidden)
        {
            if (target == null) return;
            if (group == null && !target.TryGetComponent(out group)) group = target.AddComponent<CanvasGroup>();
            group.alpha = hidden ? 0f : 1f;
        }

        private void SetPrompt(string text)
        {
            if (promptText == null) return;
            bool show = !string.IsNullOrEmpty(text);
            promptText.gameObject.SetActive(show);
            if (show) promptText.text = text;
        }

        private bool TrySetState(ShopState next)
        {
            if (!IsAllowed(State, next))
            {
                Debug.LogWarning("[ShopFlowController] Ignored transition " + State + " -> " + next + ".");
                return false;
            }
            State = next;
            return true;
        }

        private void ForceState(ShopState next)
        {
            State = next;
        }

        /// <summary>True when the transition table allows <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static bool IsAllowed(ShopState from, ShopState to)
        {
            for (int i = 0; i < Allowed.GetLength(0); i++)
            {
                if (Allowed[i, 0] == from && Allowed[i, 1] == to) return true;
            }
            return false;
        }
    }
}
