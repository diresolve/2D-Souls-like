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

    [Header("Blocking")]
    [SerializeField] private float blockDuration = 0.5f;
    [SerializeField] private float blockCooldown = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip blockMotionClip;

    public bool IsBlocking { get; private set; }
    public bool isAttacking { get; private set; }
    public bool IsDashAttacking { get; private set; }
    public int AttackSequence { get; private set; }
    public int DashAttackSequence { get; private set; }
    public float AttackDuration => attackStartupTime + attackRecoveryTime;
    public float DashAttackDuration => dashAttackStartupTime + dashAttackRecoveryTime;
    public bool CanQueueAttack => !isAttacking || queuedAttackCount < maxQueuedAttacks;
    public bool CanStartDashAttack => !isAttacking;

    private PlayerController player;
    private Rigidbody2D rb;
    private int queuedAttackCount;
    private float blockCooldownTimer;
    private bool requiresBlockRelease;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (blockCooldownTimer > 0f)
        {
            blockCooldownTimer -= Time.deltaTime;
        }
    }

    public void AddDamageBonus(int amount)
    {
        attackDamage += amount;
        dashAttackDamage += amount;
    }

    public void SetBlocking(bool isHoldingBlock, bool hasStamina)
    {
        if (!isHoldingBlock)
        {
            requiresBlockRelease = false;
            return;
        }

        if (requiresBlockRelease || IsBlocking || blockCooldownTimer > 0f || !hasStamina || isAttacking || IsDashAttacking)
        {
            return;
        }

        StartCoroutine(BlockRoutine());
    }

    private IEnumerator BlockRoutine()
    {
        IsBlocking = true;
        requiresBlockRelease = true;

        if (audioSource != null && blockMotionClip != null)
        {
            audioSource.PlayOneShot(blockMotionClip);
        }

        if (player != null && player.IsGrounded)
        {
            player.SetMovementLocked(true);
        }

        yield return new WaitForSeconds(blockDuration);

        if (player != null)
        {
            player.SetMovementLocked(false);
        }

        IsBlocking = false;
        blockCooldownTimer = blockCooldown;
    }

    public bool TryBlockAttack(float attackDirectionX, bool isFacingRight)
    {
        if (!IsBlocking) return false;

        float facingDirectionX = isFacingRight ? 1f : -1f;
        return Mathf.Sign(attackDirectionX) == facingDirectionX;
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
        if (!CanStartDashAttack) return;

        StartCoroutine(DashAttack());
    }

    private IEnumerator AttackSequenceRoutine()
    {
        yield return Attack();

        while (queuedAttackCount > 0)
        {
            queuedAttackCount--;
            yield return Attack();
        }
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        AttackSequence++;

        if (audioSource != null && attackClip != null) audioSource.PlayOneShot(attackClip);

        if (player != null && player.IsGrounded)
        {
            player.SetMovementLocked(true);
        }

        yield return new WaitForSeconds(attackStartupTime);

        float verticalInput = player != null ? player.VerticalInput : 0f;
        Vector3 hitPosition = attackPoint.position;
        Vector2 actualBoxSize = attackBoxSize;
        bool addBounce = false;

        if (verticalInput > 0.5f)
        {
            hitPosition = transform.position + Vector3.up * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
        }
        else if (verticalInput < -0.5f && player != null && !player.IsGrounded)
        {
            hitPosition = transform.position + Vector3.down * 1.5f;
            actualBoxSize = new Vector2(attackBoxSize.y, attackBoxSize.x);
            addBounce = true;
        }

        LayerMask bounceableLayers = enemyLayers | hazardLayers;
        Collider2D[] hitBounceable = Physics2D.OverlapBoxAll(hitPosition, actualBoxSize, 0f, bounceableLayers);

        if (hitBounceable.Length > 0 && addBounce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * player.UpwardBounceForce, ForceMode2D.Impulse);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitPosition, actualBoxSize, 0f, enemyLayers);
        DamageEnemies(hitEnemies, attackDamage);

        yield return new WaitForSeconds(attackRecoveryTime);

        if (player != null)
        {
            player.SetMovementLocked(false);
        }
        isAttacking = false;
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;
        IsDashAttacking = true;
        DashAttackSequence++;

        yield return new WaitForSeconds(dashAttackStartupTime);

        float attackDirection = player != null && !player.IsFacingRight ? -1f : 1f;
        Vector3 hitPosition = transform.position + Vector3.right * attackDirection * dashAttackForwardOffset;
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitPosition, dashAttackBoxSize, 0f, enemyLayers);
        DamageEnemies(hitEnemies, dashAttackDamage);

        if (audioSource != null && attackClip != null) audioSource.PlayOneShot(attackClip);

        yield return new WaitForSeconds(dashAttackRecoveryTime);

        IsDashAttacking = false;
        isAttacking = false;
    }

    private void DamageEnemies(Collider2D[] hitEnemies, int damageAmount)
    {
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable == null) continue;

            float differenceX = enemy.transform.position.x - transform.position.x;
            float forceDirection = differenceX >= 0 ? 1f : -1f;
            Vector2 attackDir = new Vector2(forceDirection, 0f);

            damageable.TakeDamage(damageAmount, attackDir);
            StartCoroutine(HitStop(0.05f));
        }
    }

    private IEnumerator HitStop(float duration)
    {
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = previousTimeScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        PlayerController playerController = player != null ? player : GetComponent<PlayerController>();
        float verticalInput = playerController != null ? playerController.VerticalInput : 0f;
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
}
