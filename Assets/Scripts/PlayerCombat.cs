using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat system")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.5f, 1.5f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 10;

    [Header("Timing")]
    [SerializeField] private float attackStartupTime = 0.1f;
    [SerializeField] private float attackRecoveryTime = 0.3f;

    public bool isAttacking {  get; private set; }

    private PlayerController player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Attack()
    {
        isAttacking = true;
        if (player.IsGrounded)
        {
            player.LockMovementForAttack(true);
        }
        yield return new WaitForSeconds(attackStartupTime);

        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 hitPosition = attackPoint.position;
        Vector2 actualBoxSize = attackBoxSize;

        if (verticalInput > 0.5f)
        {
            hitPosition = transform.position + Vector3.up * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
        }
        else if (verticalInput < -0.5f && !player.IsGrounded)
        {
            hitPosition = transform.position + Vector3.down * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
        }
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitPosition, attackBoxSize, 0f, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage); // poboljsat interakciju, da ima neki bounce i to
                StartCoroutine(HitStop(0.05f));
            }
        }
        yield return new WaitForSeconds(attackRecoveryTime);

        player.LockMovementForAttack(false);
        isAttacking = false;
    }

    private IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }
        Gizmos.color = Color.yellow;
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 debugPos = attackPoint.position;
        Vector2 debugSize = attackBoxSize;
        if (Application.isPlaying)
        {
            if (verticalInput > 0.5f)
            {
                debugPos = transform.position + Vector3.up * 1.5f;
                debugSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
            }
            else if (verticalInput < -0.5f && !GetComponent<PlayerController>().IsGrounded)
            {
                debugPos = transform.position + Vector3.down * 1.5f;
                debugSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
                // to do: ovo poboljsat
            }
        }
        Gizmos.DrawWireCube(debugPos, debugSize);
    }
}
