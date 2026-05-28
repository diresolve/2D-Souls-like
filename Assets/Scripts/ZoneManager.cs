using UnityEngine;
using System.Collections.Generic;

public class ZoneManager : MonoBehaviour
{
    [Header("Zone Settings")]
    public List<SkeletonController> enemiesInZone;
    public HeavyDoor doorToOpen;

    private bool isCleared = false;

    void Update()
    {
        if (isCleared) return;

        bool allDead = true;

        foreach (SkeletonController enemy in enemiesInZone)
        {
            if (enemy != null && enemy.currentState != SkeletonController.State.Dead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            isCleared = true;
            if (doorToOpen != null)
            {
                doorToOpen.Interact();
            }
        }
    }
}