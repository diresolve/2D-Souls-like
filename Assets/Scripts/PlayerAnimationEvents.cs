using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    public void PlayFootstep()
    {
        if (player != null)
            player.PlayFootstep();
    }
}