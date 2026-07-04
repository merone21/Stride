using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GameFinishManager : MonoBehaviour
{
    public static GameFinishManager Instance { get; private set; }

    readonly HashSet<int> finishedPlayers = new HashSet<int>();
    double startTime;
    bool resultsShown;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        startTime = PhotonNetwork.IsConnected ? PhotonNetwork.Time : Time.timeAsDouble;
    }

    public void RegisterFinish(int actorNumber)
    {
        if (!finishedPlayers.Add(actorNumber))
            return;

        var required = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 2;
        if (finishedPlayers.Count >= required)
            ShowResults();
    }

    void ShowResults()
    {
        if (resultsShown)
            return;

        resultsShown = true;

        var endTime = PhotonNetwork.IsConnected ? PhotonNetwork.Time : Time.timeAsDouble;
        var elapsed = (float)(endTime - startTime);
        var rank = GetRank(elapsed);

        FinishResultsUI.Show(elapsed, rank);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static string GetRank(float totalSeconds)
    {
        if (totalSeconds < 120f) return "S";
        if (totalSeconds < 180f) return "A";
        if (totalSeconds < 240f) return "B";
        if (totalSeconds < 300f) return "C";
        return "D";
    }

    public static string GetRankTitle(string rank)
    {
        switch (rank)
        {
            case "S": return "Efsane!";
            case "A": return "Mükemmel!";
            case "B": return "İyi!";
            case "C": return "Fena değil";
            default: return "Tekrar dene";
        }
    }

    public static Color GetRankColor(string rank)
    {
        switch (rank)
        {
            case "S": return new Color(1f, 0.84f, 0f);
            case "A": return new Color(0.2f, 0.85f, 0.35f);
            case "B": return new Color(0.3f, 0.65f, 1f);
            case "C": return new Color(1f, 0.6f, 0.2f);
            default: return new Color(0.9f, 0.3f, 0.3f);
        }
    }
}
