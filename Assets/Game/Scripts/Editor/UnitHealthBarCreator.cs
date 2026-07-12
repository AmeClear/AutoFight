#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UnitHealthBarCreator
{
    private const string PrefabPath = "Assets/Game/Res/Prefabs/UI/UnitHealthBar.prefab";
    private const string PoolRootName = "HealthBarCanvas";

    [MenuItem("Tools/Health Bar/Create Health Bar Prefab")]
    public static void CreateHealthBarPrefab()
    {
        EnsureFolder("Assets/Game/Res/Prefabs/UI");

        GameObject root = CreateHealthBarHierarchy();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        Debug.Log($"[HealthBar] Prefab 已创建: {PrefabPath}");
    }

    [MenuItem("Tools/Health Bar/Setup Scene Health Bar System")]
    public static void SetupSceneHealthBarSystem()
    {
        UnitHealthBar prefab = AssetDatabase.LoadAssetAtPath<UnitHealthBar>(PrefabPath);
        if (prefab == null)
            CreateHealthBarPrefab();

        prefab = AssetDatabase.LoadAssetAtPath<UnitHealthBar>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[HealthBar] 无法创建或加载血条 Prefab。");
            return;
        }

        Transform existingRoot = GameObject.Find(PoolRootName)?.transform;
        if (existingRoot != null)
        {
            Debug.LogWarning("[HealthBar] 场景中已存在 HealthBarCanvas，跳过创建。");
            Selection.activeGameObject = existingRoot.gameObject;
            return;
        }

        GameObject canvasRoot = new GameObject(PoolRootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject subCanvasObject = new GameObject("HealthBarSubCanvas", typeof(Canvas), typeof(CanvasScaler));
        subCanvasObject.transform.SetParent(canvasRoot.transform, false);
        Canvas subCanvas = subCanvasObject.GetComponent<Canvas>();
        subCanvas.overrideSorting = true;
        subCanvas.sortingOrder = 100;
        subCanvas.pixelPerfect = false;

        CanvasScaler subScaler = subCanvasObject.GetComponent<CanvasScaler>();
        subScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        subScaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform subRect = subCanvasObject.GetComponent<RectTransform>();
        subRect.anchorMin = Vector2.zero;
        subRect.anchorMax = Vector2.one;
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        GameObject poolObject = new GameObject("UnitHealthBarPool", typeof(UnitHealthBarPool));
        poolObject.transform.SetParent(canvasRoot.transform, false);
        UnitHealthBarPool pool = poolObject.GetComponent<UnitHealthBarPool>();

        SerializedObject serializedPool = new SerializedObject(pool);
        serializedPool.FindProperty("healthBarPrefab").objectReferenceValue = prefab;
        serializedPool.FindProperty("healthBarRoot").objectReferenceValue = subRect;
        serializedPool.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        serializedPool.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(canvasRoot, "Create Health Bar System");
        Selection.activeGameObject = canvasRoot;
        Debug.Log("[HealthBar] 场景血条系统已创建。请在角色上挂载 ActorHealthBar，并确保模型下有 Head_Top 挂载点。");
    }

    private static GameObject CreateHealthBarHierarchy()
    {
        GameObject root = new GameObject("UnitHealthBar", typeof(RectTransform), typeof(UnitHealthBar));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(120f, 36f);

        Image healthFill = CreateBarRow(
            "HealthBackground",
            root.transform,
            new Vector2(0f, 10f),
            new Vector2(100f, 10f),
            new Color(1f, 0f, 0f, 1f),
            Color.white);

        Image staminaFill = CreateBarRow(
            "StaminaBackground",
            root.transform,
            new Vector2(0f, 0f),
            new Vector2(100f, 8f),
            new Color(0.45f, 0.3f, 0.05f, 1f),
            new Color(1f, 0.85f, 0.2f, 1f));

        Image defenseFill = CreateBarRow(
            "DefenseBackground",
            root.transform,
            new Vector2(0f, -9f),
            new Vector2(100f, 8f),
            new Color(0.1f, 0.25f, 0.45f, 1f),
            new Color(0.35f, 0.75f, 1f, 1f));

        GameObject textObject = new GameObject("HealthText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 10f);
        textRect.sizeDelta = new Vector2(100f, 10f);
        Text text = textObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.raycastTarget = false;
        text.text = "100/100";

        UnitHealthBar healthBar = root.GetComponent<UnitHealthBar>();
        SerializedObject serializedBar = new SerializedObject(healthBar);
        serializedBar.FindProperty("uiRect").objectReferenceValue = rootRect;
        serializedBar.FindProperty("fillImage").objectReferenceValue = healthFill;
        serializedBar.FindProperty("staminaFillImage").objectReferenceValue = staminaFill;
        serializedBar.FindProperty("defenseFillImage").objectReferenceValue = defenseFill;
        serializedBar.FindProperty("healthText").objectReferenceValue = text;
        serializedBar.FindProperty("showHealthText").boolValue = false;
        serializedBar.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static Image CreateBarRow(
        string backgroundName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color backgroundColor,
        Color fillColor)
    {
        GameObject background = CreateImage(backgroundName, parent, backgroundColor);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = anchoredPosition;
        backgroundRect.sizeDelta = sizeDelta;

        GameObject fill = CreateImage("Fill", background.transform, fillColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        StretchFull(fillRect);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
