using UnityEngine;

public class GameIntro : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance.IntroPlayed)
        {
            gameObject.SetActive(false);
            return;
        }

        GameManager.Instance.IntroPlayed = true;
    }
    public void HideIntro()
    {
        gameObject.SetActive(false);
    }
}