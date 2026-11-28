using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeatherForSale.Obby;

namespace WeatherForSale.Boss
{
    /// <summary>How the boss fight ended.</summary>
    public enum BossResult
    {
        /// <summary>The boss is sealed in the purple bottle.</summary>
        Won,
        /// <summary>Player HP reached 0.</summary>
        PlayerDown,
        /// <summary>The time limit ran out.</summary>
        TimeUp,
    }

    /// <summary>
    /// Runs one Tempest fight in the boss arena: the time limit, the throws (a hit throws Sam back; there is no health: falling off the
    /// arena ends the fight), the win and lose results, and the shapes the attack patterns spawn (it is their <see cref="IBossArena"/>).
    /// The same scene serves Story Mode (story 006) and Freeplay (story 007): they listen to
    /// <see cref="Finished"/> and <see cref="ContinueRequested"/> and decide where to go next; with no
    /// listener the Continue button simply reloads the arena.
    /// Implements: story-008 criteria 1, 3, 4, 7, 8 and the Implementation Notes (result by event).
    /// </summary>
    public sealed class BossFightController : MonoBehaviour, IBossArena
    {
        [SerializeField] private BossConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private TempestBoss boss;
        [SerializeField] private BossSky sky;
        [SerializeField, Tooltip("Floor centre; its position is the middle of the arena floor surface.")] private Transform arenaCenter;
        [SerializeField, Tooltip("Where the player starts and returns to after falling off.")] private Transform playerStart;
        [SerializeField, Tooltip("The Storm Crystal prefab dropped after a suction in the later phases.")] private GameObject crystalPrefab;
        [SerializeField] private BossSuction suction;
        [SerializeField, Tooltip("Purple bottle on the floor; hidden until the boss is drawn in.")] private GameObject bottle;
        [SerializeField, Tooltip("Start the fight when the scene starts. Off when a caller starts it with StartFight.")] private bool autoStart = true;

        private readonly List<GameObject> shapes = new List<GameObject>();
        private readonly List<Coroutine> routines = new List<Coroutine>();
        private Material shapeMaterial;
        private bool running;
        private bool finished;
        private float immuneUntil;
        private GameObject crystal;

        /// <summary>Seconds left on the clock.</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>Seconds the fight has taken so far (the secret stars are judged on it).</summary>
        public float SecondsUsed => config != null ? config.fightSeconds - TimeRemaining : 0f;

        /// <summary>Secret stars (1-3) this fight earns; 0 unless won.</summary>
        public int SecretStars => Result == BossResult.Won && config != null ? config.SecretStarsFor(SecondsUsed) : 0;

        /// <summary>The result, or null while the fight is on.</summary>
        public BossResult? Result { get; private set; }

        /// <summary>Raised once with the result (after the win animation for <see cref="BossResult.Won"/>).</summary>
        public event Action<BossResult> Finished;

        /// <summary>Raised when the player presses Continue on the victory panel. No listener: the arena reloads.</summary>
        public event Action ContinueRequested;

        /// <summary>Raised when Sam is thrown by a hit.</summary>
        public event Action PlayerHit;

        /// <summary>Raised when the Storm Crystal is picked up.</summary>
        public event Action CrystalCollected;

        /// <summary>Raised on every <see cref="Flash"/>.</summary>
        public event Action Flashed;

        /// <summary>The config in use.</summary>
        public BossConfig Config => config;

        /// <inheritdoc />
        public Vector3 Center => arenaCenter != null ? arenaCenter.position : Vector3.zero;

        /// <inheritdoc />
        public float Radius => config != null ? config.arenaRadius : 14f;

        /// <inheritdoc />
        public Transform Player => player != null ? player.transform : null;

        /// <inheritdoc />
        public float WarningHeight => 0.12f;

        /// <inheritdoc />
        public Transform Boss => boss != null ? boss.transform : null;

        /// <inheritdoc />
        public float BossRestHeight => boss != null ? boss.RestHeight : (config != null ? config.bossHoverHeight : 0f);

        /// <inheritdoc />
        public void SetBossHeight(float centreHeight)
        {
            if (boss != null) boss.SetTargetHeight(centreHeight);
        }

        /// <inheritdoc />
        public void SetBossMoving(bool moving)
        {
            if (boss != null) boss.attackMovesBoss = moving;
        }

        private void Awake()
        {
            shapeMaterial = new Material(Shader.Find("Sprites/Default"));
            if (bottle != null) bottle.SetActive(false);
            TimeRemaining = config.fightSeconds;
        }

        private void OnEnable()
        {
            boss.DefeatStarted += OnBossDefeatStarted;
            boss.Defeated += OnBossDefeated;
            boss.ItemDropped += OnItemDropped;
        }

        private void OnDisable()
        {
            boss.DefeatStarted -= OnBossDefeatStarted;
            boss.Defeated -= OnBossDefeated;
            boss.ItemDropped -= OnItemDropped;
        }

        private void Start()
        {
            boss.Initialize(this);
            if (autoStart) StartFight();
        }

        /// <summary>Starts the clock and the boss. Ignored when already running or over.</summary>
        public void StartFight()
        {
            if (running || finished) return;
            running = true;
            boss.Begin();
        }

        private void Update()
        {
            if (!running) return;
            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f)
            {
                Finish(BossResult.TimeUp);
                return;
            }

            // No wall and no health: Sam is lost when he falls off the edge of the arena.
            if (player.transform.position.y < Center.y - config.fallDeathDepth) Finish(BossResult.PlayerDown);
        }

        /// <summary>Reloads the arena (Game Over Try Again).</summary>
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Victory panel Continue: tells the listener, or reloads when nobody listens.</summary>
        public void Continue()
        {
            if (ContinueRequested != null) ContinueRequested.Invoke();
            else Restart();
        }

        // ------------------------------------------------------------------ results

        private void OnItemDropped()
        {
            if (crystalPrefab == null || suction == null) return;
            ClearCrystal();
            Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(config.itemDropRadius * 0.6f, config.itemDropRadius);
            Vector3 spot = Center + new Vector3(ring.x, 1.3f, ring.y);
            crystal = Instantiate(crystalPrefab, spot, Quaternion.identity, transform);
            var item = crystal.GetComponent<SuctionBoostItem>();
            if (item != null) item.Collected += OnCrystalCollected;
        }

        private void OnCrystalCollected()
        {
            suction.Boost();
            CrystalCollected?.Invoke();
        }

        private void ClearCrystal()
        {
            if (crystal != null) Destroy(crystal);
            crystal = null;
        }

        private void OnBossDefeatStarted()
        {
            ClearShapes();
            ClearCrystal();
        }

        private void OnBossDefeated()
        {
            StartCoroutine(WinSequence());
        }

        private IEnumerator WinSequence()
        {
            running = false;
            player.ControlEnabled = false;
            yield return new WaitForSeconds(0.6f); // the bottle takes the last of the storm
            if (suction != null) suction.ReleasePose();
            player.ControlEnabled = false;
            if (sky != null)
            {
                sky.Calm();
                // The rainbow rises in front of Sam, the way he faces.
                if (config.rainbowOnWin) sky.ShowRainbow(player.transform.position, player.transform.forward);
            }
            yield return new WaitForSeconds(3f);
            Result = BossResult.Won;
            finished = true;
            Finished?.Invoke(BossResult.Won);
        }

        private IEnumerator GrowBottle()
        {
            bottle.SetActive(true);
            Vector3 full = bottle.transform.localScale;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                bottle.transform.localScale = full * Mathf.SmoothStep(0.01f, 1f, t / 0.6f);
                yield return null;
            }
            bottle.transform.localScale = full;
        }

        private void Finish(BossResult result)
        {
            if (finished) return;
            finished = true;
            running = false;
            Result = result;
            boss.Stop();
            StopAllRoutines();
            ClearShapes();
            ClearCrystal();
            player.ControlEnabled = false;
            Finished?.Invoke(result);
        }

        // ------------------------------------------------------------------ IBossArena

        /// <inheritdoc />
        public Coroutine StartRoutine(IEnumerator routine)
        {
            Coroutine c = StartCoroutine(routine);
            routines.Add(c);
            return c;
        }

        /// <inheritdoc />
        public GameObject SpawnDisc(Vector3 center, float radius, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BossDisc";
            go.GetComponent<MeshFilter>().sharedMesh = BossShapes.Disc; // round, not the cylinder's few-sided look
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            return Track(go, color);
        }

        /// <inheritdoc />
        public GameObject SpawnBox(Vector3 center, Vector3 size, Quaternion rotation, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BossBox";
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(center, rotation);
            go.transform.localScale = size;
            return Track(go, color);
        }

        /// <inheritdoc />
        public void Release(GameObject shape)
        {
            if (shape == null) return;
            shapes.Remove(shape);
            Destroy(shape);
        }

        /// <inheritdoc />
        public void HitPlayer(Vector3 pushDirection, float pushSpeed)
        {
            if (!running || Time.time < immuneUntil) return;
            immuneUntil = Time.time + config.hitImmuneSeconds;
            PlayerHit?.Invoke();

            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude > 0.0001f && pushSpeed > 0f)
            {
                player.Launch(pushDirection.normalized * pushSpeed + Vector3.up * (pushSpeed * 0.3f));
            }
        }

        /// <inheritdoc />
        public GameObject SpawnMesh(Mesh mesh, Vector3 position, Color color)
        {
            var go = new GameObject("BossMeshShape", typeof(MeshFilter), typeof(MeshRenderer), typeof(OwnedMesh));
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<OwnedMesh>().mesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = shapeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            shapes.Add(go);
            return go;
        }

        /// <inheritdoc />
        public void Flash()
        {
            if (sky != null) sky.Flash();
            Flashed?.Invoke();
        }

        private GameObject Track(GameObject go, Color color)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col); // a shape only warns or hurts through code; it must never block the player
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = shapeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            shapes.Add(go);
            return go;
        }

        private void StopAllRoutines()
        {
            foreach (Coroutine c in routines)
            {
                if (c != null) StopCoroutine(c);
            }
            routines.Clear();
        }

        private void ClearShapes()
        {
            foreach (GameObject go in shapes)
            {
                if (go != null) Destroy(go);
            }
            shapes.Clear();
        }
    }
}

    namespace WeatherForSale.Boss
    {
        /// <summary>Destroys the procedural mesh of a shape together with the shape.</summary>
        public sealed class OwnedMesh : MonoBehaviour
        {
            /// <summary>The mesh to destroy.</summary>
            public Mesh mesh;

            private void OnDestroy()
            {
                if (mesh != null) Destroy(mesh);
            }
        }
    }
