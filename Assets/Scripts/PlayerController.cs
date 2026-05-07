using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CameraFollowObject cameraFollowObject;

    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 8f;
    [SerializeField] private float jumpForce = 36f;

    [Header("Game Feel")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 40f;

    [Header("Ground Check")]
    [SerializeField] private BoxCollider2D feetCollider;
    [SerializeField] private LayerMask groundLayer;

    [Header("Mechanics")]
    [SerializeField] private float terminalVelocity = -12f;
    [SerializeField] private float swampSpeedMultiplier = 0.4f;
    [SerializeField] private float swampJumpMultiplier = 0.5f;
    [SerializeField] private float healTime = 1.5f;
    [SerializeField] private Vector2 damageKnockback = new Vector2(5f, 5f);
    [SerializeField] private float alteredGravityScale = 0.5f;

    [SerializeField] private float invulnerabilityTime = 1f;
    [SerializeField] private float upwardBounceForce = 15f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Currency")]
    [SerializeField] private int currentSouls = 0;
    [SerializeField] private GameObject drop;

    [Header("Dashing")]
    [SerializeField] private float dashingVelocity = 24f;
    [SerializeField] private float dashingTime = 0.02f;
    private Vector2 dashingDir;
    private bool isDashing;
    private bool canDash = true;

    private Rigidbody2D rb2D;

    private HeavyDoor currentInteractableDoor;

    private float moveHorizontal;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool jumpRequested;

    private bool canMove = true;
    private float originalMaxSpeed;

    private bool isInvulnerable = false;

    private int currentHealth;

    private float originalJumpForce;

    private float originalGravityScale;

    private bool isDead = false;

    //public static Vector3 lastDeathPosition;
    //public static int droppedSoulsAmount = 0;
    //public static bool hasDroppedSouls = false;

    [SerializeField] GameObject _gameOverScreen;

    private void Start()
    {
        _gameOverScreen.SetActive(false);
        rb2D = gameObject.GetComponent<Rigidbody2D>();
        rb2D.sleepMode = RigidbodySleepMode2D.NeverSleep;
        originalMaxSpeed = maxMoveSpeed;
        currentHealth = maxHealth;
        originalJumpForce = jumpForce;
        originalGravityScale = rb2D.gravityScale;

        RespawnSouls();

        //if (hasDroppedSouls && drop != null)
        //{
        //    GameObject droppedSouls = Instantiate(drop, lastDeathPosition, Quaternion.identity);

        //    LostSoul soulScript = droppedSouls.GetComponent<LostSoul>();
        //    if (soulScript != null)
        //    {
        //        soulScript.SetSoulValue(droppedSoulsAmount);
        //    }
        //}
    }

    private void Update()
    {
        if (isDead) return;

        if (!canMove || isDashing)
        {
            moveHorizontal = 0f;
            return;
        }

        moveHorizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            jumpRequested = true;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(UseHealingFlask());
        }

        if (Input.GetKeyDown(KeyCode.E) && currentInteractableDoor != null)
        {
            StartCoroutine(PerformInteraction());
        }

        if (Input.GetButtonDown("Dash") && canDash)
        {
            StartCoroutine(Dash());
        }

        TurnCheck();

    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isDashing) return;

        if (feetCollider != null)
        {
            isGrounded = feetCollider.IsTouchingLayers(groundLayer);
        }

 
        float targetSpeed = moveHorizontal * maxMoveSpeed;
        float currentSpeed = rb2D.linearVelocity.x;

 
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

    
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

        rb2D.linearVelocity = new Vector2(newSpeed, rb2D.linearVelocity.y);
 

        if (jumpRequested)
        {
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }

        if (rb2D.linearVelocity.y < terminalVelocity)
        {
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, terminalVelocity);
        }

        if (isGrounded)
        {
            canDash = true;
        }

        CameraYDampingCheck();
    }

    private void TurnCheck()
    {
        if (moveHorizontal > 0 && !isFacingRight)
        {
            Turn();
        }
        else if (moveHorizontal < 0 && isFacingRight)
        {
            Turn();
        }
    }

    private void Turn()
    {
        isFacingRight = !isFacingRight;

        float yRotation = isFacingRight ? 0f : 180f;
        Vector3 rotator = new Vector3(transform.rotation.x, yRotation, transform.rotation.z);
        transform.rotation = Quaternion.Euler(rotator);

        if (cameraFollowObject != null)
        {
            cameraFollowObject.CallTurn();
        }
    }

    private void CameraYDampingCheck()
    {
        if (rb2D == null || CameraManager.instance == null) return;

        if (rb2D.linearVelocity.y < CameraManager.instance.FallSpeedYDampingChangeThreshold && !CameraManager.instance.LerpedFromPlayerFalling && !CameraManager.instance.IsLerpingYDamping)
        {
            CameraManager.instance.LerpYDamping(true);
        }

        if (rb2D.linearVelocity.y >= 0f && CameraManager.instance.LerpedFromPlayerFalling && !CameraManager.instance.IsLerpingYDamping)
        {
            CameraManager.instance.LerpYDamping(false);
        }
    }

    public bool IsFacingRight { get { return isFacingRight; } }

    private IEnumerator UseHealingFlask()
    {
        canMove = false;
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        yield return new WaitForSeconds(healTime);
        canMove = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHazardCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleHazardCollision(collision);
    }

    private void HandleHazardCollision(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Hazard") && !isInvulnerable)
        {
            if (isDashing)
            {
                isDashing = false;
                rb2D.gravityScale = originalGravityScale;
            }
            canMove = false;
            isInvulnerable = true;

            TakeDamage(1);

            if (isDead) return;

            Vector2 contactNormal = collision.GetContact(0).normal;
            rb2D.linearVelocity = Vector2.zero;

            if (contactNormal.y > 0.5f)
            {
                rb2D.AddForce(new Vector2(0f, upwardBounceForce), ForceMode2D.Impulse);
            }
            else
            {
                float knockbackDirection = transform.position.x < collision.transform.position.x ? -1f : 1f;
                rb2D.AddForce(new Vector2(damageKnockback.x * knockbackDirection, damageKnockback.y), ForceMode2D.Impulse);
            }

            StartCoroutine(StunRecovery());
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        float elapsedTime = 0f;
        bool isVisible = true;

        while (elapsedTime < invulnerabilityTime)
        {
            isVisible = !isVisible;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        isInvulnerable = false;
    }
    private IEnumerator StunRecovery()
    {
        yield return new WaitForSeconds(0.5f);
        canMove = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Swamp"))
        {
            maxMoveSpeed = originalMaxSpeed * swampSpeedMultiplier;
            jumpForce = originalJumpForce * swampJumpMultiplier;
        }
        else if (collision.gameObject.CompareTag("GravityZone"))
        {
            rb2D.gravityScale = alteredGravityScale;
        }
        else if (collision.gameObject.CompareTag("Interactable"))
        {
            currentInteractableDoor = collision.GetComponent<HeavyDoor>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Swamp"))
        {
            maxMoveSpeed = originalMaxSpeed;
            jumpForce = originalJumpForce;
        }
        else if (collision.gameObject.CompareTag("GravityZone"))
        {
            rb2D.gravityScale = originalGravityScale;
        }
        else if (collision.gameObject.CompareTag("Interactable"))
        {
            if (currentInteractableDoor != null && currentInteractableDoor == collision.GetComponent<HeavyDoor>())
            {
                currentInteractableDoor = null;
            }
        }
    }

    private void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void RespawnSouls()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasDroppedSouls && drop != null)
        {
            GameObject droppedSouls = Instantiate(drop, GameManager.Instance.LastDeathPosition, Quaternion.identity);
            LostSoul soulScript = droppedSouls.GetComponent<LostSoul>();
            if (soulScript != null)
            {
                soulScript.SetSoulValue(GameManager.Instance.DroppedSoulsAmount);
            }
        }
    }

    private void Die()
    {
        isDead = true;
        //gameObject.SetActive(false);

        //if (currentSouls > 0)
        //{
        //    lastDeathPosition = transform.position;
        //    droppedSoulsAmount = currentSouls;
        //    hasDroppedSouls = true;
        //    currentSouls = 0;
        //}
        //else
        //{
        //    hasDroppedSouls = false;
        //}

        if (currentSouls > 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayerDeath(transform.position, currentSouls);
            }
            currentSouls = 0;
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearDroppedSouls();
            }
        }

        //_gameOverScreen.SetActive(true);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        _gameOverScreen.SetActive(true);
        rb2D.simulated = false;
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddSouls(int amount)
    {
        currentSouls += amount;
    }

    private IEnumerator PerformInteraction()
    {
        canMove = false;
        rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);

        currentInteractableDoor.Interact();

        yield return new WaitForSeconds(1.5f);

        canMove = true;
        currentInteractableDoor = null;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb2D.gravityScale;
        rb2D.gravityScale = 0f;

        dashingDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (dashingDir == Vector2.zero)
        {
            dashingDir = new Vector2(isFacingRight ? 1 : -1, 0);
        }

        rb2D.linearVelocity = dashingDir.normalized * dashingVelocity;

        yield return new WaitForSeconds(dashingTime);
        rb2D.gravityScale = originalGravity;
        isDashing = false;
    }
}
