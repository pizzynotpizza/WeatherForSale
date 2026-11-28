using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WeatherForSale.Freeplay;
using WeatherForSale.Progress;
using WeatherForSale.Shop;
using WeatherForSale.Story;
using WeatherForSale.Suction;
using WeatherForSale.UI;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Builds the front-end scenes of stories 006 and 007: Title (game cover, Start, Settings bottom-left,
    /// Achievement book bottom-right, Mode Select, Options), Cutscene (stills + subtitles, Skip, Story
    /// Complete) and LevelSelect (Freeplay maps around the secret "???" slot). Creates the GameModeState,
    /// FrontEndStrings and four CutsceneData assets when missing (their text is kept once edited), and puts
    /// Title first in Build Settings. Cutscene pictures are picked up by file name from
    /// Assets/Art/UI/Story/Cutscenes (e.g. Opening_1.png); a slide without one shows its colour.
    /// </summary>
    public static class FrontEndSceneBuilder
    {
        private const string Sheet = "Assets/Art/UI/Sheet/";
        private const string StoryArt = "Assets/Art/UI/Story/";
        private const string CutsceneArt = StoryArt + "Cutscenes/";
        private const string DataFolder = "Assets/Data/Story";
        internal const string ModeStatePath = DataFolder + "/GameModeState.asset";
        private const string StringsPath = DataFolder + "/FrontEndStrings.asset";
        private const string TitleScene = "Assets/Scenes/Title.unity";
        private const string CutsceneScene = "Assets/Scenes/Cutscene.unity";
        private const string LevelSelectScene = "Assets/Scenes/LevelSelect.unity";

        private static readonly Color DarkBrown = new Color(0.33f, 0.2f, 0.12f);
        private static readonly Color Outline = new Color(0.2f, 0.12f, 0.06f, 0.9f);

        [MenuItem("Weather For Sale/Build Front End Scenes (Title, Cutscene, Level Select)")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        /// <summary>Batch-mode / script entry: builds all three scenes.</summary>
        public static void BuildFromCommandLine() => BuildAll();

        private static void BuildAll()
        {
            ImportArt();
            BuildTitle();
            BuildCutscene();
            BuildLevelSelect();
            FixBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[FrontEndSceneBuilder] Built Title, Cutscene and LevelSelect.");
        }

        // ------------------------------------------------------------------ assets

        /// <summary>The shared Story / Freeplay state asset (created when missing).</summary>
        internal static GameModeState GetOrCreateModeState() => GetOrCreate<GameModeState>(ModeStatePath);

        /// <summary>The front-end text asset (created when missing).</summary>
        internal static FrontEndStrings GetOrCreateStrings()
        {
            FrontEndStrings strings = GetOrCreate<FrontEndStrings>(StringsPath);
            if (strings.secretLockedHint == "See the True Ending to unlock" || strings.secretLockedHint == "See the Secret Ending to unlock")
                strings.secretLockedHint = "Get the Secret Ending to unlock";
            if (strings.secretUnlockedLabel == "Tempest") strings.secretUnlockedLabel = "Secret Boss";
            EditorUtility.SetDirty(strings);
            return strings;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            ObbyTestSceneBuilder.EnsureFolder("Assets/Data");
            ObbyTestSceneBuilder.EnsureFolder(DataFolder);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ImportArt()
        {
            ObbyTestSceneBuilder.EnsureFolder("Assets/Art/UI/Story");
            ObbyTestSceneBuilder.EnsureFolder("Assets/Art/UI/Story/Cutscenes");
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI/Story" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static Sprite S(string sheetName) => AssetDatabase.LoadAssetAtPath<Sprite>(Sheet + sheetName + ".png");
        private static Sprite Story(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(StoryArt + file);

        // ------------------------------------------------------------------ Title

        private static void BuildTitle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera cam = NewCamera(new Color(0.55f, 0.78f, 0.98f));
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform c = NewCanvas("TitleCanvas");
            FrontEndStrings strings = GetOrCreateStrings();
            var shopStrings = AssetDatabase.LoadAssetAtPath<ShopStrings>("Assets/Data/Shop/ShopStrings.asset");

            // Cover (the logo is part of the picture).
            RectTransform cover = Rect("Cover", c);
            Stretch(cover);
            Img(cover, Story("Cover.jpg"), Color.white, false);

            Button start = SpriteButton("StartButton", c, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Button_Start.png"),
                new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(440f, 170f)); // below the cover's crystal ball
            Button titleQuit = SpriteButton("TitleQuitButton", c, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Button_Quit.png"),
                new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(380f, 140f)); // ~58 px above the bottom edge
            Button settings = RoundIconButton("SettingsButton", c, S("IconGear"), new Vector2(0f, 0f), new Vector2(110f, 110f));
            Button achievements = RoundIconButton("AchievementButton", c, S("IconBook"), new Vector2(1f, 0f), new Vector2(-110f, 110f));

            // --- Mode Select
            RectTransform modeRoot = Overlay("ModeSelect", c);
            // Two picture cards (Story: Sam and Uncle John at the shop; Stage: the four maps), the mode name on a capsule.
            RectTransform modeCard = Rect("Cards", modeRoot);
            Place(modeCard, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1200f, 900f));
            Text modeTitle = Header(modeCard, font, new Vector2(0f, 410f)); // bottom edge just above the cards
            Button story = ModeCardButton("StoryButton", modeCard, Story("ModeCard_Story.png"), font, new Vector2(-290f, -70f), out Text storyLabel);
            Button stage = ModeCardButton("StageButton", modeCard, Story("ModeCard_Stage.png"), font, new Vector2(290f, -70f), out Text stageLabel);
            CanvasGroup stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
            RectTransform stageLock = Rect("Lock", stage.transform);
            Place(stageLock, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(150f, 150f)); // over the "?" cloud
            Img(stageLock, S("IconLock"), Color.white, false).preserveAspect = true;
            Text stageLocked = Txt("LockedText", modeCard, font, "", 34, TextAnchor.MiddleCenter, Color.white);
            Place(stageLocked.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(290f, -458f), new Vector2(620f, 50f)); // under the Stage card
            stageLocked.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            Button modeClose = IconButton("CloseButton", modeCard, S("IconClose"), new Vector2(560f, 300f), new Vector2(100f, 100f)); // above the Stage card's top-right corner
            modeRoot.gameObject.SetActive(false);

            // --- Options
            RectTransform optionRoot = Overlay("Options", c);
            RectTransform optionCard = Card(optionRoot, Vector2.zero, new Vector2(980f, 760f));
            Text optionTitle = Txt("Title", optionCard, font, "", 72, TextAnchor.MiddleCenter, DarkBrown);
            optionTitle.fontStyle = FontStyle.Bold;
            Place(optionTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 185f), new Vector2(800f, 100f));
            Slider all = VolumeRow("All", optionCard, font, 85f, out Text allLabel);
            Slider music = VolumeRow("Music", optionCard, font, -20f, out Text musicLabel);
            Slider sfx = VolumeRow("SFX", optionCard, font, -125f, out Text sfxLabel);
            // Only Back, centred (the Title screen has its own Quit button).
            Button back = TextButton("BackButton", optionCard, S("BtnBlueA"), font, new Vector2(0f, -240f), new Vector2(340f, 110f), Color.white, out Text backLabel);
            optionRoot.gameObject.SetActive(false);

            AchievementBookPanel book = BuildBook(c, font, shopStrings);

            var title = new GameObject("TitleScreen").AddComponent<TitleScreen>();
            SetRefs(title,
                ("state", GetOrCreateModeState()),
                ("session", AssetDatabase.LoadAssetAtPath<ShopSession>("Assets/Data/Shop/ShopSession.asset")),
                ("progress", AssetDatabase.LoadAssetAtPath<ProgressService>("Assets/Data/ProgressService.asset")),
                ("audioSettings", AssetDatabase.LoadAssetAtPath<ShopAudioSettings>("Assets/Data/Shop/ShopAudioSettings.asset")),
                ("strings", strings),
                ("startButton", start), ("titleQuitButton", titleQuit), ("settingsButton", settings), ("achievementButton", achievements), ("achievementBook", book),
                ("modeRoot", modeRoot.gameObject), ("modeTitle", modeTitle), ("storyButton", story), ("storyLabel", storyLabel),
                ("stageButton", stage), ("stageLabel", stageLabel), ("stageGroup", stageGroup), ("stageCardImage", stage.targetGraphic), ("stageLockIcon", stageLock.gameObject), ("stageLockedText", stageLocked), ("modeCloseButton", modeClose),
                ("optionRoot", optionRoot.gameObject), ("optionTitle", optionTitle), ("allSlider", all), ("musicSlider", music), ("sfxSlider", sfx),
                ("allLabel", allLabel), ("musicLabel", musicLabel), ("sfxLabel", sfxLabel),
                ("optionBackButton", back), ("optionBackLabel", backLabel));
            // Labels filled now too, so the scene reads right in the editor.
            storyLabel.text = strings.storyModeLabel; stageLabel.text = strings.stageModeLabel; modeTitle.text = strings.modeTitle;
            stageLocked.text = strings.stageLockedText; optionTitle.text = strings.optionTitle; backLabel.text = strings.backLabel;

            NewEventSystem();
            Save(scene, TitleScene);
        }

        private static AchievementBookPanel BuildBook(RectTransform c, Font font, ShopStrings strings)
        {
            RectTransform host = Rect("AchievementBook", c);
            Stretch(host);
            var panel = host.gameObject.AddComponent<AchievementBookPanel>();
            RectTransform root = Overlay("BookRoot", host);
            RectTransform book = Rect("Book", root);
            Place(book, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 753f));
            Img(book, S("BookOpen"), Color.white, true).preserveAspect = true;
            Text title = Txt("Title", book, font, strings != null ? strings.achievementsTitle : "Achievements", 64, TextAnchor.MiddleCenter, DarkBrown);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-290f, 190f), new Vector2(480f, 100f));
            RectTransform trophy = Rect("Trophy", book);
            Place(trophy, new Vector2(0.5f, 0.5f), new Vector2(-290f, -50f), new Vector2(220f, 220f));
            Img(trophy, S("IconTrophy"), Color.white, false).preserveAspect = true;
            var labels = new Object[4];
            var locks = new Object[4];
            AchievementId[] ids = { AchievementId.Die10Times, AchievementId.Earn12Stars, AchievementId.NormalEnding, AchievementId.TrueEnding };
            for (int i = 0; i < 4; i++)
            {
                float y = 200f - i * 105f;
                RectTransform lockRt = Rect("Lock_" + i, book);
                Place(lockRt, new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(72f, 72f));
                Img(lockRt, S("IconLock"), Color.white, false).preserveAspect = true;
                Text row = Txt("Row_" + i, book, font, strings != null ? strings.AchievementLabel(ids[i]) : ids[i].ToString(), 44, TextAnchor.MiddleLeft,
                    new Color(DarkBrown.r, DarkBrown.g, DarkBrown.b, 0.55f));
                Place(row.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(350f, y), new Vector2(380f, 80f));
                labels[i] = row;
                locks[i] = lockRt.gameObject;
            }
            Button close = IconButton("CloseButton", book, S("IconClose"), new Vector2(650f, 330f), new Vector2(100f, 100f));
            SetRefs(panel, ("strings", strings), ("root", root.gameObject), ("titleText", title), ("closeButton", close));
            SetArray(panel, "rowLabels", labels);
            SetArray(panel, "rowLocks", locks);
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------ Cutscene

        private static void BuildCutscene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera(Color.black);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform c = NewCanvas("CutsceneCanvas");
            FrontEndStrings strings = GetOrCreateStrings();

            RectTransform bgHost = Rect("Picture", c);
            Stretch(bgHost);
            bgHost.offsetMin = new Vector2(-30f, -30f); // a little overscan so the storm shake shows no edge
            bgHost.offsetMax = new Vector2(30f, 30f);
            Image bg = Img(bgHost, null, Color.black, false);

            RectTransform left = Rect("PortraitLeft", c);
            Place(left, new Vector2(0f, 0f), new Vector2(360f, 600f), new Vector2(600f, 600f)); // half body, its fade behind the text box
            Image leftImg = Img(left, null, Color.white, false);
            leftImg.raycastTarget = false;
            RectTransform right = Rect("PortraitRight", c);
            Place(right, new Vector2(1f, 0f), new Vector2(-360f, 600f), new Vector2(600f, 600f));
            Image rightImg = Img(right, null, Color.white, false);
            rightImg.raycastTarget = false;

            RectTransform box = Rect("TextBox", c);
            Place(box, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1620f, 290f));
            Img(box, S("DialogFrame"), Color.white, true).raycastTarget = false;
            Text speaker = Txt("Speaker", box, font, "", 44, TextAnchor.UpperLeft, DarkBrown);
            speaker.fontStyle = FontStyle.Bold;
            Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(360f, -78f), new Vector2(600f, 56f)); // inside the frame
            Text body = Txt("Body", box, font, "", 42, TextAnchor.UpperLeft, DarkBrown);
            Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(64f, 50f);
            body.rectTransform.offsetMax = new Vector2(-64f, -112f);
            Text hint = Txt("ContinueHint", box, font, "", 30, TextAnchor.LowerRight, new Color(DarkBrown.r, DarkBrown.g, DarkBrown.b, 0.6f));
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(70f, 40f);
            hint.rectTransform.offsetMax = new Vector2(-70f, -40f);

            Text big = Txt("BigText", c, font, "", 66, TextAnchor.MiddleCenter, Color.white);
            Stretch(big.rectTransform);
            big.rectTransform.offsetMin = new Vector2(160f, 0f);
            big.rectTransform.offsetMax = new Vector2(-160f, 0f);
            big.lineSpacing = 1.3f;
            big.fontStyle = FontStyle.Bold;
            big.gameObject.SetActive(false);

            RectTransform flashRt = Rect("Flash", c);
            Stretch(flashRt);
            Image flash = Img(flashRt, null, new Color(1f, 1f, 1f, 0f), false);
            flash.raycastTarget = false;

            // Skip: white text with a black outline only (no button picture).
            Text skipLabel = Txt("SkipButton", c, font, "Skip", 52, TextAnchor.MiddleRight, Color.white);
            skipLabel.fontStyle = FontStyle.Bold;
            skipLabel.raycastTarget = true;
            Place(skipLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-150f, -70f), new Vector2(220f, 80f));
            var skipOutline = skipLabel.gameObject.AddComponent<UnityEngine.UI.Outline>();
            skipOutline.effectColor = Color.black;
            skipOutline.effectDistance = new Vector2(3f, -3f);
            var skip = skipLabel.gameObject.AddComponent<Button>();
            skip.targetGraphic = skipLabel;

            // Story Complete (after the True Ending)
            RectTransform completeRoot = Overlay("StoryComplete", c);
            RectTransform card = Card(completeRoot, Vector2.zero, new Vector2(900f, 700f));
            Text completeTitle = Header(card, font, new Vector2(0f, 330f));
            Text starsValue = ResultRow(card, font, S("IconStar"), strings.starsLabel, 120f);
            Text secretValue = ResultRow(card, font, Story("StarSecret.png"), strings.secretStarsLabel, 10f);
            Text timeValue = ResultRow(card, font, S("IconStopwatch"), strings.timeLabel, -100f);
            Button menu = TextButton("MenuButton", card, S("BtnBlueA"), font, new Vector2(0f, -245f), new Vector2(380f, 116f), Color.white, out Text menuLabel);
            completeRoot.gameObject.SetActive(false);

            var player = new GameObject("CutscenePlayer").AddComponent<CutscenePlayer>();
            SetRefs(player,
                ("state", GetOrCreateModeState()),
                ("progress", AssetDatabase.LoadAssetAtPath<ProgressService>("Assets/Data/ProgressService.asset")),
                ("strings", strings),
                ("background", bg), ("backgroundRect", bgHost), ("portraitLeft", leftImg), ("portraitRight", rightImg),
                ("textBox", box.gameObject), ("speakerText", speaker), ("bodyText", body), ("bigText", big), ("continueHint", hint),
                ("flash", flash), ("skipButton", skip), ("skipLabel", skipLabel),
                ("completeRoot", completeRoot.gameObject), ("completeTitle", completeTitle), ("starsText", starsValue), ("secretStarsText", secretValue), ("timeText", timeValue),
                ("menuButton", menu), ("menuLabel", menuLabel));
            SetArray(player, "cutscenes", BuildCutsceneAssets());

            NewEventSystem();
            Save(scene, CutsceneScene);
        }

        private static Text ResultRow(RectTransform card, Font font, Sprite icon, string label, float y)
        {
            RectTransform iconRt = Rect("Icon_" + label, card);
            Place(iconRt, new Vector2(0.5f, 0.5f), new Vector2(-280f, y), new Vector2(96f, 96f));
            Img(iconRt, icon, Color.white, false).preserveAspect = true;
            Text name = Txt("Label_" + label, card, font, label, 52, TextAnchor.MiddleLeft, DarkBrown);
            name.fontStyle = FontStyle.Bold;
            Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-50f, y), new Vector2(340f, 90f));
            Text value = Txt("Value_" + label, card, font, "", 56, TextAnchor.MiddleRight, DarkBrown);
            value.fontStyle = FontStyle.Bold;
            Place(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(195f, y), new Vector2(260f, 90f)); // right edge 75 px inside the frame
            return value;
        }

        // Cutscene text and order (English, the game's language). Pictures: Assets/Art/UI/Story/Cutscenes/<Id>_<n>.png.
        private static readonly Color Dawn = new Color(1f, 0.85f, 0.65f), Day = new Color(0.62f, 0.82f, 0.98f),
            Shop = new Color(0.86f, 0.66f, 0.42f), Dusk = new Color(0.95f, 0.62f, 0.45f), Storm = new Color(0.25f, 0.18f, 0.35f),
            Grey = new Color(0.6f, 0.64f, 0.7f), Clear = new Color(0.55f, 0.85f, 1f);

        private static Object[] BuildCutsceneAssets()
        {
            // Talking characters are shown from the waist up.
            Sprite sam = Story("Portrait_Sam_Half.png"), john = Story("Portrait_UncleJohn_Half.png"), tempest = Story("Portrait_Tempest.png");
            var list = new List<Object>
            {
                Cutscene(CutsceneId.Opening, false, EndingKind.Normal, CutsceneNext.Shop, new[]
                {
                    Slide(Dawn, null, false, "", "Sunrise spills over the rooftops. Sam bolts awake. Today feels different, as if the sky itself is holding its breath."),
                    Slide(Dawn, sam, false, "Sam", "Red jacket? Check. Lucky goggles? Check. Whatever today throws at me, I'm ready for it!"),
                    Slide(Day, null, false, "", "Down the cobbled street to Uncle John's Weather for Sale, the only shop in town that sells sunshine by the bottle."),
                    Slide(Shop, sam, false, "Sam", "Uncle John? ...Hello? The lamps are lit, the bell rang... but nobody's here."),
                    Slide(Shop, john, true, "A note on the counter", "Sam, I'm away for the day. The shop is yours. Every customer will tell you the weather they need: find it, bottle it, bring it home. I'm counting on you! - Uncle John"),
                    Slide(Shop, sam, false, "Sam", "The whole shop... on my own? Okay. Deep breath. Today, I'm the keeper of the weather!"),
                }),
                // After Uncle John's lines in the shop.
                Cutscene(CutsceneId.NormalEnding, true, EndingKind.Normal, CutsceneNext.Title, new[]
                {
                    Slide(Dusk, john, true, "Uncle John", "Not one customer left empty-handed. Sam... you were born for this. Will you open up with me again tomorrow?"),
                    Slide(Dusk, sam, false, "Sam", "Every single day, Uncle John! Rain, snow, sun or wind - just say the word!"),
                    Slide(Grey, null, false, "", "But high above the rooftops, the sky stays a dull, heavy grey... as if something out there is still waiting."),
                    BigSlide("Normal Ending"),
                    BigSlide("There's something you missed...\n\nCollect all 4 Shards"),
                }),
                // After Uncle John's lines in the shop, with all four Shards.
                Cutscene(CutsceneId.TrueIntro, false, EndingKind.True, CutsceneNext.Boss, new[]
                {
                    Slide(Storm, null, false, "", "CRACK! Thunder splits the sky in two. In a single heartbeat, the whole town is swallowed by darkness.", flash: true, shake: true),
                    Slide(Storm, tempest, true, "Tempest", "WHO dares bottle MY weather?! Every drop of rain, every gust of wind - it all belongs to ME!", flash: true, shake: true),
                    Slide(Storm, null, false, "", "Rain, snow, scorching heat and howling wind tear through the streets. People run for cover as the storm goes wild.", shake: true),
                    Slide(Storm, null, false, "", "Then Sam's bag begins to glow. The four Shards rise into the sky... and fuse into a shining purple bottle!"),
                    Slide(Storm, john, true, "Uncle John", "Sam! That bottle is the only thing strong enough to hold a storm like this. Catch him!"),
                    Slide(Storm, sam, false, "Sam", "Hey, storm! You want your weather back? Come and get it - because I'm bottling YOU!", shake: true),
                }),
                // After "Boss Sealed".
                Cutscene(CutsceneId.TrueEnding, true, EndingKind.True, CutsceneNext.CompleteScreen, new[]
                {
                    Slide(Storm, null, false, "", "With one last roar, Tempest spirals into the bottle. The glass flashes every colour of the rainbow - and the cork slams shut. POP!"),
                    Slide(Storm, tempest, true, "Tempest", "Hmph... Fine. You win, little shopkeeper. I'll stay in here... for now."),
                    Slide(Clear, null, false, "", "The clouds tear open. Sunlight pours over the town, and a rainbow stretches from one end of the sky to the other."),
                    Slide(Clear, john, true, "Uncle John", "Sam... you didn't just run the shop. You saved the whole town. I've never been prouder of you."),
                    Slide(Clear, sam, false, "Sam", "Then let's make it official - Weather for Sale is OPEN for business!"),
                    BigSlide("Secret Ending"),
                }),
            };
            return list.ToArray();
        }

        /// <summary>Slides (1-based) whose CGI picture does not show the speaker: Opening 5 is a close-up of Uncle John's note.</summary>
        private static readonly (CutsceneId, int)[] SpeakerNotInPicture = { (CutsceneId.Opening, 5) };

        private static CutsceneSlide Slide(Color colour, Sprite portrait, bool right, string speaker, string text, bool flash = false, bool shake = false)
            => new CutsceneSlide { backgroundColor = colour, portrait = portrait, portraitRight = right, speaker = speaker, text = text, flash = flash, shake = shake };

        private static CutsceneSlide BigSlide(string text)
            => new CutsceneSlide { backgroundColor = Color.black, text = text, bigCentredText = true };

        private static CutsceneData Cutscene(CutsceneId id, bool recordsEnding, EndingKind ending, CutsceneNext next, CutsceneSlide[] slides)
        {
            string path = DataFolder + "/Cutscene_" + id + ".asset";
            CutsceneData data = GetOrCreate<CutsceneData>(path);
            if (data.slides == null || data.slides.Length == 0) // new (or emptied): fill from the defaults above
            {
                data.id = id;
                data.slides = slides;
                data.recordsEnding = recordsEnding;
                data.ending = ending;
                data.next = next;
            }
            // Pictures by file name; an edited asset keeps its text. Talking characters: waist up.
            for (int i = 0; i < data.slides.Length; i++)
            {
                Sprite portrait = data.slides[i].portrait;
                if (portrait != null && (portrait.name == "Portrait_Sam" || portrait.name == "Portrait_UncleJohn"))
                    data.slides[i].portrait = Story(portrait.name + "_Half.png");
                Sprite picture = AssetDatabase.LoadAssetAtPath<Sprite>(CutsceneArt + id + "_" + (i + 1) + ".png");
                if (picture != null) data.slides[i].background = picture;
            }
            // CGI slides whose speaker is not in the picture keep the waist-up portrait.
            foreach ((CutsceneId slideOf, int slideNumber) in SpeakerNotInPicture)
                if (slideOf == id && slideNumber <= data.slides.Length && data.slides[slideNumber - 1].portrait != null)
                    data.slides[slideNumber - 1].portraitOverPicture = true;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data); // saved now: a folder refresh for the next asset would reload it empty
            return data;
        }

        // ------------------------------------------------------------------ Level Select

        private static void BuildLevelSelect()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera(new Color(0.55f, 0.78f, 0.98f));
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform c = NewCanvas("LevelSelectCanvas");
            FrontEndStrings strings = GetOrCreateStrings();
            var catalog = AssetDatabase.LoadAssetAtPath<MapCatalog>("Assets/Data/Shop/MapCatalog.asset");

            RectTransform bgRt = Rect("Background", c);
            Stretch(bgRt);
            // The map table picture; the map cards and the secret slot sit on its parchment.
            Sprite tableArt = Story("LevelSelectBG.png");
            Img(bgRt, tableArt != null ? tableArt : Story("Cover.jpg"), tableArt != null ? Color.white : new Color(0.55f, 0.55f, 0.6f), false);

            RectTransform board = Rect("Board", c); // layout parent only: the background is the board
            Place(board, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1640f, 920f));
            Text title = Header(board, font, new Vector2(0f, 440f)); // 234 tall: top 15 px inside the screen edge

            // The four maps are painted on the parchment (LevelSelectBG.png, 1920 x 1080): each button is a clear
            // hit area over its painted map, with the stars along the top and the name on a capsule at the bottom.
            // Rects are in canvas units from the centre (board is 30 lower); catalog order: rainforest, valley, desert, glacier.
            Rect[] painted =
            {
                new Rect(-326f, 220f, 610f, 320f), // top-left: rainforest and waterfall
                new Rect(300f, 236f, 580f, 310f),  // top-right: red canyon in the wind (valley)
                new Rect(-330f, -84f, 620f, 330f), // bottom-left: desert arch (same height as the glacier, so names and stars line up)
                new Rect(354f, -84f, 690f, 330f),  // bottom-right: glacier
            };
            int count = catalog != null ? Mathf.Min(4, catalog.Count) : 0;
            var buttons = new Object[count];
            var names = new Object[count];
            var stars = new Object[count * 3];
            for (int i = 0; i < count; i++)
            {
                MapEntry map = catalog.maps[i];
                RectTransform cardRt = Rect("Map_" + map.id, board);
                Place(cardRt, new Vector2(0.5f, 0.5f), painted[i].position, painted[i].size);
                Image hit = Img(cardRt, null, new Color(1f, 1f, 1f, 0f), false); // invisible: the painting is the picture
                var button = cardRt.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                RectTransform labelRt = Rect("Label", cardRt);
                Place(labelRt, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(320f, 80f)); // up off the painted map's bottom edge
                Img(labelRt, S("CapsuleWood"), Color.white, false).raycastTarget = false;
                Text name = Txt("Name", labelRt, font, map.displayName, 40, TextAnchor.MiddleCenter, DarkBrown);
                Stretch(name.rectTransform);
                name.rectTransform.offsetMin = new Vector2(40f, 8f); // inside the capsule, clear of its rivets
                name.rectTransform.offsetMax = new Vector2(-40f, -8f);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 24;
                name.resizeTextMaxSize = 40;
                name.fontStyle = FontStyle.Bold;
                for (int s = 0; s < 3; s++)
                {
                    RectTransform star = Rect("Star_" + s, cardRt);
                    Place(star, new Vector2(0.5f, 1f), new Vector2(-80f + s * 80f, -110f), new Vector2(76f, 76f)); // down off the top edge, clear of the header
                    Image starImg = Img(star, S("StarSmallEmpty"), Color.white, false);
                    starImg.preserveAspect = true;
                    starImg.raycastTarget = false;
                    stars[i * 3 + s] = starImg;
                }
                buttons[i] = button;
                names[i] = name;
            }

            // The secret slot on the painted compass rose in the middle: a round button (lock, or Tempest once
            // unlocked); its name and hint on the parchment below the maps.
            RectTransform secret = Rect("Secret", board);
            Place(secret, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(220f, 220f));
            Sprite secretFrame = Story("SecretSlotFrame.png");
            Image secretBg = Img(secret, secretFrame != null ? secretFrame : S("RoundWood"), Color.white, false);
            secretBg.preserveAspect = true;
            var secretButton = secret.gameObject.AddComponent<Button>();
            secretButton.targetGraphic = secretBg;
            // Tempest in full colour inside the frame's ring (no lock badge: "???" and the press hint say it is locked).
            RectTransform tempestRt = Rect("Tempest", secret);
            Place(tempestRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(140f, 124f));
            Image tempestImg = Img(tempestRt, Story("Portrait_Tempest.png"), Color.white, false);
            tempestImg.preserveAspect = true;
            tempestImg.raycastTarget = false;
            Text secretLabel = null; // no name under the slot (user): only the hint when the locked slot is pressed
            Text secretHint = Txt("Hint", board, font, strings.secretLockedHint, 28, TextAnchor.UpperCenter, Color.white);
            Place(secretHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -318f), new Vector2(520f, 44f));
            secretHint.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            secretHint.gameObject.SetActive(false); // shown for a moment when the locked slot is pressed

            Button back = RoundIconButton("BackButton", c, S("IconBack"), new Vector2(0f, 1f), new Vector2(100f, -100f));

            // Details
            RectTransform detail = Overlay("Details", c);
            RectTransform dCard = Card(detail, Vector2.zero, new Vector2(900f, 760f));
            Text dName = Header(dCard, font, new Vector2(0f, 360f));
            var dStars = new Object[3];
            for (int s = 0; s < 3; s++)
            {
                RectTransform star = Rect("Star_" + s, dCard);
                Place(star, new Vector2(0.5f, 0.5f), new Vector2(-150f + s * 150f, s == 1 ? 175f : 158f), new Vector2(120f, 120f)); // clear of the header
                Image img = Img(star, S("StarBigEmpty"), Color.white, false);
                img.preserveAspect = true;
                dStars[s] = img;
            }
            var lines = new Object[3];
            for (int s = 0; s < 3; s++)
            {
                Text line = Txt("StarLine_" + s, dCard, font, "", 40, TextAnchor.MiddleCenter, DarkBrown);
                Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 48f - s * 56f), new Vector2(700f, 54f));
                lines[s] = line;
            }
            RectTransform shardIcon = Rect("ShardIcon", dCard);
            Place(shardIcon, new Vector2(0.5f, 0.5f), new Vector2(-120f, -140f), new Vector2(76f, 76f));
            Image shardImg = Img(shardIcon, S("IconShardEmpty"), Color.white, false);
            shardImg.preserveAspect = true;
            Text shardText = Txt("ShardText", dCard, font, "", 46, TextAnchor.MiddleLeft, DarkBrown);
            shardText.fontStyle = FontStyle.Bold;
            Place(shardText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(90f, -140f), new Vector2(320f, 70f));
            Button dStart = TextButton("StartButton", dCard, S("BtnBlueA"), font, new Vector2(0f, -268f), new Vector2(400f, 116f), Color.white, out Text dStartLabel);
            Button dClose = IconButton("CloseButton", dCard, S("IconClose"), new Vector2(455f, 300f), new Vector2(96f, 96f)); // beside the header, not on it
            detail.gameObject.SetActive(false);

            var select = new GameObject("LevelSelect").AddComponent<LevelSelect>();
            SetRefs(select,
                ("state", GetOrCreateModeState()),
                ("session", AssetDatabase.LoadAssetAtPath<ShopSession>("Assets/Data/Shop/ShopSession.asset")),
                ("progress", AssetDatabase.LoadAssetAtPath<ProgressService>("Assets/Data/ProgressService.asset")),
                ("catalog", catalog), ("suctionConfig", AssetDatabase.LoadAssetAtPath<SuctionConfig>("Assets/Data/Suction/SuctionConfig.asset")),
                ("strings", strings),
                ("starFull", S("StarSmall")), ("starEmpty", S("StarSmallEmpty")), ("shardFull", S("IconShard")), ("shardEmpty", S("IconShardEmpty")),
                ("title", title), ("secretButton", secretButton), ("secretLabel", secretLabel), ("secretLock", null),
                ("secretPicture", tempestImg), ("secretHint", secretHint),
                ("bossConfig", AssetDatabase.LoadAssetAtPath<WeatherForSale.Boss.BossConfig>("Assets/Data/Boss/BossConfig.asset")),
                ("secretStarFull", Story("StarSecret.png")), ("secretStarEmpty", Story("StarSecretEmpty.png")), ("backButton", back),
                ("detailRoot", detail.gameObject), ("detailName", dName), ("detailShardIcon", shardImg), ("detailShardText", shardText),
                ("detailStart", dStart), ("detailStartLabel", dStartLabel), ("detailClose", dClose));
            SetArray(select, "mapButtons", buttons);
            SetArray(select, "mapPictures", new Object[0]); // the maps are painted on the background
            SetArray(select, "mapNames", names);
            SetArray(select, "mapStars", stars);
            SetArray(select, "detailStars", dStars);
            SetArray(select, "detailStarLines", lines);
            title.text = strings.levelSelectTitle;
            dStartLabel.text = strings.startLabel;

            NewEventSystem();
            Save(scene, LevelSelectScene);
        }

        // ------------------------------------------------------------------ build settings

        private static void FixBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            void Add(string path) { if (System.IO.File.Exists(path) && !scenes.Exists(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true)); }
            Add(TitleScene);
            Add(CutsceneScene);
            Add("Assets/Scenes/Shop.unity");
            Add(LevelSelectScene);
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) Add(s.path); // keeps the maps and boss, drops missing ones
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ helpers

        private static void Save(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static Camera NewCamera(Color background)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return cam;
        }

        private static RectTransform NewCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        private static void NewEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static Image Img(RectTransform rt, Sprite sprite, Color colour, bool sliced)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = colour;
            if (sliced && sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        private static Text Txt(string name, Transform parent, Font font, string text, int size, TextAnchor anchor, Color colour)
        {
            RectTransform rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = colour;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static RectTransform Overlay(string name, RectTransform parent)
        {
            RectTransform rt = Rect(name, parent);
            Stretch(rt);
            Img(rt, null, new Color(0f, 0f, 0f, 0.7f), false);
            return rt;
        }

        private static RectTransform Card(RectTransform parent, Vector2 position, Vector2 size)
        {
            RectTransform rt = Rect("Card", parent);
            Place(rt, new Vector2(0.5f, 0.5f), position, size);
            Img(rt, S("PanelLarge"), Color.white, true);
            return rt;
        }

        /// <summary>The navy signboard header with white outlined title text on its face.</summary>
        private static Text Header(RectTransform card, Font font, Vector2 position)
        {
            RectTransform header = Rect("Header", card);
            Place(header, new Vector2(0.5f, 0.5f), position, SuctionStageBuilder.HeaderSize);
            Img(header, S("RibbonBlue"), Color.white, false);
            Text text = Txt("Title", header, font, "", 50, TextAnchor.MiddleCenter, Color.white);
            SuctionStageBuilder.PlaceOnHeaderFace(text.rectTransform);
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 28;
            text.resizeTextMaxSize = 50;
            var outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Outline;
            outline.effectDistance = new Vector2(3f, -3f);
            return text;
        }

        private static Button TextButton(string name, Transform parent, Sprite sprite, Font font, Vector2 position, Vector2 size, Color textColour, out Text label)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, new Vector2(0.5f, 0.5f), position, size);
            Image img = Img(rt, sprite, Color.white, true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            label = Txt("Label", rt, font, "", 50, TextAnchor.MiddleCenter, textColour);
            Stretch(label.rectTransform);
            label.fontStyle = FontStyle.Bold;
            if (textColour == Color.white)
            {
                var outline = label.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Outline;
                outline.effectDistance = new Vector2(2f, -2f);
            }
            return button;
        }

        /// <summary>A Mode Select picture card (720 x 998 art) with its mode name on a wooden capsule near the bottom.</summary>
        private static Button ModeCardButton(string name, Transform parent, Sprite card, Font font, Vector2 position, out Text label)
        {
            Button button = SpriteButton(name, parent, card, new Vector2(0.5f, 0.5f), position, new Vector2(520f, 720f));
            RectTransform capsule = Rect("Capsule", button.transform);
            Place(capsule, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(380f, 110f));
            Image capsuleImg = Img(capsule, S("CapsuleWood"), Color.white, false);
            capsuleImg.raycastTarget = false;
            label = Txt("Label", capsule, font, "", 48, TextAnchor.MiddleCenter, DarkBrown);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(40f, 8f);
            label.rectTransform.offsetMax = new Vector2(-40f, -8f);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            return button;
        }

        private static Button SpriteButton(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, anchor, position, size);
            Image img = Img(rt, sprite, Color.white, false);
            img.preserveAspect = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            return button;
        }

        private static Button IconButton(string name, Transform parent, Sprite icon, Vector2 position, Vector2 size)
        {
            Button b = SpriteButton(name, parent, icon, new Vector2(0.5f, 0.5f), position, size);
            return b;
        }

        /// <summary>A round orange button (the v2 key cap) with an icon on it, at a screen corner.</summary>
        private static Button RoundIconButton(string name, Transform parent, Sprite icon, Vector2 anchor, Vector2 position)
        {
            Button b = SpriteButton(name, parent, S("RoundWood"), anchor, position, new Vector2(150f, 150f));
            RectTransform iconRt = Rect("Icon", b.transform);
            Place(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(86f, 86f));
            Image img = Img(iconRt, icon, Color.white, false);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return b;
        }

        private static Slider VolumeRow(string name, RectTransform card, Font font, float y, out Text label)
        {
            label = Txt(name + "Label", card, font, name, 48, TextAnchor.MiddleLeft, DarkBrown);
            label.fontStyle = FontStyle.Bold;
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-265f, y), new Vector2(200f, 80f));
            RectTransform sliderRt = Rect(name + "Slider", card);
            Place(sliderRt, new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(540f, 56f));
            RectTransform background = Rect("Background", sliderRt);
            Stretch(background);
            Img(background, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Boss/PlayerFrame.png"), Color.white, true);
            RectTransform fillArea = Rect("Fill Area", sliderRt);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(14f, 9f);
            fillArea.offsetMax = new Vector2(-14f, -9f);
            RectTransform fill = Rect("Fill", fillArea);
            Stretch(fill);
            Img(fill, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Boss/PlayerFillGreen.png"), Color.white, true);
            RectTransform handleArea = Rect("Handle Slide Area", sliderRt);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(28f, 0f);
            handleArea.offsetMax = new Vector2(-28f, 0f);
            RectTransform handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(64f, 64f);
            Image handleImg = Img(handle, S("RoundWood"), Color.white, false);
            handleImg.preserveAspect = true;
            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = 100f;
            return slider;
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning("[FrontEndSceneBuilder] No field '" + field + "' on " + target.GetType().Name); continue; }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[FrontEndSceneBuilder] No field '" + field + "' on " + target.GetType().Name); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
