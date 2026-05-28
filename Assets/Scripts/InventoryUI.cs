using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Reference")]
    public PlayerInventory playerInventory;
    public Image potionIcon;
    public TextMeshProUGUI potionCountText;
    public void RefreshUI()
    {
        if (playerInventory == null) return;

        int currentPotions = 0;
        foreach (ItemData item in playerInventory.items)
        {
            if (item is HealthPotionItem)
            {
                currentPotions++;
            }
        }

        if (potionCountText != null)
        {
            potionCountText.text = currentPotions.ToString();
        }

        if (potionIcon != null)
        {
            Color iconColor = potionIcon.color;
            iconColor.a = currentPotions > 0 ? 1f : 0.5f;
            potionIcon.color = iconColor;
        }
    }
}