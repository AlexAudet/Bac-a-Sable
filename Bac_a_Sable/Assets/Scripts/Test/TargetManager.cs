using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TargetManager : MonoBehaviour
{
    public static TargetManager instance;
    TargetManager()
    {
        instance = this;
    }


    [PropertySpace(10, 10)]
    Canvas canvasInstance;
    HealthBar healthBarInstance;
    GameObject objectInstance;
    public Vector3 healthBarOffset = new Vector3(0,2.2f,0);
    public GameObject objetReference;
    public Canvas canvasReference;
    public Vector3 spawnPosition = new Vector3(1,0,1);
    public int speed = 1;
    public float TargetScale = 1;
    public Vector3 motionRange = new Vector3(10, 10, 10);
    public Vector3 direction;

    // Update is called once per frame
    Vector3 pos;
    void Update()
    {
        Zone();  
    }

    public void SpawnTarget()
    {
        if (objectInstance != null)
           Destroy(objectInstance);
      
        objectInstance = Instantiate(objetReference, spawnPosition, Quaternion.LookRotation(Vector3.zero));
        objectInstance.transform.localScale *= TargetScale;

        objectInstance.GetComponent<Collider>().isTrigger=true;

        canvasInstance = Instantiate(canvasReference,objectInstance.transform);
        canvasInstance.transform.localPosition = healthBarOffset;
        healthBarInstance = canvasInstance.GetComponentInChildren<HealthBar>();

        TargetHealth newTargetHealth;
        newTargetHealth = objectInstance.AddComponent<TargetHealth>();
        newTargetHealth.healthBar = healthBarInstance;
        RandomMovement();
    }

    public void AddSpeed()
    {
        speed++;
    }

    private void Zone()
    {
        pos = objectInstance.transform.position;

        if (pos.x > (spawnPosition.x + motionRange.x))
        {
            direction.x = Random.Range(-1f, -0.01f);
        }

        if (pos.x < spawnPosition.x - motionRange.x)
        {
            direction.x = Random.Range(0.01f, 1f);
        }

        if (pos.y > spawnPosition.y + motionRange.y)
        {
            direction.y = Random.Range(-1f, -0.01f);
        }

        if (pos.y < spawnPosition.y - motionRange.y)
        {
            direction.y = Random.Range(0.01f, 1f);
        }

        if (pos.z > spawnPosition.z + motionRange.z)
        {
            direction.z = Random.Range(-1f, -0.01f);
        }

        if (pos.z < spawnPosition.z - motionRange.z)
        {
            direction.z = Random.Range(0.01f, 1f);
        }

        objectInstance.transform.position += (direction * speed) * Time.deltaTime;
    }

    public void RandomMovement()
    {

        if (direction.x > 0)
        {
            direction.x = Random.Range(-1f, -0.01f);
        }

        if (direction.x < 0)
        {
            direction.x = Random.Range(0.01f, 1f);
        }

        if (direction.y > 0)
        {
            direction.y = Random.Range(-1f, -0.01f);
        }

        if (direction.y < 0)
        {
            direction.y = Random.Range(0.01f, 1f);
        }

        if (direction.z > 0)
        {
            direction.z = Random.Range(-1f, -0.01f);
        }

        if (direction.z < 0)
        {
            direction.z = Random.Range(0.01f, 1f);
        }

        objectInstance.transform.Translate(direction);
    }

}
