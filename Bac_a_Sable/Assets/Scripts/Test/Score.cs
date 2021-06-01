using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{

    public static Score instance;
    Score()
    {
        instance = this;
    }

    public int score;
    public float timeLeft;
    public Text scoreText;
    public Text timeText;

    void Update()
    {
        

        if (timeLeft <= 0)
        {
            timeLeft = 0;
            FindObjectOfType<BoiteEtFusilGameManager>().GameOver();
            timeText.text = timeLeft.ToString();
        }
        else
        {
            timeLeft -= Time.deltaTime;
            timeText.text = timeLeft.ToString();   
        }
        
    }
    void Start()
    {
        score = 0;
        timeLeft =30;
        timeText.text = timeLeft.ToString();
        scoreText.text = score.ToString();
    }
    public void AddPointsAndTime(int points)
    {
        score += points;
        timeLeft+=points;
        timeText.text = timeLeft.ToString();
        scoreText.text = score.ToString();
        TargetManager.instance.AddSpeed();
    }
}
