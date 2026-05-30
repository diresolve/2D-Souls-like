using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BonfireUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Health")]
    [SerializeField] private TextMeshProUGUI healthLevelText;
    [SerializeField] private TextMeshProUGUI healthCostText;
    [SerializeField] private Button healthButton;

    [Header("Stamina")]
    [SerializeField] private TextMeshProUGUI staminaLevelText;
    [SerializeField] private TextMeshProUGUI staminaCostText;
    [SerializeField] private Button staminaButton;

    [Header("Damage")]
    [SerializeField] private TextMeshProUGUI damageLevelText;
    [SerializeField] private TextMeshProUGUI damageCostText;
    [SerializeField] private Button damageButton;

    [Header("Other")]
    [SerializeField] private TextMeshProUGUI soulBalanceText;
    [SerializeField] private Button closeButton;
    [SerializeField] private MusicController musicController;

    [Header("Level Up VFX")]
    [SerializeField] private GameObject levelUpVfxPrefab;
    [SerializeField] private float levelUpVfxLifetime = 2f;
    [SerializeField] private Vector3 levelUpVfxOffset = Vector3.zero;

    private bool didLevelUpThisSession;

    private PlayerStats currentStats;
    private PlayerController currentPlayerController;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (healthButton != null) healthButton.onClick.AddListener(OnHealthClicked);
        if (staminaButton != null) staminaButton.onClick.AddListener(OnStaminaClicked);
        if (damageButton != null) damageButton.onClick.AddListener(OnDamageClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(PlayerStats stats)
    {
        currentStats = stats;
        currentPlayerController = stats != null ? stats.GetComponent<PlayerController>() : null;
        didLevelUpThisSession = false;

        if (currentPlayerController != null) currentPlayerController.SetCombatEnabled(false);
        if (musicController != null) musicController.PlayBonfireMusic();

        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (currentPlayerController != null) currentPlayerController.SetCombatEnabled(true);
        if (musicController != null) musicController.PlayBackgroundMusic();

        if (didLevelUpThisSession && levelUpVfxPrefab != null && currentPlayerController != null)
        {
            Vector3 spawnPos = currentPlayerController.transform.position + levelUpVfxOffset;
            GameObject vfx = Instantiate(levelUpVfxPrefab, spawnPos, Quaternion.identity);
            if (levelUpVfxLifetime > 0f) Destroy(vfx, levelUpVfxLifetime);
        }

        didLevelUpThisSession = false;
        currentStats = null;
        currentPlayerController = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        if (currentStats == null) return;

        UpdateStatRow(healthLevelText, healthCostText, healthButton, currentStats.HealthLevel);
        UpdateStatRow(staminaLevelText, staminaCostText, staminaButton, currentStats.StaminaLevel);
        UpdateStatRow(damageLevelText, damageCostText, damageButton, currentStats.DamageLevel);

        if (soulBalanceText != null && currentPlayerController != null)
        {
            soulBalanceText.text = currentPlayerController.CurrentSouls.ToString();
        }
    }

    private void UpdateStatRow(TextMeshProUGUI levelText, TextMeshProUGUI costText, Button button, int currentLevel)
    {
        if (levelText != null) levelText.text = $"Lv {currentLevel}/{currentStats.MaxLevel}";

        if (currentLevel >= currentStats.MaxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (button != null) button.interactable = false;
        }
        else
        {
            int cost = currentStats.GetCostForNextLevel(currentLevel);
            if (costText != null) costText.text = cost.ToString();
            if (button != null) button.interactable = currentStats.CanLevelUp(currentLevel);
        }
    }

    private void OnHealthClicked()
    {
        if (currentStats != null && currentStats.TryLevelUpHealth())
        {
            didLevelUpThisSession = true;
            Refresh();
        }
    }

    private void OnStaminaClicked()
    {
        if (currentStats != null && currentStats.TryLevelUpStamina())
        {
            didLevelUpThisSession = true;
            Refresh();
        }
    }

    private void OnDamageClicked()
    {
        if (currentStats != null && currentStats.TryLevelUpDamage())
        {
            didLevelUpThisSession = true;
            Refresh();
        }
    }
}
