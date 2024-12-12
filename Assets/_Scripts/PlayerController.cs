using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.UI;
using System;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] private Image healthbarImage;
    [SerializeField] private GameObject ui;
    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private float mouseSensitivity,
                                    sprintSpeed,
                                    walkSpeed, 
                                    jumpForce, 
                                    smoothTime;

    [SerializeField] private Item[] items;
    private int itemIndex;
    private int previousItemIndex = -1;
    private float verticalLookRotation;
    private bool grouned = true;
    private Vector3 smoothMoveVelocity;
    private Vector3 moveAmount;

    // Health
    private const float maxHealth = 100f;
    private float currentHealth = maxHealth;

    private PlayerManager playerManager;
    private Rigidbody rigidbody;
    private PhotonView photonView;
    private SingleShotGun parametr;

    private void Awake()
    {
        Cursor.visible = false; //Cursor.lockState = CursorLockMode.Locked;
        rigidbody = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        playerManager = PhotonView.Find((int)photonView.InstantiationData[0]).GetComponent<PlayerManager>();
        parametr = GameObject.Find("Rifle1").GetComponent<SingleShotGun>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            EquipItem(0);
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rigidbody);
            Destroy(ui);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        #region Movement
        Look();
        Move();
        Jump();
        #endregion

        #region (weapon logic)
        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            if (itemIndex >= items.Length - 2)
            {
                EquipItem(0);
            }
            else
            {
                EquipItem(itemIndex + 1);
            }
        }

        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            if (itemIndex <= 0)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                EquipItem(itemIndex - 1);
            }
        }

        if (parametr.isReloading)
        {
            return;
        }

        if (parametr.currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetButton("Fire1") && Time.time > parametr.nextFire)
        {
            
            parametr.nextFire = Time.time + 1f / parametr.fireRate;
            items[itemIndex].Use();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }

        #endregion

        /*if (Input.GetKeyDown(KeyCode.Tab))
        {
            
        }*/

        if (transform.position.y < -10f)
        {
            Death();
        }

    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        rigidbody.MovePosition(rigidbody.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);

    }

    private void Look()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    private void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * 
            (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), 
            ref smoothMoveVelocity, smoothTime);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grouned)
        {
            rigidbody.AddForce(transform.up * jumpForce);
        }
    }

    private void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
        {
            return;
        }
        itemIndex = _index;
        items[itemIndex].itemGameObjects.SetActive(true);
        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObjects.SetActive(false);
        }
        previousItemIndex = itemIndex;
        if (photonView.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!photonView.IsMine && targetPlayer == photonView.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }

    public void SetGroundedState(bool _grounded)
    {
        grouned = _grounded;
    }

    public void TakeDamage(float damage)
    {
        photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        currentHealth -= damage;
        healthbarImage.fillAmount = currentHealth / maxHealth;

        if (currentHealth < 0)
        {
            Death();
        }
    }

    private void Death()
    {
        playerManager.Die();
    }

    private IEnumerator Reload()
    {
        parametr.anim.SetBool("isReloading", true);
        parametr.isReloading = true;
        Debug.Log("Realoding...");

        yield return new WaitForSeconds(parametr.reloadTime);
        parametr.anim.SetBool("isReloading", false);
        parametr.currentAmmo = parametr.maxAmmo;
        parametr.isReloading = false;
    }

}
