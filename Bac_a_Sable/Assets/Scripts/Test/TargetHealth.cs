using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TargetHealth : MonoBehaviour
{
    public HealthBar healthBar;
    
    public float maxHealth = 100;
    [ReadOnly]
    public float health;
    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }
    private void OnTriggerEnter(Collider other)
    {
        TargetManager.instance.RandomMovement();
    }

    public void takeDomage (float domage)
    {

        health -= domage;
        healthBar.SetHealth(health);
        if (health <= 0)
        {
            death();
            healthBar.SetHealth(health);
        }
        else
            healthBar.SetHealth(health);

    }


    private void death()
    {
        TargetManager.instance.SpawnTarget();
        Score.instance.AddPointsAndTime(TargetManager.instance.speed);
    }


}
