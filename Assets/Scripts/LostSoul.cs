using UnityEngine;

public class LostSoul : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("Check this box ONLY on the prefab that spawns when the player dies.")]
    [SerializeField] private bool isDeathRetrieval = false;
    [SerializeField] private int soulValue = 10;
    [SerializeField] private bool teleportPlayerOnCollect = false;

    [Header("Animation (Death Retrieval Only)")]
    [SerializeField] private Animator animator;
    [SerializeField] private float appearDistance = 8f;
    [SerializeField] private float shatterAnimDuration = 0.5f;

    private bool hasBeenCollected = false;
    private bool hasAppeared = false;
    private Transform playerTransform;

    private void Start()
    {
        if (isDeathRetrieval)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
    }

    private void Update()
    {
        if (!isDeathRetrieval) return;

        if (!hasAppeared && !hasBeenCollected && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= appearDistance)
            {
                hasAppeared = true;
                if (animator != null)
                {
                    animator.SetTrigger("Appear");
                }
            }
        }
    }

    public void SetSoulValue(int amount)
    {
        soulValue = amount;
    }

    public void EnableTeleportOnCollect()
    {
        teleportPlayerOnCollect = true;
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

                if (teleportPlayerOnCollect)
                {
                    player.TeleportToStart();
                }

                if (isDeathRetrieval)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ClearDroppedSouls();
                    }

                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null) rb.simulated = false;

                    if (animator != null) animator.SetTrigger("Collect");

                    Destroy(gameObject, shatterAnimDuration);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}