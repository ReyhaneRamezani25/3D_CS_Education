#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CourseUIBuilder_EN
{
    [MenuItem("Tools/Build Course Selection UI (EN)")]
    public static void BuildUI()
    {
        // === CANVAS ===
        GameObject canvasGO = new GameObject("Canvas_CourseUI",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // === EVENT SYSTEM ===
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // === BACKGROUND ===
        GameObject bg = CreateImage("Background", canvasGO.transform);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        StretchFull(bgRT);
        bg.GetComponent<Image>().color = new Color(0.96f, 0.82f, 0.92f, 1f); // soft pink

        // === HEADER ===
        GameObject header = CreateImage("Header", canvasGO.transform);
        RectTransform headerRT = header.GetComponent<RectTransform>();

        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 260);
        headerRT.anchoredPosition = new Vector2(0, -50);
        headerRT.offsetMin = new Vector2(60, headerRT.offsetMin.y);
        headerRT.offsetMax = new Vector2(-60, headerRT.offsetMax.y);

        Image headerImg = header.GetComponent<Image>();
        headerImg.color = new Color(0.58f, 0.30f, 0.70f, 1f); // purple

        Shadow headerShadow = header.AddComponent<Shadow>();
        headerShadow.effectDistance = new Vector2(0, -18);
        headerShadow.effectColor = new Color(0, 0, 0, 0.35f);

        // Header Icon
        GameObject headerIcon = CreateImage("HeaderIcon", header.transform);
        RectTransform iconRT = headerIcon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(120, 120);
        iconRT.anchoredPosition = new Vector2(40, 0);
        headerIcon.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);

        // Header Title
        GameObject headerTitle = CreateTMP("HeaderTitle", header.transform,
            "Choose a Course", 78, FontStyles.Bold);

        RectTransform titleRT = headerTitle.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.70f);
        titleRT.anchorMax = new Vector2(0.5f, 0.70f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(70, 0);
        titleRT.sizeDelta = new Vector2(900, 120);

        TextMeshProUGUI titleTMP = headerTitle.GetComponent<TextMeshProUGUI>();
        titleTMP.alignment = TextAlignmentOptions.Center;

        // Header Subtitle
        GameObject headerSub = CreateTMP("HeaderSubtitle", header.transform,
            "Select a course to begin", 38, FontStyles.Normal);

        RectTransform subRT = headerSub.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.35f);
        subRT.anchorMax = new Vector2(0.5f, 0.35f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(70, 0);
        subRT.sizeDelta = new Vector2(900, 80);

        TextMeshProUGUI subTMP = headerSub.GetComponent<TextMeshProUGUI>();
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(1, 1, 1, 0.8f);

        // === LESSON LIST ===
        GameObject list = new GameObject("LessonList",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));

        list.transform.SetParent(canvasGO.transform, false);

        RectTransform listRT = list.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0, 0);
        listRT.anchorMax = new Vector2(1, 1);
        listRT.offsetMin = new Vector2(60, 140);
        listRT.offsetMax = new Vector2(-60, -360);

        VerticalLayoutGroup vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 30;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = list.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // === CREATE 4 CARDS ===
        CreateLessonCard(list.transform, "Data Structures", "12 chapters | 50 exercises");
        CreateLessonCard(list.transform, "Programming Fundamentals", "10 chapters | 42 exercises");
        CreateLessonCard(list.transform, "Computer Networks", "9 chapters | 36 exercises");
        CreateLessonCard(list.transform, "Databases", "12 chapters | 48 exercises");

        Selection.activeGameObject = canvasGO;
        Debug.Log("✅ Course Selection UI (EN) built successfully!");
    }

    private static void CreateLessonCard(Transform parent, string title, string subtitle)
    {
        GameObject card = new GameObject("LessonCard_" + title,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        card.transform.SetParent(parent, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(960, 160);

        Image img = card.GetComponent<Image>();
        img.color = new Color(0.76f, 0.38f, 0.78f, 0.95f);

        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(0, -14);
        shadow.effectColor = new Color(0, 0, 0, 0.30f);

        // Icon placeholder
        GameObject icon = CreateImage("Icon", card.transform);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(120, 120);
        iconRT.anchoredPosition = new Vector2(40, 0);
        icon.GetComponent<Image>().color = new Color(1, 1, 1, 0.85f);

        // Text Group
        GameObject textGroup = new GameObject("TextGroup", typeof(RectTransform));
        textGroup.transform.SetParent(card.transform, false);

        RectTransform tgRT = textGroup.GetComponent<RectTransform>();
        tgRT.anchorMin = new Vector2(0, 0.5f);
        tgRT.anchorMax = new Vector2(0, 0.5f);
        tgRT.pivot = new Vector2(0, 0.5f);
        tgRT.anchoredPosition = new Vector2(190, 0);
        tgRT.sizeDelta = new Vector2(650, 140);

        // Title
        GameObject titleTMP = CreateTMP("Title", textGroup.transform, title, 56, FontStyles.Bold);
        RectTransform tRT = titleTMP.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0.65f);
        tRT.anchorMax = new Vector2(1, 0.65f);
        tRT.sizeDelta = new Vector2(0, 60);

        TextMeshProUGUI titleText = titleTMP.GetComponent<TextMeshProUGUI>();
        titleText.alignment = TextAlignmentOptions.Left;

        // Subtitle
        GameObject subTMP = CreateTMP("Subtitle", textGroup.transform, subtitle, 34, FontStyles.Normal);
        RectTransform sRT = subTMP.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0.25f);
        sRT.anchorMax = new Vector2(1, 0.25f);
        sRT.sizeDelta = new Vector2(0, 40);

        TextMeshProUGUI subtitleText = subTMP.GetComponent<TextMeshProUGUI>();
        subtitleText.alignment = TextAlignmentOptions.Left;
        subtitleText.color = new Color(1, 1, 1, 0.75f);

        // Arrow
        GameObject arrow = CreateTMP("Arrow", card.transform, "›", 80, FontStyles.Bold);
        RectTransform aRT = arrow.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(1, 0.5f);
        aRT.anchorMax = new Vector2(1, 0.5f);
        aRT.pivot = new Vector2(1, 0.5f);
        aRT.anchoredPosition = new Vector2(-30, 0);
        aRT.sizeDelta = new Vector2(80, 100);

        TextMeshProUGUI arrowText = arrow.GetComponent<TextMeshProUGUI>();
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.color = new Color(1, 1, 1, 0.8f);
    }

    private static GameObject CreateImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int size, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;

        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
