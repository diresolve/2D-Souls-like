using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int capacity = 5; 
    public List<ItemData> items = new List<ItemData>();

    [Header("UI")]
    public InventoryUI inventoryUI;

    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasPersistedPlayerState) return;

        items.Clear();
        items.AddRange(GameManager.Instance.PersistedItems);

        if (inventoryUI != null) inventoryUI.RefreshUI();
    }

    public bool AddItem(ItemData item)
    {
        if (items.Count >= capacity)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        items.Add(item);
        Debug.Log($"{item.itemName} added to inventory.");

        if (inventoryUI != null) inventoryUI.RefreshUI();

        return true;
    }

    public bool ConsumeFirstHealthPotion()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is HealthPotionItem)
            {
                bool wasUsed = items[i].Use(player);

                if (wasUsed && items[i].isConsumable)
                {
                    items.RemoveAt(i);

                    if (inventoryUI != null) inventoryUI.RefreshUI();

                    return true;
                }
            }
        }

        Debug.Log("Nema� Health Potion u inventoryju!");
        return false;
    }
}