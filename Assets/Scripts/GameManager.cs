using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player data")]
    public Vector3 LastDeathPosition;
    public int DroppedSoulsAmount = 0;
    public bool HasDroppedSouls = false;

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
}
