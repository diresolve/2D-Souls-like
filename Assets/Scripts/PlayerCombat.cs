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

    public bool isAttacking {  get; private set; }

    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public IEnumerator Attack()
    {
        PlayerController playerController = GetPlayerController();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        isAttacking = true;
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

        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float differenceX = enemy.transform.position.x - transform.position.x;
                float forceDirection = differenceX >= 0 ? 1f : -1f;

                Vector2 attackDir = new Vector2(forceDirection, 0f);

                damageable.TakeDamage(attackDamage, attackDir);
                StartCoroutine(HitStop(0.05f));
            }
        }
        yield return new WaitForSeconds(attackRecoveryTime);

        if (playerController != null)
        {
            playerController.LockMovementForAttack(false);
        }
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
