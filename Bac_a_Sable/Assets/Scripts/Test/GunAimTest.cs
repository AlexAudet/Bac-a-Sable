using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAimTest : MonoBehaviour
{

    public float domage = 10f;
    public float range = 100f;
    public float fireRate = 15f;
    public float impactForce = 30f;

    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    private float nextTimeToFire = 0f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }    
    }

    void Shoot()
    {
        muzzleFlash.Play();
        RaycastHit hit;

        if ((Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range)))
        {
            Debug.Log(hit.transform.name);

            TargetHealth target = hit.transform.GetComponent<TargetHealth>();

            if (target)
            {
                target.takeDomage(domage);
            }

           GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impactGO, 2f); 

        } 

    }
}
