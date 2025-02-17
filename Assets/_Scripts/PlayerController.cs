using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.UI;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    // == Player Settings ==
    [Header("Player Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Health Settings")]
    [SerializeField] private Image healthbarImage;
    private const float maxHealth = 100f;
    private float currentHealth = maxHealth;

    // == References ==
    [Header("References")]
    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private GameObject ui;
    [SerializeField] private Item[] items;

    // == Private Variables ==
    private Rigidbody rigidbody;
    private PhotonView photonView;
    private PlayerManager playerManager;

    private int itemIndex;
    private int previousItemIndex = -1;

    private float verticalLookRotation;
    private bool grounded = true;

    private Vector3 smoothMoveVelocity;
    private Vector3 moveAmount;

    // == Weapon Parameters ==
    private SingleShotGun gunParams;

    private void Awake()
    {
        Cursor.visible = false; // Cursor.lockState = CursorLockMode.Locked;
        rigidbody = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        playerManager = PhotonView.Find((int)photonView.InstantiationData[0]).GetComponent<PlayerManager>();
        gunParams = GameObject.Find("Rifle1").GetComponent<SingleShotGun>();
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

        // == Movement ==
        HandleLook();
        HandleMovement();
        HandleJump();

        // == Weapon Logic ==
        HandleWeaponSwitch();
        HandleWeaponUse();

        // Check if player fell
        if (transform.position.y < -10f)
        {
            HandleDeath();
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        rigidbody.MovePosition(rigidbody.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    private void HandleLook()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    private void HandleMovement()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(
            moveAmount,
            moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed),
            ref smoothMoveVelocity,
            smoothTime
        );
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rigidbody.AddForce(transform.up * jumpForce);
        }
    }

    private void HandleWeaponSwitch()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0f) EquipItem((itemIndex >= items.Length - 1) ? 0 : itemIndex + 1);
        else if (scroll < 0f) EquipItem((itemIndex <= 0) ? items.Length - 1 : itemIndex - 1);
    }

    private void HandleWeaponUse()
    {
        if (gunParams.isReloading) return;

        if (gunParams.currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetButton("Fire1") && Time.time > gunParams.nextFire)
        {
            gunParams.nextFire = Time.time + 1f / gunParams.fireRate;
            items[itemIndex].Use();
        }

        if (Input.GetKeyDown(KeyCode.R)) Reload();
    }

    private void EquipItem(int _index)
    {
        if (_index == previousItemIndex) return;

        itemIndex = _index;
        items[itemIndex].itemGameObjects.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObjects.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (photonView.IsMine)
        {
            Hashtable hash = new Hashtable { { "itemIndex", itemIndex } };
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
        grounded = _grounded;
    }

    public void TakeDamage(float damage)
    {
        photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage)
    {
        if (!photonView.IsMine) return;

        currentHealth -= damage;
        healthbarImage.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0) HandleDeath();
    }

    private void HandleDeath()
    {
        playerManager.Die();
    }

    private IEnumerator Reload()
    {
        gunParams.anim.SetBool("isReloading", true);
        gunParams.isReloading = true;

        yield return new WaitForSeconds(gunParams.reloadTime);

        gunParams.anim.SetBool("isReloading", false);
        gunParams.currentAmmo = gunParams.maxAmmo;
        gunParams.isReloading = false;
    }
}
