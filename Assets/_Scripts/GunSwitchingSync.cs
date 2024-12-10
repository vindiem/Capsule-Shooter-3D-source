using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GunSwitchingSync : MonoBehaviourPunCallbacks
{
    private PhotonView view;
    public GunSwitchingSync script;

    private void Awake()
    {
        view = GetComponent<PhotonView>();

        if (!view.IsMine)
        {
            script.enabled = false;
        }

    }


}
