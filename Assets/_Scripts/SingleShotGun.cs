using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam;
    PhotonView PV;

    public float fireRate;
    public float nextFire;
    //public float range;
    public ParticleSystem muzzleFlashVFX;
    public AudioClip shotSFX;
    public AudioSource _audioSource;
    public int maxAmmo = 15;
    public int currentAmmo;
    public float reloadTime = 4f;
    public bool isReloading = false;
    public Animator anim;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    public override void Use()
    {
        Shoot();
    }

    // system of raycast
    void Shoot()
    {
        _audioSource.PlayOneShot(shotSFX);
        muzzleFlashVFX.Play();

        currentAmmo--;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        ray.origin = cam.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
            PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
        }
    }

    // bullet impact
    [PunRPC]
    void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
    {
        //Debug.Log(hitPosition);
        Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
        if (colliders.Length != 0)
        {
            GameObject bulletImpactObj = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
            Destroy(bulletImpactObj, 5f);
            bulletImpactObj.transform.SetParent(colliders[0].transform);
        }

    }

}
