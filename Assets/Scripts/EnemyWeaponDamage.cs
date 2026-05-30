using UnityEngine;

public class EnemyWeaponDamage : MonoBehaviour
{
    [SerializeField] private int damage = 25;

    public int Damage => damage;

    public void SetDamage(int value)
    {
        damage = value;
    }
}
