using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CameraFollowObject cameraFollowObject;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string dashActionName = "Sprint";
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string healActionName = "Heal";

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
    [SerializeField] private UnityEngine.UI.Slider healthBar;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.1f;

    // stamina system
    [Header("Stamina")]
    [SerializeField] private UnityEngine.UI.Slider staminaBar;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 35f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [SerializeField] private float dashStaminaCost = 25f;

    [SerializeField] private float currentStamina;
    [SerializeField] private float lastStaminaUse;

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

    // for double jump
    private int maxJumps = 2;
    private int jumpsRemaining = 2;

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

    private PlayerCombat combatScript;
    private List<InputAction> enabledInputActions = new List<InputAction>();
    private List<InputAction> ownedInputActions = new List<InputAction>();
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction healAction;
    private Vector2 moveInput;

    private void Awake()
    {
        combatScript = GetComponent<PlayerCombat>();
        InitializeInputActions();
    }

    private void OnEnable()
    {
        EnableGameplayInput();
    }

    private void OnDisable()
    {
        DisableGameplayInput();
    }

    private void OnDestroy()
    {
        DisposeOwnedInputActions();
    }

    private void Start()
    {
        _gameOverScreen.SetActive(false);
        rb2D = gameObject.GetComponent<Rigidbody2D>();
        rb2D.sleepMode = RigidbodySleepMode2D.NeverSleep;
        originalMaxSpeed = maxMoveSpeed;
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        currentStamina = maxStamina;

        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }

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

        HandleStaminaRegen();
        ReadMoveInput();

        if (isDashing) return;

        if (!canMove)
        {
            moveHorizontal = 0f;
            return;
        }

        // to do: moze skocit s enemyja samo ako drugi put skace
        if (WasActionPressedThisFrame(jumpAction) /*&& isGrounded*/ && !isDashing && jumpsRemaining > 0)
        {
            jumpRequested = true;
        }

        if (WasActionPressedThisFrame(healAction))
        {
            StartCoroutine(UseHealingFlask());
        }

        if (WasActionPressedThisFrame(interactAction) && currentInteractableDoor != null)
        {
            StartCoroutine(PerformInteraction());
        }

        if (WasActionPressedThisFrame(dashAction) && canDash && currentStamina >= dashStaminaCost)
        {
            if (canConsumeStamina(dashStaminaCost))
            {
                StartCoroutine(Dash());
            }
            
        }

        if (WasActionPressedThisFrame(attackAction) && !isDashing /*&& isGrounded && !isDashing*/)
        {
            if (combatScript != null && !combatScript.isAttacking)
            {
                if (canConsumeStamina(15f))
                {
                    StartCoroutine(combatScript.Attack());
                }
            }
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
            jumpsRemaining--;
            jumpRequested = false;
        }

        if (rb2D.linearVelocity.y < terminalVelocity)
        {
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, terminalVelocity);
        }

        if (isGrounded && rb2D.linearVelocity.y <= 0.01f)
        {
            canDash = true;
            jumpsRemaining = maxJumps;

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
    public float VerticalInput { get { return moveInput.y; } }

    private void InitializeInputActions()
    {
        InputActionMap playerActionMap = inputActions != null
            ? inputActions.FindActionMap(playerActionMapName, false)
            : null;

        moveAction = FindInputAction(playerActionMap, moveActionName, CreateMoveAction);
        jumpAction = FindInputAction(playerActionMap, jumpActionName, CreateJumpAction);
        dashAction = FindInputAction(playerActionMap, dashActionName, CreateDashAction);
        attackAction = FindInputAction(playerActionMap, attackActionName, CreateAttackAction);
        interactAction = FindInputAction(playerActionMap, interactActionName, CreateInteractAction);
        healAction = FindInputAction(playerActionMap, healActionName, CreateHealAction);
    }

    private InputAction FindInputAction(InputActionMap actionMap, string actionName, Func<InputAction> fallbackFactory)
    {
        if (actionMap != null && !string.IsNullOrWhiteSpace(actionName))
        {
            InputAction action = actionMap.FindAction(actionName, false);
            if (action != null)
            {
                return action;
            }
        }

        InputAction fallbackAction = fallbackFactory();
        ownedInputActions.Add(fallbackAction);
        return fallbackAction;
    }

    private InputAction CreateMoveAction()
    {
        InputAction action = new InputAction(moveActionName, InputActionType.Value, expectedControlType: "Vector2");

        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        action.AddBinding("<Gamepad>/leftStick");
        action.AddBinding("<Gamepad>/dpad");

        return action;
    }

    private InputAction CreateJumpAction()
    {
        return CreateButtonAction(jumpActionName, "<Keyboard>/space", "<Gamepad>/buttonSouth");
    }

    private InputAction CreateDashAction()
    {
        return CreateButtonAction(dashActionName, "<Keyboard>/leftShift", "<Gamepad>/leftStickPress", "<Gamepad>/rightShoulder");
    }

    private InputAction CreateAttackAction()
    {
        return CreateButtonAction(attackActionName, "<Mouse>/leftButton", "<Keyboard>/leftCtrl", "<Gamepad>/buttonWest");
    }

    private InputAction CreateInteractAction()
    {
        return CreateButtonAction(interactActionName, "<Keyboard>/e", "<Gamepad>/buttonNorth");
    }

    private InputAction CreateHealAction()
    {
        return CreateButtonAction(healActionName, "<Keyboard>/h", "<Gamepad>/selectButton");
    }

    private InputAction CreateButtonAction(string actionName, params string[] bindings)
    {
        InputAction action = new InputAction(actionName, InputActionType.Button);

        foreach (string binding in bindings)
        {
            action.AddBinding(binding);
        }

        return action;
    }

    private void EnableGameplayInput()
    {
        if (moveAction == null)
        {
            InitializeInputActions();
        }

        EnableInputAction(moveAction);
        EnableInputAction(jumpAction);
        EnableInputAction(dashAction);
        EnableInputAction(attackAction);
        EnableInputAction(interactAction);
        EnableInputAction(healAction);
    }

    private void EnableInputAction(InputAction action)
    {
        if (action == null || action.enabled)
        {
            return;
        }

        action.Enable();
        enabledInputActions.Add(action);
    }

    private void DisableGameplayInput()
    {
        foreach (InputAction action in enabledInputActions)
        {
            action.Disable();
        }

        enabledInputActions.Clear();
    }

    private void DisposeOwnedInputActions()
    {
        DisableGameplayInput();

        foreach (InputAction action in ownedInputActions)
        {
            action.Dispose();
        }

        ownedInputActions.Clear();
    }

    private void ReadMoveInput()
    {
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        moveHorizontal = moveInput.x;
    }

    private bool WasActionPressedThisFrame(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

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

            TakeDamage(15);

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

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

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
        //currentStamina -= dashStaminaCost;
        //lastStaminaUse = Time.time;

        canDash = false;
        isDashing = true;

        float originalGravity = rb2D.gravityScale;
        rb2D.gravityScale = 0f;

        //float dashDuration = isGrounded ? dashingTime : 0.05f;

        dashingDir = moveInput;
        if (dashingDir.sqrMagnitude < 0.01f)
        {
            dashingDir = new Vector2(isFacingRight ? 1 : -1, 0);
        }

        rb2D.linearVelocity = dashingDir.normalized * dashingVelocity;

        yield return new WaitForSeconds(dashingTime);
        rb2D.gravityScale = originalGravity;
        //rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x * 0.8f, 0f);
        isDashing = false;
    }

    private void HandleStaminaRegen()
    {
        if (currentStamina < maxStamina && Time.time >= lastStaminaUse + staminaRegenDelay)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        if (staminaBar != null)
        {
            staminaBar.value = currentStamina;
        }
    }

    public void LockMovementForAttack(bool isLocked)
    {
        canMove = !isLocked;
        if (isLocked)
        {
            rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
        }

    }

    public bool canConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            lastStaminaUse = Time.time;

            if (staminaBar != null)
            {
                staminaBar.value = currentStamina;
            }
            return true;
        }
        return false;
    }

    public bool IsGrounded { get { return isGrounded; } }
}
