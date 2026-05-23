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
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2f;

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
    [SerializeField] private float heavyAttackCooldown = 6f;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;

    [Header("Arena Event")]
    [SerializeField] private BossArenaTrigger arenaTrigger;

    [Header("Death")]
    [SerializeField] private float deathFloatHeight = 1.5f;
    [SerializeField] private float deathFloatDuration = 1.1f;

    //private int currentHealth;
    private Rigidbody2D boss;
    private BossWeaponDamage bossWeaponDamage;
    private float nextAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;
    private bool isFacingRight = false;

    void Start()
    {
        currentHealth = maxHealth;
        boss = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
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

       float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                animator.SetFloat("Speed", 0f);
                if (distanceToPlayer < 10f) 
                {
                    currentState = State.Chase;
                }
                break;

            case State.Chase:
                LookAtPlayer();
                animator.SetFloat("Speed", moveSpeed);

                if (distanceToPlayer <= attackRange)
                {
                    if (Time.time >= nextAttackTime)
                    {
                        bool useHeavyAttack = Time.time >= nextHeavyAttackTime;
                        StartCoroutine(AttackRoutine(useHeavyAttack));
                    }
                    else
                    {
                        animator.SetFloat("Speed", 0f);
                    }
                }
                else
                {
                    Vector2 target = new Vector2(player.position.x, boss.position.y);
                    Vector2 newPos = Vector2.MoveTowards(boss.position, target, moveSpeed * Time.deltaTime);
                    boss.MovePosition(newPos);
                }
                break;

            case State.Attacking:
                break;
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

        bossWeaponDamage.SetDamage(normalAttackDamage);
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
        animator.SetFloat("Speed", 0f);

        int attackDamage = isHeavyAttack
            ? Mathf.RoundToInt(normalAttackDamage * heavyAttackDamageMultiplier)
            : normalAttackDamage;
        float attackAnimationSpeed = isHeavyAttack ? Mathf.Max(heavyAttackAnimationSpeed, 0.01f) : 1f;

        SetBossWeaponDamage(attackDamage);
        animator.speed = attackAnimationSpeed;
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f / attackAnimationSpeed);

        animator.speed = 1f;
        SetBossWeaponDamage(normalAttackDamage);
        nextAttackTime = Time.time + attackCooldown;
        if (isHeavyAttack)
        {
            nextHeavyAttackTime = Time.time + heavyAttackCooldown;
        }
        currentState = State.Idle; 
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
