using System.Collections;
using UnityEngine;

public class SkeletonController : MonoBehaviour, IDamageable
{
    public enum State { Idle, Chase, Attacking, Returning, Dead }

    [Header("State")]
    public State currentState = State.Idle;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Hit feedback & Knockback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackResistance = 1f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject skeletonWeapon;
    [SerializeField] private Transform visuals;

    [Header("Leash")]
    [SerializeField] private float leashRange = 6f;
    private Vector2 homePosition;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;

    private int currentHealth;
    private float nextAttackTime = 0f;
    private bool isFacingRight = true;
    private Color originalColor;

    private Rigidbody2D rb;
    private Transform player;
    private Coroutine attackRoutine;

    void Start()
    {
        homePosition = transform.position;
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        originalColor = spriteRenderer.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;

        if (skeletonWeapon != null)
            skeletonWeapon.SetActive(false);
    }

    void Update()
    {
        if (currentState != State.Dead && currentState != State.Attacking)
        {
            float distFromHome = Vector2.Distance(transform.position, homePosition);

            if (distFromHome > leashRange && currentState != State.Returning)
                currentState = State.Returning;
        }

        if (currentState == State.Dead || player == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                StopMoving();

                if (distanceToPlayer <= chaseRange)
                    currentState = State.Chase;

                break;

            case State.Chase:
                LookAtPlayer();

                float distToHome = Vector2.Distance(transform.position, homePosition);

                if (distToHome > leashRange + 1f)
                {
                    currentState = State.Returning;
                    break;
                }

                if (distanceToPlayer <= attackRange)
                {
                    StopMoving();
                    if (Time.time >= nextAttackTime)
                        attackRoutine = StartCoroutine(AttackRoutine());
                }
                else if (distanceToPlayer > chaseRange)
                {
                    currentState = State.Idle;
                }
                else
                {
                    MoveTowardsPlayer();
                }

                break;

            case State.Returning:
                LookAtPoint(homePosition);
                MoveTowardsPoint(homePosition);

                float distFromHome = Vector2.Distance(transform.position, homePosition);

                if (distFromHome < 0.5f)
                {
                    transform.position = new Vector3(homePosition.x, transform.position.y, transform.position.z);
                    StopMoving();
                    currentState = State.Idle;
                }

                break;

            case State.Attacking:
                break;
        }
    }

    private void MoveTowardsPoint(Vector2 target)
    {
        animator.SetFloat("Speed", moveSpeed);
        float direction = Mathf.Sign(target.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        HandleWalkSound();
    }

    private void MoveTowardsPlayer()
    {
        MoveTowardsPoint(player.position);
    }

    private void StopMoving()
    {
        animator.SetFloat("Speed", 0f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == walkClip)
        {
            audioSource.Stop();
        }
    }

    private void LookAtPlayer()
    {
        LookAtPoint(player.position);
    }

    private void LookAtPoint(Vector2 target)
    {
        if (target.x > transform.position.x && !isFacingRight)
            Flip();
        else if (target.x < transform.position.x && isFacingRight)
            Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = visuals.localScale;
        scale.x *= -1;
        visuals.localScale = scale;
    }

    private IEnumerator AttackRoutine()
    {
        currentState = State.Attacking;

        StopMoving();
        LookAtPlayer();

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1f);

        nextAttackTime = Time.time + attackCooldown;
        currentState = State.Chase;
    }

    public void TakeDamage(int amount, Vector2 attackDirection)
    {
        if (currentState == State.Dead)
            return;

        currentHealth -= amount;

        if (currentState == State.Attacking && attackRoutine != null)
        {
            StopCoroutine(attackRoutine);

            if (skeletonWeapon != null)
                skeletonWeapon.SetActive(false);

            currentState = State.Chase;
        }

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
            Die();
        else
        {
            ApplyCleanKnockback(attackDirection);
            animator.SetTrigger("Hurt");
        }
    }

    private void ApplyCleanKnockback(Vector2 attackDirection)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 knockbackDir = new Vector2(Mathf.Sign(attackDirection.x), 0f).normalized;
        float appliedForce = knockbackForce / knockbackResistance;
        rb.AddForce(knockbackDir * appliedForce, ForceMode2D.Impulse);
    }

    private IEnumerator DamageFlash()
    {
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        currentState = State.Dead;

        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        StopAllCoroutines();

        if (skeletonWeapon != null)
            skeletonWeapon.SetActive(false);

        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Dead");

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        Destroy(gameObject, 1.5f);
    }

    public void EnableWeaponHitbox()
    {
        if (skeletonWeapon != null)
            skeletonWeapon.SetActive(true);
        if (audioSource != null && attackClip != null)
        {
            audioSource.PlayOneShot(attackClip);
        }
    }

    public void DisableWeaponHitbox()
    {
        if (skeletonWeapon != null)
            skeletonWeapon.SetActive(false);
    }

    private void HandleWalkSound()
    {
        if (audioSource != null && walkClip != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = walkClip;
                audioSource.Play();
            }
        }
    }
}