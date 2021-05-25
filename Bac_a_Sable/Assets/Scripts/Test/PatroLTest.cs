using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatroLTest : MonoBehaviour
{
    public GameObject objet;
    public float speed;
    public Transform[] movespot;
    private int randomspot;
    private float waittime;
    public float startwaittime;

    // Start is called before the first frame update
    void Start()
    {
        randomspot = Random.Range(0, movespot.Length);
    }

    // Update is called once per frame
    void Update()
    {
        objet.transform.position = Vector3.MoveTowards(transform.position, movespot[randomspot].position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position,movespot[randomspot].position) < 0.2f)
        {
            if (waittime<=0)
            {
                waittime = startwaittime;
                randomspot = Random.Range(0, movespot.Length);
            }
            else
            {
                waittime -= Time.deltaTime;
            }
        }
    }
}
