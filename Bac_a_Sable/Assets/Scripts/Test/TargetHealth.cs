using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TargetHealth : MonoBehaviour
{
    public HealthBar healthBar;
    public Score score;
    
    public float maxHealth = 100;
    [ReadOnly]
    public float health;
    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
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
        score.AddPointsAndTime(TargetManager.instance.speed);
    }


}
