using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player data")]
    public Vector3 LastDeathPosition;
    public int DroppedSoulsAmount = 0;
    public bool HasDroppedSouls = false;

    [Header("Persisted Player State")]
    public bool HasPersistedPlayerState = false;
    public int PersistedSouls;
    public int PersistedHealthLevel;
    public int PersistedStaminaLevel;
    public int PersistedDamageLevel;
    public List<ItemData> PersistedItems = new List<ItemData>();

    public bool IntroPlayed = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayerDeath(Vector3 deathPosition, int soulsToDrop)
    {
        LastDeathPosition = deathPosition;
        DroppedSoulsAmount = soulsToDrop;
        HasDroppedSouls = true;
    }

    public void ClearDroppedSouls()
    {
        HasDroppedSouls = false;
        DroppedSoulsAmount = 0;
    }

    public void SavePlayerState(int souls, int healthLevel, int staminaLevel, int damageLevel, List<ItemData> items)
    {
        PersistedSouls = souls;
        PersistedHealthLevel = healthLevel;
        PersistedStaminaLevel = staminaLevel;
        PersistedDamageLevel = damageLevel;

        PersistedItems.Clear();
        if (items != null) PersistedItems.AddRange(items);

        HasPersistedPlayerState = true;
    }

    public void ClearPersistedPlayerState()
    {
        HasPersistedPlayerState = false;
        PersistedItems.Clear();
    }
}
