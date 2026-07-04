using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Awake()
    {
        // MasterClient sahne değiştirince diğer client otomatik takip etsin
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        Debug.Log("Master sunucusuna bağlanılıyor...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Bağlandı. Oda aranıyor...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Boş oda yok, yeni oda kuruluyor.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Odaya girildi. Oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
        TryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Rakip odaya girdi. Oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
        TryStartGame();
    }

    void TryStartGame()
    {
        // İki kişi dolunca ve sadece MasterClient sahneyi yükler
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("İki oyuncu hazır, Game sahnesi yükleniyor.");
            PhotonNetwork.LoadLevel("Game");
        }
    }
}