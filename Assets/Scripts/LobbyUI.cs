using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviourPunCallbacks
{
    [SerializeField] InputField roomNameInput;
    [SerializeField] Text statusText;
    [SerializeField] Button createRoomButton;
    [SerializeField] Button joinRoomButton;
    [SerializeField] Transform roomListContent;
    [SerializeField] Text roomListEmptyText;

    readonly Dictionary<string, GameObject> roomListEntries = new Dictionary<string, GameObject>();

    void Awake()
    {
        EnsureEventSystem();
        EnsureUI();
        SetButtonsInteractable(false);
        SetStatus("Sunucuya bağlanılıyor...");
    }

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (roomNameInput != null)
            roomNameInput.interactable = true;

        if (PhotonNetwork.InLobby)
            OnJoinedLobby();
    }

    void OnCreateRoomClicked()
    {
        if (NetworkManager.Instance == null)
            return;

        SetStatus("Oda kuruluyor...");
        NetworkManager.Instance.CreateRoom(roomNameInput.text);
    }

    void OnJoinRoomClicked()
    {
        if (NetworkManager.Instance == null)
            return;

        SetStatus("Odaya katılınıyor...");
        NetworkManager.Instance.JoinRoom(roomNameInput.text);
    }

    public override void OnJoinedLobby()
    {
        SetButtonsInteractable(true);
        SetStatus("Bağlandı. Oda kurun veya listeden bir odaya katılın.");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var info in roomList)
        {
            if (info.RemovedFromList)
            {
                RemoveRoomListEntry(info.Name);
                continue;
            }

            if (!info.IsOpen || !info.IsVisible)
            {
                RemoveRoomListEntry(info.Name);
                continue;
            }

            if (roomListEntries.ContainsKey(info.Name))
                UpdateRoomListEntry(info);
            else
                AddRoomListEntry(info);
        }

        if (roomListEmptyText != null)
            roomListEmptyText.gameObject.SetActive(roomListEntries.Count == 0);
    }

    public override void OnJoinedRoom()
    {
        SetButtonsInteractable(false);
        var roomName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "";
        SetStatus("Odaya girildi: " + roomName + " | Rakip bekleniyor... (" +
                  PhotonNetwork.CurrentRoom.PlayerCount + "/2)");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus("Rakip bulundu! Oyun başlıyor... (" + PhotonNetwork.CurrentRoom.PlayerCount + "/2)");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (message != null && message.ToLowerInvariant().Contains("already exist"))
        {
            SetStatus("Bu isimde oda var, katılmayı deniyor...");
            return;
        }

        SetButtonsInteractable(true);
        SetStatus("Oda kurulamadı. Farklı bir oda adı deneyin.");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetButtonsInteractable(true);
        SetStatus("Odaya katılınamadı. Oda adını kontrol edin.");
    }

    void AddRoomListEntry(RoomInfo info)
    {
        var entry = CreateRoomListEntry(info);
        roomListEntries.Add(info.Name, entry);
    }

    void UpdateRoomListEntry(RoomInfo info)
    {
        if (!roomListEntries.TryGetValue(info.Name, out var entry))
            return;

        var label = entry.GetComponentInChildren<Text>();
        if (label != null)
            label.text = info.Name + "  (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
    }

    void RemoveRoomListEntry(string roomName)
    {
        if (!roomListEntries.TryGetValue(roomName, out var entry))
            return;

        Destroy(entry);
        roomListEntries.Remove(roomName);
    }

    GameObject CreateRoomListEntry(RoomInfo info)
    {
        var entry = new GameObject("Room_" + info.Name, typeof(RectTransform), typeof(Image), typeof(Button));
        entry.transform.SetParent(roomListContent, false);

        var rect = entry.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 36f);

        var image = entry.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(entry.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = info.Name + "  (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
        text.raycastTarget = false;

        var button = entry.GetComponent<Button>();
        var roomName = info.Name;
        button.onClick.AddListener(() =>
        {
            roomNameInput.text = roomName;
            NetworkManager.Instance.JoinRoom(roomName);
            SetStatus("Odaya katılınıyor: " + roomName);
        });

        return entry;
    }

    void SetButtonsInteractable(bool interactable)
    {
        createRoomButton.interactable = interactable;
        joinRoomButton.interactable = interactable;
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[Lobby] " + message);
    }

    static void EnsureEventSystem()
    {
        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = go.GetComponent<EventSystem>();
            DontDestroyOnLoad(go);
        }

        var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            Destroy(legacyModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    void EnsureUI()
    {
        if (roomNameInput != null && statusText != null)
            return;

        var canvasGo = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var panel = CreatePanel(canvasGo.transform, "LobbyPanel", new Vector2(500f, 520f));

        statusText = CreateText(panel, "StatusText", "Sunucuya bağlanılıyor...", 22, TextAnchor.UpperCenter,
            new Vector2(0f, -20f), new Vector2(460f, 60f));

        CreateText(panel, "RoomNameLabel", "Oda Adı", 20, TextAnchor.MiddleCenter,
            new Vector2(0f, -90f), new Vector2(460f, 30f));

        roomNameInput = CreateInputField(panel, "RoomNameInput", "Oda1234",
            new Vector2(0f, -130f), new Vector2(460f, 40f));

        var buttonRow = CreateButtonRow(panel, new Vector2(0f, -190f), new Vector2(460f, 45f), 20f);
        createRoomButton = CreateLayoutButton(buttonRow, "CreateRoomButton", "Oda Kur",
            new Color(0.15f, 0.55f, 0.25f));
        joinRoomButton = CreateLayoutButton(buttonRow, "JoinRoomButton", "Odaya Katıl",
            new Color(0.15f, 0.35f, 0.7f));

        CreateText(panel, "RoomListLabel", "Açık Odalar", 20, TextAnchor.MiddleCenter,
            new Vector2(0f, -250f), new Vector2(460f, 30f));

        var scrollGo = new GameObject("RoomListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel, false);
        var scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 1f);
        scrollRect.anchorMax = new Vector2(0.5f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, -285f);
        scrollRect.sizeDelta = new Vector2(460f, 200f);

        scrollGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(scrollGo.transform, false);
        roomListContent = contentGo.transform;

        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        roomListEmptyText = CreateText(contentGo.transform, "EmptyText", "Henüz açık oda yok", 18,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(400f, 40f));
    }

    static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
    {
        var panelGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);

        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        panelGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        return rect;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment,
        Vector2 position, Vector2 size)
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
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }

    static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = Color.white;
        text.supportRichText = false;

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        var placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10f, 6f);
        placeholderRect.offsetMax = new Vector2(-10f, -6f);

        var placeholderText = placeholderGo.GetComponent<Text>();
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 20;
        placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderText.text = placeholder;
        placeholderText.raycastTarget = false;

        var input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.interactable = true;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    static RectTransform CreateButtonRow(Transform parent, Vector2 position, Vector2 size, float spacing)
    {
        var rowGo = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(parent, false);

        var rect = rowGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        return rect;
    }

    static Button CreateLayoutButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var layoutElement = go.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 45f;

        go.GetComponent<Image>().color = color;

        var text = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        go.GetComponent<Image>().color = color;

        var text = CreateText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter, Vector2.zero, size);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }
}
