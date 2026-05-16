using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat system")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.5f, 1.5f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask hazardLayers;
    [SerializeField] private int attackDamage = 10;

    [Header("Timing")]
    [SerializeField] private float attackStartupTime = 0.1f;
    [SerializeField] private float attackRecoveryTime = 0.3f;
    [SerializeField] private int maxQueuedAttacks = 1;

    [Header("Dash Attack")]
    [SerializeField] private float dashAttackStartupTime = 0.05f;
    [SerializeField] private float dashAttackRecoveryTime = 0.25f;
    [SerializeField] private int dashAttackDamage = 15;
    [SerializeField] private Vector2 dashAttackBoxSize = new Vector2(3f, 1.5f);
    [SerializeField] private float dashAttackForwardOffset = 1.5f;

    public bool isAttacking {  get; private set; }
    public bool IsDashAttacking { get; private set; }
    public float AttackDuration { get { return attackStartupTime + attackRecoveryTime; } }
    public float DashAttackDuration { get { return dashAttackStartupTime + dashAttackRecoveryTime; } }
    public bool CanQueueAttack { get { return !isAttacking || queuedAttackCount < maxQueuedAttacks; } }
    public bool CanStartDashAttack { get { return !isAttacking; } }
    public int AttackSequence { get; private set; }
    public int DashAttackSequence { get; private set; }

    private PlayerController player;
    private int queuedAttackCount;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public void QueueAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(AttackSequenceRoutine());
            return;
        }

        if (queuedAttackCount < maxQueuedAttacks)
        {
            queuedAttackCount++;
        }
    }

    public void StartDashAttack()
    {
        if (!CanStartDashAttack)
        {
            return;
        }

        StartCoroutine(DashAttack());
    }

    private IEnumerator AttackSequenceRoutine()
    {
        do
        {
            yield return Attack();

            if (queuedAttackCount <= 0)
            {
                break;
            }

            queuedAttackCount--;
        }
        while (true);
    }

    private IEnumerator Attack()
    {
        PlayerController playerController = GetPlayerController();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        isAttacking = true;
        AttackSequence++;

        if (playerController != null && playerController.IsGrounded)
        {
            playerController.LockMovementForAttack(true);
        }
        yield return new WaitForSeconds(attackStartupTime);

        float verticalInput = GetVerticalInput();
        Vector3 hitPosition = attackPoint.position;
        Vector2 actualBoxSize = attackBoxSize;

        bool addBounce = false;

        if (verticalInput > 0.5f)
        {
            hitPosition = transform.position + Vector3.up * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
        }
        else if (verticalInput < -0.5f && playerController != null && !playerController.IsGrounded)
        {
            hitPosition = transform.position + Vector3.down * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
            addBounce = true;
        }

        // mozda da koristimo interface za ovo?

        LayerMask bounceableLayers = enemyLayers | hazardLayers;
        Collider2D[] hitBounceable = Physics2D.OverlapBoxAll(hitPosition, actualBoxSize, 0f, bounceableLayers);

     
        if (hitBounceable.Length > 0 && addBounce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * playerController.UpwardBounceForce, ForceMode2D.Impulse);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitPosition, actualBoxSize, 0f, enemyLayers);
        DamageEnemies(hitEnemies, attackDamage);

        yield return new WaitForSeconds(attackRecoveryTime);

        if (playerController != null)
        {
            playerController.LockMovementForAttack(false);
        }
        isAttacking = false;
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;
        IsDashAttacking = true;
        DashAttackSequence++;

        yield return new WaitForSeconds(dashAttackStartupTime);

        PlayerController playerController = GetPlayerController();
        float attackDirection = playerController != null && !playerController.IsFacingRight ? -1f : 1f;
        Vector3 hitPosition = transform.position + Vector3.right * attackDirection * dashAttackForwardOffset;
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitPosition, dashAttackBoxSize, 0f, enemyLayers);
        DamageEnemies(hitEnemies, dashAttackDamage);

        yield return new WaitForSeconds(dashAttackRecoveryTime);

        IsDashAttacking = false;
        isAttacking = false;
    }

    private void DamageEnemies(Collider2D[] hitEnemies, int damageAmount)
    {
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float differenceX = enemy.transform.position.x - transform.position.x;
                float forceDirection = differenceX >= 0 ? 1f : -1f;
                Vector2 attackDir = new Vector2(forceDirection, 0f);

                damageable.TakeDamage(damageAmount, attackDir);
                StartCoroutine(HitStop(0.05f));
            }
        }
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
        float verticalInput = GetVerticalInput();
        PlayerController playerController = GetPlayerController();
        Vector3 debugPos = attackPoint.position;
        Vector2 debugSize = attackBoxSize;
        if (Application.isPlaying)
        {
            if (verticalInput > 0.5f)
            {
                debugPos = transform.position + Vector3.up * 1.5f;
                debugSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
            }
            else if (verticalInput < -0.5f && playerController != null && !playerController.IsGrounded)
            {
                debugPos = transform.position + Vector3.down * 1.5f;
                debugSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
            }
        }
        Gizmos.DrawWireCube(debugPos, debugSize);

        if (playerController != null)
        {
            Gizmos.color = Color.cyan;
            float attackDirection = playerController.IsFacingRight ? 1f : -1f;
            Vector3 dashAttackPos = transform.position + Vector3.right * attackDirection * dashAttackForwardOffset;
            Gizmos.DrawWireCube(dashAttackPos, dashAttackBoxSize);
        }
    }

    private float GetVerticalInput()
    {
        PlayerController playerController = GetPlayerController();
        return playerController != null ? playerController.VerticalInput : 0f;
    }

    private PlayerController GetPlayerController()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        return player;
    }
}
