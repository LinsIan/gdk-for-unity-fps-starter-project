using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreTextController : MonoBehaviour
{
    [SerializeField] private Text scoreText;

    private int counter = 0;

    public void UpdateScore(float score)
    {
        scoreText.text = "Score: "+ score.ToString();

    }

    public void Log(string message)
    {
        scoreText.text += " " + message;
        counter++;
        if (counter == 20)
        {
            scoreText.text = "Score: 0";
            counter = 0;
        }
    }
}
