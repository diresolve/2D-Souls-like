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

    public int HealthLevel => healthLevel;
    public int StaminaLevel => staminaLevel;
    public int DamageLevel => damageLevel;
    public int MaxLevel => maxLevelPerStat;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
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
        return true;
    }

    public bool TryLevelUpStamina()
    {
        if (!CanLevelUp(staminaLevel)) return false;
        int cost = GetCostForNextLevel(staminaLevel);
        playerController.AddSouls(-cost);
        playerController.AddMaxStaminaBonus(staminaPerLevel);
        staminaLevel++;
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
