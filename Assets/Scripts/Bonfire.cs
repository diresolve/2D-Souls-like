using UnityEngine;
using UnityEngine.InputSystem;

public class Bonfire : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Key interactKey = Key.E;

    [Header("UI")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private BonfireUI bonfireUI;

    private bool playerInRange;
    private PlayerStats playerStats;

    private void Awake()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || playerStats == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (bonfireUI != null) bonfireUI.Open(playerStats);
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerStats = other.GetComponent<PlayerStats>();
        if (playerStats == null) return;

        playerInRange = true;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        playerStats = null;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (bonfireUI != null) bonfireUI.Close();
    }
}
