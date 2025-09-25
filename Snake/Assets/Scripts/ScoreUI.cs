using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public Sprite[] digitSprites;
    public Image[] digitSlots;

    void Start()
    {
        UpdateScore(0);
    }

    public void UpdateScore(int score)
    {
        string scoreStr = score.ToString().PadLeft(digitSlots.Length, '0');

        for (int i = 0; i < digitSlots.Length; i++)
        {
            char c = scoreStr[i];
            int digit = c - '0';
            digitSlots[i].sprite = digitSprites[digit];
            digitSlots[i].enabled = true;
        }
    }
}
