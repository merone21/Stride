using UnityEngine;
using UnityEngine.UI;

public static class FinishResultsUI
{
    static GameObject root;

    public static void Show(float elapsedSeconds, string rank)
    {
        if (root != null)
            Object.Destroy(root);

        var minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        var seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
        var rankTitle = GameFinishManager.GetRankTitle(rank);
        var rankColor = GameFinishManager.GetRankColor(rank);

        root = new GameObject("FinishResultsUI");
        Object.DontDestroyOnLoad(root);

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(root.transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var overlay = CreateImage(canvasGo.transform, "Overlay", new Color(0f, 0f, 0f, 0.65f));
        StretchFull(overlay.rectTransform);

        var panel = CreateImage(canvasGo.transform, "Panel", new Color(0.08f, 0.1f, 0.14f, 0.95f));
        var panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 420f);

        CreateText(panel.transform, "Title", "Tebrikler!", 42, FontStyle.Bold,
            new Color(1f, 1f, 1f), TextAnchor.UpperCenter, new Vector2(0f, -30f), new Vector2(660f, 60f));

        var timeLabel = minutes == 0
            ? $"{seconds} saniyede"
            : $"{minutes} dakika {seconds} saniyede";

        CreateText(panel.transform, "TimeText", "Oyunu " + timeLabel + " tamamladınız!", 30, FontStyle.Normal,
            new Color(0.9f, 0.92f, 0.95f), TextAnchor.UpperCenter, new Vector2(0f, -110f), new Vector2(660f, 50f));

        CreateText(panel.transform, "RankLabel", "Rank", 24, FontStyle.Normal,
            new Color(0.7f, 0.75f, 0.8f), TextAnchor.UpperCenter, new Vector2(0f, -175f), new Vector2(660f, 36f));

        CreateText(panel.transform, "RankValue", rank, 96, FontStyle.Bold,
            rankColor, TextAnchor.UpperCenter, new Vector2(0f, -250f), new Vector2(660f, 110f));

        CreateText(panel.transform, "RankTitle", rankTitle, 28, FontStyle.Italic,
            rankColor, TextAnchor.UpperCenter, new Vector2(0f, -340f), new Vector2(660f, 40f));
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style,
        Color color, TextAnchor anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
