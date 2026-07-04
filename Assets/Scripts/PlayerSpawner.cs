using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    public static GameObject LocalPlayerInstance { get; set; }

    static readonly Vector3[] SpawnPoints =
    {
        new Vector3(-6f, 2f, 2f),
        new Vector3(0f, 2f, 2f)
    };

    public static Vector3 GetSpawnPosition(int actorNumber)
    {
        int spawnIndex = actorNumber == 1 ? 0 : 1;
        return SpawnPoints[spawnIndex];
    }

    void Awake()
    {
        LocalPlayerInstance = null;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        TrySpawnLocalPlayer();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
            TrySpawnLocalPlayer();
    }

    void TrySpawnLocalPlayer()
    {
        if (LocalPlayerInstance != null)
            return;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Game sahnesine oda olmadan girildi. Önce Lobby'den oyun başlatın.");
            return;
        }

        string prefabName = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "Player1" : "Player2";
        int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? 0 : 1;
        var spawnPosition = SpawnPoints[spawnIndex];

        Debug.Log("Oyuncu oluşturuluyor: " + prefabName + " @ " + spawnPosition);
        LocalPlayerInstance = PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.identity);
        AssignCameraTarget(LocalPlayerInstance.transform);
    }

    static void AssignCameraTarget(Transform playerTransform)
    {
        var follow = FindFirstObjectByType<PlayerCameraFollow>();
        if (follow != null)
            follow.SetTarget(playerTransform);
    }

    public override void OnLeftRoom()
    {
        LocalPlayerInstance = null;
        SceneManager.LoadScene("Lobby");
    }
}
