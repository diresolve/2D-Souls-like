using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int costInSouls;
    public bool isConsumable = true;
    public abstract bool Use(PlayerController player);
}