using System.Collections;
using UnityEngine;
using TMPro;
using RayFire;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Netcode.Components;
using System;

public class ProjectileLauncher : NetworkBehaviour
{
    public bool shootingEnabled = true;

    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;

    [SerializeField] private Transform projectileSpawnPoint;

    //Gun stats
    public float shootForce, upwardForce;
    public float timeBetweenShooting, spread, reloadTime;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;
    int bulletsLeft, bulletsShot;

    //some bools
    bool shooting, readyToShoot, reloading;
    bool aiming;
    public Camera fpsCam;
    public bool allowInvoke = true;

    [SerializeField] GameObject parentObject;


    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
    }

    private void Start()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }

    void Update()
    {
        if (!IsOwner) return;
        MyInput();
    }

    private void MyInput()
    {
        //Input
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        aiming = Input.GetKey(KeyCode.Mouse1);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading) Reload();

        //Shoot
        if (readyToShoot && shooting && !reloading && aiming && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            PrimaryFireServerRpc();
            ShotDelay();
        }
    }

    private void SpawnDummyProjectile()
    {
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.GetPoint(75); ;
        Vector3 directionWithoutSpread = targetPoint - projectileSpawnPoint.position;
        GameObject currentBullet = Instantiate(clientProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        currentBullet.transform.forward = directionWithoutSpread.normalized;
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithoutSpread.normalized * shootForce, ForceMode.Impulse);
    }

    [ServerRpc]
    private void PrimaryFireServerRpc()
    {
        //Find the hit position using a raycast
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        //Check if the ray hits something
        Vector3 targetPoint = ray.GetPoint(75);

        //Calculate direction
        Vector3 directionWithoutSpread = targetPoint - projectileSpawnPoint.position;

        //Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(serverProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

        if (currentBullet.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact dealDamage))
        {
            dealDamage.SetOwner(OwnerClientId, parentObject);
        }

        currentBullet.transform.forward = directionWithoutSpread.normalized;

        //AddForce
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithoutSpread.normalized * shootForce, ForceMode.Impulse);

        SpawnDummyProjectileClientRpc();
    }


    [ClientRpc]
    private void SpawnDummyProjectileClientRpc()
    {
        //if (IsOwner) { return;}

        SpawnDummyProjectile();
    }

    private void ShotReset()
    {
        readyToShoot = true;
        allowInvoke = true;
    }

    private void Reload()
    {
        reloading = true;

        Invoke("ReloadingFinished", reloadTime);
    }

    private void ShotDelay()
    {
        readyToShoot = false;

        Invoke("ShotReset", timeBetweenShooting);
    }


    private void ReloadingFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    #region Setters

    public void SetShootForce(float v)
    {
        shootForce = v;
    }
    public void SetUpwardForce(float v)
    {
        upwardForce = v;
    }
    public void SetSpread(float v)
    {
        spread = v;
    }
    public void SetMagazinSize(float v)
    {
        int _v = Mathf.RoundToInt(v);
        magazineSize = _v;
    }
    public void SetBulletsPerTap(float v)
    {
        int _v = Mathf.RoundToInt(v);
        bulletsPerTap = _v;
    }

    #endregion
}
