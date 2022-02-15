using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sets the HighScore text when the scene is loaded
/// </summary>
public class HighScore : MonoBehaviour
{
    [SerializeField] private Text _highScoreText;

    void Start()
    {
        //If the currentHighScore is 0, then we don't have an High Score set yet, so we hide the Text.
        int currentHighScore = PlayerPrefs.GetInt("HS", 0);
        if(currentHighScore == 0)
        {
            _highScoreText.enabled = false;
        }

        _highScoreText.text = $"High Score: {currentHighScore}";
    }
}
