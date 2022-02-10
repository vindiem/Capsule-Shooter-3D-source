using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;

public class scoreboardItem : MonoBehaviour
{
    public Text usernameText;
    public Text killsText;
    public Text deathsText;

    public void Initialize(Player player)
    {
        usernameText.text = player.NickName;
    }
}
