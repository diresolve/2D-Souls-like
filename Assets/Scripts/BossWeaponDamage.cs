using UnityEngine;

public class BossWeaponDamage : MonoBehaviour
{
    [SerializeField] private int damage = 25;

    public int Damage => damage;

    public void SetDamage(int value)
    {
        damage = Mathf.Max(value, 0);
    }
}