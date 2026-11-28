#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Boss;
using WeatherForSale.Obby;
using WeatherForSale.Progress;
using WeatherForSale.Story;
using WeatherForSale.Suction;

namespace WeatherForSale.Dev
{
    /// <summary>
    /// Editor-only play-test bot. Plays the untested routes the way a player does: it presses the real UI
    /// buttons (pointer click events), taps Space through cutscenes and presses the shown key in every
    /// suction prompt through the Input System. It steers Sam back toward the arena centre with the
    /// virtual stick during the boss fight. One shortcut: it puts Sam on the obby finish instead of running
    /// the course (the route under test starts after the obby). Writes a step log and screenshots to
    /// <see cref="outFolder"/>. Not in builds.
    /// </summary>
    public sealed class PlaytestBot : MonoBehaviour
    {
        public enum Route { SecretEnding, FreeplayMap, SecretBossFromLevelSelect, All, BossProbe, Screens }

        public Route route = Route.All;
        public string outFolder = "production/qa/evidence/bot-run";
        public float timeScale = 2f;
        public bool steer = true;

        private readonly List<string> log = new List<string>();
        private int shot;
        private bool failed;

        /// <summary>Starts the bot on a route (call in play mode).</summary>
        public static PlaytestBot Launch(Route route, string outFolder)
        {
            var go = new GameObject("PlaytestBot");
            DontDestroyOnLoad(go);
            var bot = go.AddComponent<PlaytestBot>();
            bot.route = route;
            bot.outFolder = outFolder;
            return bot;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(outFolder);
            Time.timeScale = timeScale;
            if (route == Route.SecretEnding || route == Route.All) yield return Run("Route 1: boss win -> Secret Ending (Story)", SecretEnding());
            if (route == Route.SecretBossFromLevelSelect || route == Route.All) yield return Run("Route 3: secret boss from Level Select (Freeplay)", SecretBoss());
            if (route == Route.FreeplayMap || route == Route.All) yield return Run("Route 2: Freeplay map to the end -> Level Select", FreeplayMap());
            if (route == Route.Screens) yield return Run("Screens", Screens());
            if (route == Route.BossProbe) yield return Run("Boss probe (steer " + steer + ")", BossProbe());
            Time.timeScale = 1f;
            Log(failed ? "RESULT: FAIL" : "RESULT: PASS");
            File.WriteAllLines(Path.Combine(outFolder, "bot-log.txt"), log);
            Destroy(gameObject);
        }

        private IEnumerator Run(string name, IEnumerator body)
        {
            Log("== " + name);
            while (true)
            {
                object cur;
                try
                {
                    if (!body.MoveNext()) break;
                    cur = body.Current;
                }
                catch (System.Exception e)
                {
                    Fail("exception: " + e.Message);
                    break;
                }
                yield return cur;
            }
        }

        // ------------------------------------------------------------------ routes

        private IEnumerator SecretEnding()
        {
            var state = Find<GameModeState>("Assets/Data/Story/GameModeState.asset");
            state.BeginStory();
            state.SetCutscene(CutsceneId.TrueIntro); // the shop hands over here when all four Shards are in
            SceneManager.LoadScene(state.cutsceneScene);
            yield return WaitScene("Cutscene", 10f);
            yield return Shot("r1-true-intro");
            yield return TapThroughCutscene("Boss_Tempest", 60f);
            yield return WaitScene("Boss_Tempest", 15f);
            yield return BeatBoss("r1");
            yield return ClickUntilScene("ResultButton", "Cutscene", 20f);
            yield return Wait(1.5f);
            yield return Shot("r1-true-ending");
            yield return TapThroughCutscene(null, 60f, () => Active("MenuButton"));
            if (!Active("MenuButton")) { Fail("Story Complete screen did not show"); yield break; }
            yield return Wait(1f);
            yield return Shot("r1-story-complete");
            var progress = Find<ProgressService>("Assets/Data/ProgressService.asset");
            Check(progress.Data.secretBossUnlocked, "secret boss unlocked after the Secret Ending");
            Check(progress.Data.secretStars > 0, "secret stars saved (" + progress.Data.secretStars + ")");
            yield return ClickUntilScene("MenuButton", "Title", 15f);
            Check(SceneManager.GetActiveScene().name == "Title", "Menu returns to Title");
        }

        private IEnumerator SecretBoss()
        {
            yield return FromTitleToLevelSelect();
            yield return Shot("r3-level-select");
            Click("Secret"); // opens Tempest's details panel
            yield return Wait(0.5f);
            yield return Shot("r3-secret-details");
            yield return ClickUntilScene("StartButton", "Boss_Tempest", 10f);
            yield return WaitScene("Boss_Tempest", 15f);
            yield return BeatBoss("r3");
            yield return ClickUntilScene("ResultButton", "LevelSelect", 20f);
            Check(SceneManager.GetActiveScene().name == "LevelSelect", "secret boss Continue returns to Level Select");
            yield return Wait(1f);
            yield return Shot("r3-back-at-level-select");
        }

        private IEnumerator FreeplayMap()
        {
            yield return FromTitleToLevelSelect();
            var progress = Find<ProgressService>("Assets/Data/ProgressService.asset");
            int before = progress.Data.BestStars("rainforest");
            Click("Map_rainforest");
            yield return Wait(0.5f);
            yield return Shot("r2-details");
            yield return ClickUntilScene("StartButton", "Obby_Rainforest", 10f);
            yield return WaitScene("Obby_Rainforest", 20f);
            yield return Wait(2f);
            // Shortcut: Sam is put on the finish (the route under test is what comes after the course).
            var finish = FindAnyObjectByType<FinishTrigger>();
            var player = FindAnyObjectByType<PlayerController>();
            if (finish == null || player == null) { Fail("no finish or player in Obby_Rainforest"); yield break; }
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = finish.transform.position;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            Log("put Sam on the finish");
            var suction = FindAnyObjectByType<SuctionMinigame>();
            float t = 0f;
            while (suction != null && suction.Phase != SuctionPhase.Collecting && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            if (suction == null || suction.Phase != SuctionPhase.Collecting) { Fail("suction did not start"); yield break; }
            yield return Shot("r2-suction");
            int presses = 0;
            t = 0f;
            while (suction.Phase == SuctionPhase.Collecting && t < 60f)
            {
                yield return PressKey(suction.CurrentKey);
                presses++;
                yield return new WaitForSecondsRealtime(0.07f);
                t += 0.1f;
            }
            Log("suction filled after " + presses + " key presses");
            yield return ClickUntil("ContinueButton", () => suction.Phase != SuctionPhase.Collected, 15f);
            yield return Wait(1f);
            yield return Shot("r2-complete");
            yield return ClickUntilScene("NextButton", "LevelSelect", 15f);
            Check(SceneManager.GetActiveScene().name == "LevelSelect", "Freeplay map end returns to Level Select");
            yield return Wait(1f);
            Check(progress.Data.BestStars("rainforest") >= Mathf.Max(1, before), "rainforest stars saved (" + progress.Data.BestStars("rainforest") + ")");
            yield return Shot("r2-back-at-level-select");
        }

        private IEnumerator FromTitleToLevelSelect()
        {
            if (SceneManager.GetActiveScene().name != "Title") SceneManager.LoadScene("Title");
            yield return WaitScene("Title", 10f);
            yield return Wait(0.5f);
            Click("StartButton");
            yield return Wait(0.5f);
            yield return ClickUntilScene("StageButton", "LevelSelect", 10f);
            yield return WaitScene("LevelSelect", 10f);
            yield return Wait(0.5f);
        }

        // Screenshots of the front-end screens (Mode Select, Level Select, locked hint, details, a cutscene still).
        private IEnumerator Screens()
        {
            if (SceneManager.GetActiveScene().name != "Title") SceneManager.LoadScene("Title");
            yield return WaitScene("Title", 10f);
            yield return Wait(0.5f);
            Click("StartButton");
            yield return Wait(0.6f);
            yield return Shot("mode-select");
            yield return ClickUntilScene("StageButton", "LevelSelect", 10f);
            yield return Wait(0.6f);
            yield return Shot("level-select");
            Click("Secret");
            yield return Wait(0.3f);
            yield return Shot("secret-pressed");
            if (SceneManager.GetActiveScene().name != "LevelSelect") yield break;
            Click("Map_desert");
            yield return Wait(0.5f);
            yield return Shot("details");
            var state = Find<GameModeState>("Assets/Data/Story/GameModeState.asset");
            state.BeginStory();
            state.SetCutscene(CutsceneId.Opening);
            SceneManager.LoadScene(state.cutsceneScene);
            yield return WaitScene("Cutscene", 10f);
            for (int i = 0; i < 8; i++) { yield return PressKey(Key.Space); yield return Wait(0.25f); }
            yield return Wait(1.5f);
            yield return Shot("opening-note-with-portrait");
        }

        // Watches Sam and the boss twice a second for one attempt (to see how Sam is lost).
        private IEnumerator BossProbe()
        {
            SceneManager.LoadScene("Boss_Tempest");
            yield return WaitScene("Boss_Tempest", 15f);
            Time.timeScale = 1f;
            var fight = FindAnyObjectByType<BossFightController>();
            var suction = FindAnyObjectByType<BossSuction>();
            var player = FindAnyObjectByType<PlayerController>();
            var boss = FindAnyObjectByType<TempestBoss>();
            yield return Wait(1f);
            Bounds bb = new Bounds(boss.transform.position, Vector3.zero);
            foreach (Renderer r in boss.GetComponentsInChildren<Renderer>()) bb.Encapsulate(r.bounds);
            Log("boss bounds centre y=" + (bb.center.y - fight.Center.y).ToString("0.0") + " size=" + bb.size + " pivot y=" + (boss.transform.position.y - fight.Center.y).ToString("0.0")
                + " camera=" + Camera.main.transform.position + " fov=" + Camera.main.fieldOfView);
            float next = 0f;
            while (fight.Result == null && Time.unscaledTime < 600f)
            {
                if (Time.unscaledTime >= next)
                {
                    next = Time.unscaledTime + 0.5f;
                    Vector3 p = player.transform.position - fight.Center;
                    Log("Sam r=" + new Vector2(p.x, p.z).magnitude.ToString("0.0") + " y=" + p.y.ToString("0.0") + " ctrl=" + player.ControlEnabled
                        + " move=" + player.MoveInput + " boss=" + boss.State + " bossY=" + (boss.transform.position.y - fight.Center.y).ToString("0.0"));
                }
                if (suction.IsActive) { yield return PressKey(suction.CurrentKey); yield return new WaitForSecondsRealtime(0.04f); }
                else { if (steer) HoldMoveKeys(fight, player); yield return null; }
            }
            ReleaseKeys();
            Log("probe result " + fight.Result);
        }

        // ------------------------------------------------------------------ boss

        private IEnumerator BeatBoss(string tag)
        {
            Time.timeScale = 1f; // real speed: the suction window is timed like a player's mashing
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                yield return Wait(0.5f);
                var fight = FindAnyObjectByType<BossFightController>();
                var suction = FindAnyObjectByType<BossSuction>();
                var player = FindAnyObjectByType<PlayerController>();
                var boss = FindAnyObjectByType<TempestBoss>();
                if (fight == null || suction == null || player == null || boss == null) { Fail("boss arena parts missing"); yield break; }
                bool shotSuction = false, wasActive = false;
                float peak = 0f;
                int presses = 0, rescues = 0;
                while (fight.Result == null)
                {
                    if (suction.IsActive)
                    {
                        wasActive = true;
                        if (!shotSuction) { shotSuction = true; yield return Shot(tag + "-boss-suction"); }
                        yield return PressKey(suction.CurrentKey);
                        presses++;
                        peak = Mathf.Max(peak, suction.Fill);
                        yield return new WaitForSecondsRealtime(0.04f); // about 12 presses a second
                    }
                    else
                    {
                        if (wasActive)
                        {
                            wasActive = false;
                            Log("suction window over: peak fill " + peak.ToString("0.00") + ", boss HP " + boss.Hp + "/" + boss.MaxHp);
                            peak = 0f;
                        }
                        HoldMoveKeys(fight, player);
                        if (CatchIfThrown(fight, player)) rescues++;
                        yield return null;
                    }
                }
                ReleaseKeys();
                if (rescues > 0) Log("safety net: Sam put back on the arena " + rescues + " time(s) after being thrown off (the bot cannot read the attack warnings)");
                Log("boss attempt " + attempt + ": " + fight.Result + " (" + presses + " key presses, HP " + boss.Hp + ", " + Mathf.RoundToInt(fight.TimeRemaining) + " s left)");
                if (fight.Result == BossResult.Won)
                {
                    yield return Wait(0.8f);
                    yield return Shot(tag + "-boss-sealed");
                    Time.timeScale = timeScale;
                    yield break;
                }
                yield return ClickUntilScene("ResultButton", "Boss_Tempest", 10f, reload: true); // Try Again reloads the arena
            }
            Time.timeScale = timeScale;
            Fail("boss not beaten in 4 attempts");
        }

        // Walks back toward the middle with W/A/S/D (camera relative) when Sam drifts past a third of the radius.
        private static void HoldMoveKeys(BossFightController fight, PlayerController player)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            Vector3 to = fight.Center - player.transform.position;
            to.y = 0f;
            var keys = new List<Key>();
            if (to.magnitude > fight.Radius * 0.3f)
            {
                Camera cam = Camera.main;
                Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
                Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
                to.Normalize();
                float x = Vector3.Dot(to, right), y = Vector3.Dot(to, fwd);
                if (y > 0.38f) keys.Add(Key.W);
                if (y < -0.38f) keys.Add(Key.S);
                if (x > 0.38f) keys.Add(Key.D);
                if (x < -0.38f) keys.Add(Key.A);
            }
            InputSystem.QueueStateEvent(kb, new KeyboardState(keys.ToArray()));
        }

        // Shortcut: a hit throws Sam far (no health, the edge is the danger). The bot does not dodge, so when Sam is
        // flying out past the edge it puts him back near the middle, the way a player who dodged would still be there.
        private static bool CatchIfThrown(BossFightController fight, PlayerController player)
        {
            Vector3 rel = player.transform.position - fight.Center;
            if (new Vector2(rel.x, rel.z).magnitude < fight.Radius * 0.85f && rel.y > -1f) return false;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = fight.Center + new Vector3(0f, 0.3f, -fight.Radius * 0.35f);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            return true;
        }

        private static void ReleaseKeys()
        {
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        }

        // ------------------------------------------------------------------ input

        private IEnumerator TapThroughCutscene(string untilScene, float timeout, System.Func<bool> until = null)
        {
            string start = SceneManager.GetActiveScene().name;
            int taps = 0;
            float t = 0f;
            while (t < timeout)
            {
                if (untilScene != null && SceneManager.GetActiveScene().name == untilScene) break;
                if (until != null && until()) break;
                yield return PressKey(Key.Space);
                taps++;
                yield return new WaitForSecondsRealtime(0.25f);
                t += 0.3f;
            }
            Log("tapped Space " + taps + " times in " + start);
        }

        private static IEnumerator PressKey(Key key)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || key == Key.None) yield break;
            InputSystem.QueueStateEvent(kb, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;
        }

        private bool Click(string name)
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (b.name != name || !b.isActiveAndEnabled || !b.interactable) continue;
                var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(b.gameObject, data, ExecuteEvents.pointerClickHandler);
                Log("click " + name + " in " + SceneManager.GetActiveScene().name);
                return true;
            }
            return false;
        }

        private static bool Active(string name)
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsSortMode.None)) if (b.name == name && b.isActiveAndEnabled) return true;
            return false;
        }

        private IEnumerator ClickUntilScene(string button, string scene, float timeout, bool reload = false)
        {
            int handle = SceneManager.GetActiveScene().handle;
            yield return ClickUntil(button, () => SceneManager.GetActiveScene().name == scene && (!reload || SceneManager.GetActiveScene().handle != handle), timeout);
            if (SceneManager.GetActiveScene().name != scene) Fail(button + " did not lead to " + scene);
            yield return Wait(0.5f);
        }

        // Panels ignore presses for a short delay after they open, so keep clicking until something happens.
        private IEnumerator ClickUntil(string button, System.Func<bool> done, float timeout)
        {
            float t = 0f;
            while (!done() && t < timeout)
            {
                Click(button);
                yield return new WaitForSecondsRealtime(0.5f);
                t += 0.5f;
            }
        }

        // ------------------------------------------------------------------ helpers

        private IEnumerator WaitScene(string scene, float timeout)
        {
            float t = 0f;
            while (SceneManager.GetActiveScene().name != scene && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            if (SceneManager.GetActiveScene().name != scene) Fail("scene " + scene + " did not load");
            else Log("in " + scene);
        }

        private static IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(outFolder, (++shot).ToString("00") + "-" + name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Log("screenshot " + path);
        }

        private static T Find<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

        private void Check(bool ok, string what)
        {
            if (ok) Log("OK   " + what);
            else Fail(what);
        }

        private void Fail(string what)
        {
            failed = true;
            Log("FAIL " + what);
        }

        private void Log(string line)
        {
            line = "[" + Time.realtimeSinceStartup.ToString("0.0") + "] " + line;
            log.Add(line);
            Debug.Log("[PlaytestBot] " + line);
            File.WriteAllLines(Path.Combine(outFolder, "bot-log.txt"), log);
        }
    }
}
#endif
