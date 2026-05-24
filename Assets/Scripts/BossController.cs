using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class BossController : MonoBehaviour, IDamageable
{
    public enum State { Idle, Chase, Attacking, Dead }

    [Header("State")]
    public State currentState = State.Idle;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 200;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackStartColliderGap = 0.15f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float chaseRange = 10f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hitbox")]
    [SerializeField] private GameObject bossWeapon;
    [SerializeField] private int currentHealth;

    [Header("Heavy Attack")]
    [SerializeField] private int normalAttackDamage = 25;
    [SerializeField] private float heavyAttackDamageMultiplier = 1.5f;
    [SerializeField] private float heavyAttackAnimationSpeed = 0.5f;
    [SerializeField] private float minHeavyAttackCooldown = 5f;
    [SerializeField] private float maxHeavyAttackCooldown = 7f;
    [SerializeField] private float attackAnimationClipLength = 0.8f;

    [Header("Rage Mode")]
    [SerializeField, Range(0.01f, 1f)] private float rageHealthThreshold = 0.5f;
    [SerializeField] private float rageMoveSpeedMultiplier = 1.25f;
    [SerializeField] private float rageDamageMultiplier = 1.25f;
    [SerializeField] private float rageHeavyCooldownMultiplier = 0.75f;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;

    [Header("Arena Event")]
    [SerializeField] private BossArenaTrigger arenaTrigger;

    [Header("Death")]
    [SerializeField] private float deathFloatHeight = 1.5f;
    [SerializeField] private float deathFloatDuration = 1.1f;

    //private int currentHealth;
    private Rigidbody2D boss;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;
    private BossWeaponDamage bossWeaponDamage;
    private Coroutine attackRoutine;
    private float nextAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;
    private bool isFacingRight = false;
    private bool isEnraged = false;

    void Start()
    {
        currentHealth = maxHealth;
        boss = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.interactable = false;
            if (healthBar.handleRect != null)
            {
                healthBar.handleRect.gameObject.SetActive(false);
            }
            UpdateHealthBar();
        }

        InitializeBossWeaponDamage();
    }

    void Update()
    {
       if (currentState == State.Dead)
        {
            return;
        }

       if (player == null)
        {
            StopMoving();
            return;
        }

       float distanceToPlayer = Vector2.Distance(transform.position, player.position);
       float horizontalGapToPlayer = GetHorizontalGapToPlayer();
       float attackStartGap = Mathf.Max(0f, attackStartColliderGap);

        switch (currentState)
        {
            case State.Idle:
                StopMoving();
                if (IsArenaActive() || distanceToPlayer < chaseRange) 
                {
                    ActivateBoss();
                }
                break;

            case State.Chase:
                LookAtPlayer();

                if (horizontalGapToPlayer > attackStartGap)
                {
                    MoveTowardsPlayer();
                }
                else
                {
                    StopMoving();
                    if (Time.time >= nextAttackTime)
                    {
                        bool useHeavyAttack = Time.time >= nextHeavyAttackTime;
                        attackRoutine = StartCoroutine(AttackRoutine(useHeavyAttack));
                    }
                }
                break;

            case State.Attacking:
                break;
        }
    }

    public void ActivateBoss()
    {
        animator.speed = 1f;
        if (currentState == State.Idle)
        {
            currentState = State.Chase;
        }
    }

    private bool IsArenaActive()
    {
        return arenaTrigger != null && arenaTrigger.HasTriggered;
    }

    private float GetHorizontalGapToPlayer()
    {
        if (bodyCollider != null && playerCollider != null)
        {
            Bounds bossBounds = bodyCollider.bounds;
            Bounds playerBounds = playerCollider.bounds;

            if (playerBounds.center.x >= bossBounds.center.x)
            {
                return Mathf.Max(0f, playerBounds.min.x - bossBounds.max.x);
            }

            return Mathf.Max(0f, bossBounds.min.x - playerBounds.max.x);
        }

        float fallbackCenterDistance = Mathf.Abs(player.position.x - transform.position.x);
        return Mathf.Max(0f, fallbackCenterDistance - attackRange);
    }

    private void MoveTowardsPlayer()
    {
        float currentMoveSpeed = GetCurrentMoveSpeed();
        animator.SetFloat("Speed", currentMoveSpeed);
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        boss.linearVelocity = new Vector2(direction * currentMoveSpeed, boss.linearVelocity.y);
    }

    private void StopMoving()
    {
        animator.SetFloat("Speed", 0f);
        if (boss != null)
        {
            boss.linearVelocity = new Vector2(0f, boss.linearVelocity.y);
        }
    }

    private void InitializeBossWeaponDamage()
    {
        if (bossWeapon == null)
        {
            return;
        }

        bossWeaponDamage = bossWeapon.GetComponent<BossWeaponDamage>();
        if (bossWeaponDamage == null)
        {
            bossWeaponDamage = bossWeapon.AddComponent<BossWeaponDamage>();
        }

        bossWeaponDamage.SetDamage(GetCurrentNormalAttackDamage());
    }

    public void EnableWeapon()
    {
        if (bossWeapon != null)
        {
            bossWeapon.SetActive(true);
        }
    }

    public void DisableWeapon()
    {
        if (bossWeapon != null)
        {
            bossWeapon.SetActive(false);
        }
    }

    private void SetBossWeaponDamage(int damage)
    {
        if (bossWeaponDamage == null)
        {
            InitializeBossWeaponDamage();
        }

        if (bossWeaponDamage != null)
        {
            bossWeaponDamage.SetDamage(damage);
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }
    private void LookAtPlayer()
    {
        if (player.position.x > transform.position.x && !isFacingRight)
            Flip();
        else if (player.position.x < transform.position.x && isFacingRight)
            Flip();
    }

    private IEnumerator AttackRoutine(bool isHeavyAttack)
    {
        currentState = State.Attacking;
        StopMoving();
        LookAtPlayer();

        int normalDamage = GetCurrentNormalAttackDamage();
        int attackDamage = isHeavyAttack
            ? Mathf.RoundToInt(normalDamage * heavyAttackDamageMultiplier)
            : normalDamage;
        float attackAnimationSpeed = isHeavyAttack ? Mathf.Max(heavyAttackAnimationSpeed, 0.01f) : 1f;

        SetBossWeaponDamage(attackDamage);
        animator.speed = attackAnimationSpeed;
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackAnimationClipLength / attackAnimationSpeed);

        FinishAttack(isHeavyAttack);
    }

    private void FinishAttack(bool wasHeavyAttack)
    {
        attackRoutine = null;
        animator.speed = 1f;
        animator.ResetTrigger("Attack");
        animator.CrossFade("BossIdle", 0.05f);
        SetBossWeaponDamage(GetCurrentNormalAttackDamage());
        nextAttackTime = Time.time + attackCooldown;
        if (wasHeavyAttack)
        {
            nextHeavyAttackTime = Time.time + GetNextHeavyAttackCooldown();
        }
        currentState = State.Chase; 
    }

    private float GetNextHeavyAttackCooldown()
    {
        float minCooldown = Mathf.Min(minHeavyAttackCooldown, maxHeavyAttackCooldown);
        float maxCooldown = Mathf.Max(minHeavyAttackCooldown, maxHeavyAttackCooldown);
        float cooldownMultiplier = isEnraged ? rageHeavyCooldownMultiplier : 1f;
        return Random.Range(minCooldown, maxCooldown) * cooldownMultiplier;
    }

    private float GetCurrentMoveSpeed()
    {
        return isEnraged ? moveSpeed * rageMoveSpeedMultiplier : moveSpeed;
    }

    private int GetCurrentNormalAttackDamage()
    {
        float damageMultiplier = isEnraged ? rageDamageMultiplier : 1f;
        return Mathf.Max(0, Mathf.RoundToInt(normalAttackDamage * damageMultiplier));
    }

    private void TryEnterRageMode()
    {
        if (isEnraged || maxHealth <= 0)
        {
            return;
        }

        float healthPercent = (float)currentHealth / maxHealth;
        if (healthPercent > rageHealthThreshold)
        {
            return;
        }

        isEnraged = true;
        SetBossWeaponDamage(GetCurrentNormalAttackDamage());

        if (nextHeavyAttackTime > Time.time)
        {
            float remainingCooldown = nextHeavyAttackTime - Time.time;
            nextHeavyAttackTime = Time.time + remainingCooldown * rageHeavyCooldownMultiplier;
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        StopAllCoroutines();
        attackRoutine = null;
        if (arenaTrigger != null)
        {
            arenaTrigger.ResetArea();
        }
        //if (spriteRenderer != null)
        //{
        //    spriteRenderer.color = Color.white;
        //}
        //if (healthBar != null)
        //{
        //    healthBar.gameObject.SetActive(false);
        //}
        currentState = State.Dead;
        animator.speed = 1f;
        animator.SetBool("Dead", true);
        boss.linearVelocity = Vector2.zero;
        boss.gravityScale = 0f;

        DisableWeapon();

        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(FloatUpOnDeath());
    }

    private IEnumerator FloatUpOnDeath()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * deathFloatHeight;
        float elapsedTime = 0f;

        while (elapsedTime < deathFloatDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / deathFloatDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(startPosition, endPosition, easedT);
            yield return null;
        }

        transform.position = endPosition;
    }

    public void TakeDamage(int amount, Vector2 attackDirection)
    {
        if (currentState == State.Dead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHealthBar();
        TryEnterRageMode();

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt"); 
        }

    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float clampedHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            healthBar.value = clampedHealth;

            if (healthBar.fillRect != null)
            {
                healthBar.fillRect.gameObject.SetActive(clampedHealth > 0f);
            }
        }
    }

    public void Vanish()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
}

public class BossWeaponDamage : MonoBehaviour
{
    [SerializeField] private int damage = 25;

    public int Damage { get { return damage; } }

    public void SetDamage(int value)
    {
        damage = Mathf.Max(value, 0);
    }
}
