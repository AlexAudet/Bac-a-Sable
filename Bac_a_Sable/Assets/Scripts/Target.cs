﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class legermouvement : MonoBehaviour
{

   [PropertySpace(10,10)]
    public GameObject objetReference;
    GameObject objectInstance;
    public Vector3 spawnPosition = new Vector3(1,0,1);
    public float speed = 1;
    public float TargetScale = 1;
    public Vector3 motionRange = new Vector3(10, 10, 10);
    public Vector3 direction = new Vector3(1, 1, 1);
    // Start is called before the first frame update
    void Start()
    {
        objectInstance = Instantiate(objetReference,spawnPosition,Quaternion.LookRotation(Vector3.up));
        objectInstance.transform.localScale *= TargetScale;
    }

    // Update is called once per frame

    Vector3 pos;
    void Update()
    {
       pos = objectInstance.transform.position;

       if(pos.x>(spawnPosition.x+motionRange.x))
        {
            direction.x = Random.Range(-1f, 0.01f);
        }

        if (pos.x < spawnPosition.x - motionRange.x)
        {
            direction.x = Random.Range(0.01f, 1f);
        }

        if (pos.y > spawnPosition.y + motionRange.y)
        {
            direction.y = Random.Range(-1f, 0.01f);
        }

        if (pos.y < spawnPosition.y - motionRange.y)
        {
            direction.y = Random.Range(0.01f, 1f);
        }

        if (pos.z > spawnPosition.z + motionRange.z)
        {
            direction.z = Random.Range(-1f, 0.01f);
        }

        if (pos.z < spawnPosition.z - motionRange.z)
        {
            direction.z = Random.Range(0.01f,1f);
        }

        objectInstance.transform.position += (direction * speed)*Time.deltaTime;

      //if(pu de vie) spawn un autre ;
    }
}
