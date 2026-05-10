using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 25;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hit feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float knockbackResistance = 9f;

    private int currentHealth;
    private Color originalColor;

    private Rigidbody2D enemy;
    void Start()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
        { 
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        originalColor = spriteRenderer.color;

        enemy = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int amount, Vector2 attackDirection)
    {
        currentHealth -= amount;
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // toma ovaj odi dio je grozan moga si ga popravit

        float baseKnockbackForce = 10f;

        Vector2 knockbackDir = new Vector2(attackDirection.x, 0.2f).normalized;
        enemy.AddForce(knockbackDir * (baseKnockbackForce / knockbackResistance), ForceMode2D.Impulse);


    }
    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        currentHealth = maxHealth;
    }
}
