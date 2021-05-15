using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class legermouvement : MonoBehaviour
{
    public GameObject objet;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        objet.transform.position += new Vector3(1 * Time.deltaTime, 1 * Time.deltaTime, 1 * Time.deltaTime);
    }
}
