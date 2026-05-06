using UnityEngine;

public class LostSoul : MonoBehaviour
{
    [SerializeField] private int soulValue = 10;

    private bool hasBeenCollected = false;

    public void SetSoulValue(int amount)
    {
        soulValue = amount;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasBeenCollected)
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                hasBeenCollected = true;
                player.AddSouls(soulValue);
                //PlayerController.hasDroppedSouls = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ClearDroppedSouls();
                }
                Destroy(gameObject);
            }
        }
    }
}