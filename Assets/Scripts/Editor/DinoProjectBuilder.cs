using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class DinoProjectBuilder
{
    const string AnimDir = "Assets/Animations";
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/Game.unity";

    const float GroundTop = -3f;
    const float TileWorldWidth = 20f;

    static Font uiFont;

    [MenuItem("Dino/Build Everything")]
    public static void BuildAll()
    {
        bool inputBackendChanged = EnsureNewInputBackend();
        ApplyPlayerSettings();

        foreach (string dir in new[] { AnimDir, PrefabDir, SceneDir })
            Directory.CreateDirectory(dir);

        GeneratedArt art = SpriteFactory.Generate();
        RuntimeAnimatorController controller = BuildAnimator(art);
        GameObject[] prefabs = BuildPrefabs(art);
        BuildScene(art, controller, prefabs[0], prefabs[1], prefabs[2]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dino: everything built. Open " + ScenePath + " and press Play.");

        if (inputBackendChanged && !Application.isBatchMode)
        {
            if (EditorUtility.DisplayDialog("Restart needed",
                    "Active Input Handling was switched to the new Input System. Unity must restart for it to take effect.\n\nRestart now?",
                    "Restart", "Later"))
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }
    }

    [MenuItem("Dino/Build Executable")]
    public static void BuildPlayer()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("Run Dino > Build Everything first.");
            return;
        }

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string name = "DinoRun";
        string location;
        switch (target)
        {
            case BuildTarget.StandaloneWindows64: location = "Builds/Windows/" + name + ".exe"; break;
            case BuildTarget.StandaloneOSX: location = "Builds/macOS/" + name + ".app"; break;
            default: location = "Builds/" + target + "/" + name; break;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(location));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = location,
            target = target,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Dino build " + report.summary.result + ": " + location);
        if (report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(location);
    }

    static bool EnsureNewInputBackend()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return false;

        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty prop = so.FindProperty("activeInputHandler");
        if (prop == null || prop.intValue == 1) return false;

        prop.intValue = 1; // 0 = old, 1 = new, 2 = both
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return true;
    }

    static void ApplyPlayerSettings()
    {
        PlayerSettings.productName = "Dino Run";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
    }

    static RuntimeAnimatorController BuildAnimator(GeneratedArt art)
    {
        AnimationClip run = MakeClip("Run", new[] { art.dinoRun[0], art.dinoRun[1] }, 0.3f, true);
        AnimationClip jump = MakeClip("Jump", new[] { art.dinoJump }, 0.1f, true);
        AnimationClip hit = MakeClip("Hit", new[] { art.dinoHit }, 0.8f, false);
        AnimationClip death = MakeClip("Death", new[] { art.dinoDead }, 0.1f, true);

        AddHitFlash(hit);

        string path = AnimDir + "/Dino.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = "Grounded",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true,
        });
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState runState = sm.AddState("Run");
        AnimatorState jumpState = sm.AddState("Jump");
        AnimatorState hitState = sm.AddState("Hit");
        AnimatorState deathState = sm.AddState("Death");
        runState.motion = run;
        jumpState.motion = jump;
        hitState.motion = hit;
        deathState.motion = death;
        sm.defaultState = runState;

        AnimatorStateTransition t = runState.AddTransition(jumpState);
        t.hasExitTime = false; t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

        t = jumpState.AddTransition(runState);
        t.hasExitTime = false; t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        t = sm.AddAnyStateTransition(hitState);
        t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "Hit");

        t = hitState.AddTransition(runState);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0f;

        t = sm.AddAnyStateTransition(deathState);
        t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimationClip MakeClip(string name, Sprite[] frames, float length, bool loop)
    {
        string path = AnimDir + "/Dino_" + name + ".anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { frameRate = 60f };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite",
        };

        // extra key at the end so the last frame isn't cut short
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i * length / frames.Length, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe
        {
            time = length,
            value = loop ? frames[0] : frames[frames.Length - 1],
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void AddHitFlash(AnimationClip clip)
    {
        const int steps = 8; // 0.1s each over 0.8s
        AnimationCurve r = new AnimationCurve(), g = new AnimationCurve(), b = new AnimationCurve(), a = new AnimationCurve();
        for (int i = 0; i <= steps; i++)
        {
            float time = i * 0.1f;
            bool flash = i < steps && i % 2 == 0;
            r.AddKey(time, 1f);
            g.AddKey(time, flash ? 0.25f : 1f);
            b.AddKey(time, flash ? 0.25f : 1f);
            a.AddKey(time, i < steps && i % 2 == 1 ? 0.45f : 1f);
        }
        SetColorCurve(clip, "m_Color.r", r);
        SetColorCurve(clip, "m_Color.g", g);
        SetColorCurve(clip, "m_Color.b", b);
        SetColorCurve(clip, "m_Color.a", a);
    }

    static void SetColorCurve(AnimationClip clip, string property, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), property), curve);
    }

    static GameObject[] BuildPrefabs(GeneratedArt art)
    {
        GameObject cactus = NewInteractableObject("Cactus", art.cacti[0], 5, false);
        cactus.AddComponent<GroundObstacle>();
        SetRefs(cactus.GetComponent<GroundObstacle>(), "variants", art.cacti);

        GameObject bird = NewInteractableObject("Bird", art.bird[0], 5, false);
        AirObstacle air = bird.AddComponent<AirObstacle>();
        SetRefs(air, "flapFrames", art.bird);
        SetFloat(air, "bobAmplitude", 0.2f);
        SetFloat(air, "bobSpeed", 3f);

        GameObject star = NewInteractableObject("Star", art.star, 5, true);
        StarItem item = star.AddComponent<StarItem>();
        SetFloat(item, "bobAmplitude", 0.15f);
        SetFloat(item, "bobSpeed", 4f);

        return new[] { SavePrefab(cactus), SavePrefab(bird), SavePrefab(star) };
    }

    // collider and rigidbody have to exist before the script is added
    static GameObject NewInteractableObject(string name, Sprite sprite, int order, bool circle)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (circle)
        {
            CircleCollider2D c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = sprite.bounds.size.x * 0.5f;
            c.offset = sprite.bounds.center;
        }
        else
        {
            BoxCollider2D b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = sprite.bounds.size;
            b.offset = sprite.bounds.center;
        }
        return go;
    }

    static GameObject SavePrefab(GameObject go)
    {
        string path = PrefabDir + "/" + go.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void BuildScene(GeneratedArt art, RuntimeAnimatorController controller,
        GameObject cactusPrefab, GameObject birdPrefab, GameObject starPrefab)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildBackground(art);
        Collider2D ground = BuildGroundCollider();
        PlayerController player = BuildPlayer(art, controller, ground);

        GameObject managerGo = new GameObject("GameManager");
        GameManager gm = managerGo.AddComponent<GameManager>();

        GameObject spawnerGo = new GameObject("Spawner");
        ObstacleSpawner spawner = spawnerGo.AddComponent<ObstacleSpawner>();
        SetRef(spawner, "groundObstaclePrefab", cactusPrefab);
        SetRef(spawner, "airObstaclePrefab", birdPrefab);
        SetRef(spawner, "starPrefab", starPrefab);

        UIManager ui = BuildUI(art, gm, player);
        SetRef(gm, "player", player);
        SetRef(gm, "ui", ui);

        BuildEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void BuildCamera()
    {
        GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(214, 238, 246, 255);
        go.AddComponent<AudioListener>();
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    static void BuildBackground(GeneratedArt art)
    {
        GameObject root = new GameObject("World");

        GameObject sky = new GameObject("Sky");
        sky.transform.SetParent(root.transform);
        sky.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        SpriteRenderer skyRenderer = sky.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = art.sky;
        skyRenderer.sortingOrder = -100;

        MakeLayer(root, "Clouds", art.clouds, 0f, 0.08f, 0.25f, -90);
        MakeLayer(root, "Mountains", art.mountains, 0f, 0.2f, 0f, -80);
        MakeLayer(root, "Hills", art.hills, 0f, 0.45f, 0f, -70);
        MakeLayer(root, "Ground", art.ground, (GroundTop - 4.5f) / 2f, 1f, 0f, -10);
    }

    static void MakeLayer(GameObject parent, string name, Sprite sprite, float y, float factor, float drift, int order)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent.transform);
        layer.transform.position = new Vector3(0f, y, 0f);

        for (int i = 0; i < 2; i++)
        {
            GameObject tile = new GameObject("Tile" + i);
            tile.transform.SetParent(layer.transform, false);
            tile.transform.localPosition = new Vector3(i * TileWorldWidth, 0f, 0f);
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }

        ParallaxLayer p = layer.AddComponent<ParallaxLayer>();
        SetFloat(p, "speedFactor", factor);
        SetFloat(p, "tileWidth", TileWorldWidth);
        SetFloat(p, "drift", drift);
    }

    static Collider2D BuildGroundCollider()
    {
        GameObject go = new GameObject("GroundCollider");
        go.transform.position = new Vector3(0f, GroundTop - 0.5f, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(60f, 1f);
        return box;
    }

    static PlayerController BuildPlayer(GeneratedArt art, RuntimeAnimatorController controller, Collider2D ground)
    {
        GameObject go = new GameObject("Player");
        go.transform.position = new Vector3(-5f, GroundTop + 0.02f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.dinoRun[0];
        sr.sortingOrder = 10;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.mass = 1f;
        rb.freezeRotation = true;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.95f, 1.2f);
        box.offset = new Vector2(0.1f, 0.6f);

        PlayerController pc = go.AddComponent<PlayerController>();
        SetRef(pc, "groundCollider", ground);
        SetRef(pc, "animator", animator);
        return pc;
    }

    static void BuildEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    static UIManager BuildUI(GeneratedArt art, GameManager gm, PlayerController player)
    {
        uiFont = LoadBuiltinFont();

        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform root = canvasGo.transform;

        Color gold = new Color32(255, 214, 80, 255);
        Color white = Color.white;
        Color dim = new Color(0f, 0f, 0f, 0.62f);

        // HUD
        RectTransform hud = NewRect("HUD", root);
        Stretch(hud);

        MakeText(hud, "LivesLabel", "LIVES", 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(300f, 44f), white);
        RectTransform healthBg = MakePanel(hud, "HealthBar", new Color32(20, 20, 30, 220), new Vector2(0f, 1f), new Vector2(50f, -80f), new Vector2(420f, 44f));
        Image healthFill = MakeFill(healthBg, "Fill", art.white, 5f);
        for (int i = 1; i <= 2; i++) // two dividers split the bar into three lives
        {
            RectTransform div = NewRect("Divider" + i, healthBg);
            div.anchorMin = new Vector2(i / 3f, 0f);
            div.anchorMax = new Vector2(i / 3f, 1f);
            div.sizeDelta = new Vector2(5f, 0f);
            div.gameObject.AddComponent<Image>().color = new Color32(20, 20, 30, 255);
        }

        MakeText(hud, "DashLabel", "DASH  (SHIFT)", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(50f, -138f), new Vector2(320f, 36f), white);
        RectTransform dashBg = MakePanel(hud, "DashBar", new Color32(20, 20, 30, 220), new Vector2(0f, 1f), new Vector2(50f, -176f), new Vector2(420f, 20f));
        Image dashFill = MakeFill(dashBg, "Fill", art.white, 3f);
        dashFill.color = new Color32(90, 190, 255, 255);

        Text scoreText = MakeText(hud, "ScoreText", "SCORE  00000", 56, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-50f, -30f), new Vector2(700f, 70f), white);
        Text highText = MakeText(hud, "HighScoreText", "HI  00000", 40, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-50f, -100f), new Vector2(700f, 54f), gold);
        Button pauseButton = MakeButton(hud, "PauseButton", "II", 40, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(100f, 70f));

        // Main menu
        RectTransform menu = MakePanel(root, "MainMenu", dim, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(menu);
        MakeText(menu, "Title", "DINO RUN", 170, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(1400f, 200f), gold);
        MakeText(menu, "Subtitle", "survive the desert", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 60f), white);
        Text menuHigh = MakeText(menu, "MenuHighScore", "HIGH SCORE  00000", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 70f), gold);
        Button play = MakeButton(menu, "PlayButton", "PLAY", 56, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(440f, 110f));
        Button quit = MakeButton(menu, "QuitButton", "QUIT", 56, new Vector2(0.5f, 0.5f), new Vector2(0f, -185f), new Vector2(440f, 110f));
        MakeText(menu, "Hint", "SPACE  jump      SHIFT  dash      ESC  pause", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(1500f, 50f), white);

        // Pause menu
        RectTransform pause = MakePanel(root, "PauseMenu", dim, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(pause);
        MakeText(pause, "Title", "PAUSED", 130, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1200f, 160f), gold);
        Button resume = MakeButton(pause, "ResumeButton", "RESUME", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(480f, 100f));
        Button pauseRestart = MakeButton(pause, "RestartButton", "RESTART", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(480f, 100f));
        Button pauseMenuBtn = MakeButton(pause, "MainMenuButton", "MAIN MENU", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(480f, 100f));

        // Game over
        RectTransform over = MakePanel(root, "GameOver", dim, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(over);
        MakeText(over, "Title", "GAME OVER", 140, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(1400f, 170f), new Color32(240, 90, 80, 255));
        Text finalScore = MakeText(over, "FinalScore", "SCORE  00000", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(900f, 80f), white);
        Text finalHigh = MakeText(over, "FinalHigh", "HIGH SCORE  00000", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 64f), gold);
        Button overRestart = MakeButton(over, "RestartButton", "PLAY AGAIN", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(480f, 100f));
        Button overMenu = MakeButton(over, "MainMenuButton", "MAIN MENU", 52, new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(480f, 100f));

        // Button wiring
        Wire(play, gm.StartGame);
        Wire(quit, gm.Quit);
        Wire(pauseButton, gm.Pause);
        Wire(resume, gm.Resume);
        Wire(pauseRestart, gm.Restart);
        Wire(pauseMenuBtn, gm.GoToMainMenu);
        Wire(overRestart, gm.Restart);
        Wire(overMenu, gm.GoToMainMenu);

        UIManager ui = canvasGo.AddComponent<UIManager>();
        SetRef(ui, "player", player);
        SetRef(ui, "hudPanel", hud.gameObject);
        SetRef(ui, "mainMenuPanel", menu.gameObject);
        SetRef(ui, "pausePanel", pause.gameObject);
        SetRef(ui, "gameOverPanel", over.gameObject);
        SetRef(ui, "scoreText", scoreText);
        SetRef(ui, "highScoreText", highText);
        SetRef(ui, "healthFill", healthFill);
        SetRef(ui, "dashFill", dashFill);
        SetRef(ui, "menuHighScoreText", menuHigh);
        SetRef(ui, "finalScoreText", finalScore);
        SetRef(ui, "finalHighScoreText", finalHigh);

        menu.gameObject.SetActive(true);
        pause.gameObject.SetActive(false);
        over.gameObject.SetActive(false);
        return ui;
    }

    static Font LoadBuiltinFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        return f;
    }

    static void Wire(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static RectTransform MakePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = NewRect(name, parent);
        Place(rt, anchor, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static Image MakeFill(RectTransform bar, string name, Sprite sprite, float inset)
    {
        RectTransform rt = NewRect(name, bar);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);

        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(Transform parent, string name, string content, int size, TextAnchor align,
        Vector2 anchor, Vector2 pos, Vector2 box, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Place(rt, anchor, pos, box);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = uiFont;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = NewRect(name, parent);
        Place(rt, anchor, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color32(255, 214, 80, 255);

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock colors = b.colors;
        colors.highlightedColor = new Color(0.85f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        b.colors = colors;

        Text t = MakeText(rt, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color32(40, 34, 20, 255));
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero;
        t.rectTransform.offsetMax = Vector2.zero;
        return b;
    }

    static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindOrThrow(so, target, field);
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetRefs(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindOrThrow(so, target, field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        FindOrThrow(so, target, field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SerializedProperty FindOrThrow(SerializedObject so, Object target, string field)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) throw new System.Exception(target.GetType().Name + " has no serialized field '" + field + "'");
        return prop;
    }
}
