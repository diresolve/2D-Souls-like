using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthPotion", menuName = "Inventory/Health Potion")]
public class HealthPotionItem : ItemData
{
    public int healAmount = 30;

    public override bool Use(PlayerController player)
    {
        return player.TryUseHealingItem(healAmount);
    }
}