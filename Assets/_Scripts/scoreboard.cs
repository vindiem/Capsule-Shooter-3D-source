using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class scoreboard : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform container;
    [SerializeField] GameObject scoreboardItemPefab;

    Dictionary<Player, scoreboardItem> scoreboardItems = new Dictionary<Player, scoreboardItem>();

    void Start()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddScoreboardItem(player);
        }
    }



    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddScoreboardItem(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RemoveScoreBoardItem(otherPlayer);
    }

    void AddScoreboardItem(Player player)
    {
        scoreboardItem item = Instantiate(scoreboardItemPefab, container).GetComponent<scoreboardItem>();
        item.Initialize(player);
        scoreboardItems[player] = item;
    }

    public void RemoveScoreBoardItem(Player player)
    {
        Destroy(scoreboardItems[player].gameObject);
        scoreboardItems.Remove(player);

    }

    

}
