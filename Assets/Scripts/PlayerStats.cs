using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Cost Scaling")]
    [SerializeField] private int baseLevelCost = 100;
    [SerializeField] private int costIncrementPerLevel = 100;
    [SerializeField] private int maxLevelPerStat = 5;

    [Header("Per-Level Bonuses")]
    [SerializeField] private int healthPerLevel = 20;
    [SerializeField] private float staminaPerLevel = 20f;
    [SerializeField] private int damagePerLevel = 3;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;

    private int healthLevel = 0;
    private int staminaLevel = 0;
    private int damageLevel = 0;

    private int baseMaxHealth;
    private float baseMaxStamina;
    private float originalHealthBarWidth;
    private float originalStaminaBarWidth;

    public int HealthLevel => healthLevel;
    public int StaminaLevel => staminaLevel;
    public int DamageLevel => damageLevel;
    public int MaxLevel => maxLevelPerStat;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();

        if (playerController != null)
        {
            baseMaxHealth = playerController.MaxHealth;
            baseMaxStamina = playerController.MaxStamina;
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        CacheOriginalBarWidths();

        if (GameManager.Instance != null && GameManager.Instance.HasPersistedPlayerState)
        {
            int h = GameManager.Instance.PersistedHealthLevel;
            int s = GameManager.Instance.PersistedStaminaLevel;
            int d = GameManager.Instance.PersistedDamageLevel;

            if (playerController != null && h > 0) playerController.AddMaxHealthBonus(healthPerLevel * h);
            if (playerController != null && s > 0) playerController.AddMaxStaminaBonus(staminaPerLevel * s);
            if (playerCombat != null && d > 0) playerCombat.AddDamageBonus(damagePerLevel * d);

            healthLevel = h;
            staminaLevel = s;
            damageLevel = d;

            GameManager.Instance.ClearPersistedPlayerState();
        }

        UpdateBarWidths();
    }

    private void CacheOriginalBarWidths()
    {
        if (playerController == null) return;

        if (playerController.HealthBar != null)
        {
            RectTransform rt = playerController.HealthBar.GetComponent<RectTransform>();
            if (rt != null) originalHealthBarWidth = rt.sizeDelta.x;
        }

        if (playerController.StaminaBar != null)
        {
            RectTransform rt = playerController.StaminaBar.GetComponent<RectTransform>();
            if (rt != null) originalStaminaBarWidth = rt.sizeDelta.x;
        }
    }

    private void ResizeFromLeft(RectTransform rt, float newWidth)
    {
        if (rt == null) return;

        float scaleX = rt.localScale.x;
        float currentWidth = rt.sizeDelta.x;
        float leftEdge = rt.anchoredPosition.x - rt.pivot.x * currentWidth * scaleX;

        rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(leftEdge + rt.pivot.x * newWidth * scaleX, rt.anchoredPosition.y);
    }

    private void UpdateBarWidths()
    {
        if (playerController == null) return;

        int absoluteMaxHealth = baseMaxHealth + (healthPerLevel * maxLevelPerStat);
        float absoluteMaxStamina = baseMaxStamina + (staminaPerLevel * maxLevelPerStat);

        if (playerController.HealthBar != null && absoluteMaxHealth > 0)
        {
            RectTransform rt = playerController.HealthBar.GetComponent<RectTransform>();
            if (rt != null)
            {
                float ratio = (float)playerController.MaxHealth / absoluteMaxHealth;
                ResizeFromLeft(rt, originalHealthBarWidth * ratio);
            }
        }

        if (playerController.StaminaBar != null && absoluteMaxStamina > 0f)
        {
            RectTransform rt = playerController.StaminaBar.GetComponent<RectTransform>();
            if (rt != null)
            {
                float ratio = playerController.MaxStamina / absoluteMaxStamina;
                ResizeFromLeft(rt, originalStaminaBarWidth * ratio);
            }
        }
    }

    public int GetCostForNextLevel(int currentLevel)
    {
        return baseLevelCost + costIncrementPerLevel * currentLevel;
    }

    public bool CanLevelUp(int currentLevel)
    {
        if (currentLevel >= maxLevelPerStat) return false;
        if (playerController == null) return false;
        return playerController.CurrentSouls >= GetCostForNextLevel(currentLevel);
    }

    public bool TryLevelUpHealth()
    {
        if (!CanLevelUp(healthLevel)) return false;
        int cost = GetCostForNextLevel(healthLevel);
        playerController.AddSouls(-cost);
        playerController.AddMaxHealthBonus(healthPerLevel);
        healthLevel++;
        UpdateBarWidths();
        return true;
    }

    public bool TryLevelUpStamina()
    {
        if (!CanLevelUp(staminaLevel)) return false;
        int cost = GetCostForNextLevel(staminaLevel);
        playerController.AddSouls(-cost);
        playerController.AddMaxStaminaBonus(staminaPerLevel);
        staminaLevel++;
        UpdateBarWidths();
        return true;
    }

    public bool TryLevelUpDamage()
    {
        if (!CanLevelUp(damageLevel)) return false;
        int cost = GetCostForNextLevel(damageLevel);
        playerController.AddSouls(-cost);
        if (playerCombat != null) playerCombat.AddDamageBonus(damagePerLevel);
        damageLevel++;
        return true;
    }
}
