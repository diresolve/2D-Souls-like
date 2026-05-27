using UnityEngine;
using UnityEngine.InputSystem;

public class MerchantCat : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Key interactKey = Key.E;

    [Header("Optional Feedback")]
    [SerializeField] private GameObject interactPrompt;

    [Header("Meow Effect")]
    [SerializeField] private AudioClip meowSound;
    [SerializeField, Range(0f, 1f)] private float meowVolume = 1f;
    [SerializeField] private GameObject meowVfxPrefab;
    [SerializeField] private Transform meowVfxAnchor;
    [SerializeField] private float meowVfxLifetime = 1f;

    private bool playerInRange;
    private PlayerController playerInRangeRef;

    private void Awake()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || playerInRangeRef == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            Interact(playerInRangeRef);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        playerInRange = true;
        playerInRangeRef = player;

        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        playerInRangeRef = null;

        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Interact(PlayerController player)
    {
        PlayMeow();
    }

    private void PlayMeow()
    {
        Vector3 spawnPos = meowVfxAnchor != null ? meowVfxAnchor.position : transform.position;

        if (meowSound != null)
        {
            AudioSource.PlayClipAtPoint(meowSound, spawnPos, meowVolume);
        }

        if (meowVfxPrefab != null)
        {
            GameObject vfx = Instantiate(meowVfxPrefab, spawnPos, Quaternion.identity);
            if (meowVfxLifetime > 0f) Destroy(vfx, meowVfxLifetime);
        }
    }
}
