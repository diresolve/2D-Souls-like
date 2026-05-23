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

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;

    [Header("Arena Event")]
    [SerializeField] private BossArenaTrigger arenaTrigger;

    //private int currentHealth;
    private Rigidbody2D boss;
    private float nextAttackTime = 0f;
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
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

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
                        StartCoroutine(AttackRoutine());
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

    private IEnumerator AttackRoutine()
    {
        currentState = State.Attacking;
        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f);

        nextAttackTime = Time.time + attackCooldown;
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
        animator.SetBool("Dead", true);
        boss.linearVelocity = Vector2.zero;
        GetComponent<Rigidbody2D>().gravityScale = 0f;

        GetComponent<Collider2D>().enabled = false;
    }

    public void TakeDamage(int amount, Vector2 attackDirection)
    {
        if (currentState == State.Dead) return;

        currentHealth -= amount;

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt"); 
        }

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
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
