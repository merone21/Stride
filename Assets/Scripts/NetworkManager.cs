using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    const byte MaxPlayers = 2;
    const string GameSceneName = "Game";

    public bool IsConnectedToMaster { get; private set; }

    string lastRequestedRoomName;
    bool tryJoinOnCreateFail;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
            return;

        Debug.Log("Photon'a bağlanılıyor...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Henüz bağlantı hazır değil.");
            return;
        }

        lastRequestedRoomName = string.IsNullOrWhiteSpace(roomName)
            ? "Oda" + Random.Range(1000, 9999)
            : roomName.Trim();

        tryJoinOnCreateFail = true;

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        TryCreateRoom(lastRequestedRoomName);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Henüz bağlantı hazır değil.");
            return;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("Katılmak için oda adı girin.");
            return;
        }

        tryJoinOnCreateFail = false;
        lastRequestedRoomName = roomName.Trim();

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        Debug.Log("Odaya katılınıyor: " + lastRequestedRoomName);
        PhotonNetwork.JoinRoom(lastRequestedRoomName);
    }

    void TryCreateRoom(string roomName)
    {
        var options = new RoomOptions
        {
            MaxPlayers = MaxPlayers,
            IsVisible = true,
            IsOpen = true,
            EmptyRoomTtl = 0
        };

        Debug.Log("Oda kuruluyor: " + roomName);
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnConnectedToMaster()
    {
        IsConnectedToMaster = true;
        Debug.Log("Master sunucusuna bağlandı. Lobiye giriliyor...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        IsConnectedToMaster = false;
        Debug.Log("Bağlantı kesildi: " + cause);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Lobiye girildi. Oda kurabilir veya mevcut odaya katılabilirsiniz.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        var lowerMessage = message?.ToLowerInvariant() ?? string.Empty;

        if (tryJoinOnCreateFail && lowerMessage.Contains("already exist"))
        {
            tryJoinOnCreateFail = false;
            Debug.LogWarning("Bu isimde oda zaten var. Katılmayı deniyor: " + lastRequestedRoomName);
            PhotonNetwork.JoinRoom(lastRequestedRoomName);
            return;
        }

        if (lowerMessage.Contains("already exist"))
        {
            var uniqueRoomName = lastRequestedRoomName + "-" + Random.Range(1000, 9999);
            Debug.LogWarning("Oda adı dolu. Yeni ad ile kuruluyor: " + uniqueRoomName);
            lastRequestedRoomName = uniqueRoomName;
            TryCreateRoom(uniqueRoomName);
            return;
        }

        Debug.LogError("Oda kurulamadı: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        var lowerMessage = message?.ToLowerInvariant() ?? string.Empty;

        if (lowerMessage.Contains("not exist") || lowerMessage.Contains("does not exist"))
        {
            Debug.LogError("Odaya katılınamadı. Oda adını kontrol edin.");
            return;
        }

        if (lowerMessage.Contains("full") || lowerMessage.Contains("already"))
        {
            Debug.LogError("Odaya katılınamadı. Oda dolu olabilir.");
            return;
        }

        Debug.LogError("Odaya katılınamadı: " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Odaya girildi: " + PhotonNetwork.CurrentRoom.Name +
                  " | Oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
        TryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " odaya katıldı. Oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
        TryStartGame();
    }

    void TryStartGame()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < MaxPlayers)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("İki oyuncu hazır. Game sahnesi yükleniyor...");
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(GameSceneName);
    }
}
