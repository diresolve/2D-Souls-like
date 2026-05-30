using UnityEngine;

public class GameIntro : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private PlayerController player;
    void Start()
    {
        if (GameManager.Instance.IntroPlayed)
        {
            gameObject.SetActive(false);
            return;
        }

        GameManager.Instance.IntroPlayed = true;
        animator = GetComponent<Animator>();

        player.SetMovementLocked(true);
    }
    public void PauseAnimation()
    {
        animator.speed = 0f;
        Invoke(nameof(ResumeAnimation), 1f);
    }

    private void ResumeAnimation()
    {
        animator.speed = 1f;
    }
    public void HideIntro()
    {
        player.SetMovementLocked(false);
        gameObject.SetActive(false);
    }
}