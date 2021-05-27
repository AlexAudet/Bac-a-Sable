using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public int score;
    public float timeLeft;
    public Text scoreText;
    public Text timeText;

    void Update()
    {
        timeLeft -= 1 * Time.deltaTime;

        if (timeLeft <= 0)
        {
            FindObjectOfType<BoiteEtFusilGameManager>().GameOver();
        }

        timeText.text = timeLeft.ToString();
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
    }
}
