using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Mechanics")]
    [SerializeField] private float terminalVelocity = -12f;
    [SerializeField] private float healTime = 0.5f;
    [SerializeField] private Vector2 damageKnockback = new Vector2(5f, 5f);

    [SerializeField] private float invulnerabilityTime = 1f;
    [SerializeField] private float upwardBounceForce = 15f;

    [Header("Health")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Heal Visuals")]
    [SerializeField] private GameObject healObject;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleAnimationState = "Idle";
    [SerializeField] private string runAnimationState = "Run";
    [SerializeField] private string jumpAnimationState = "jump";
    [SerializeField] private string jumpToFallAnimationState = "JumptoFall";
    [SerializeField] private string fallAnimationState = "Fall";
    [SerializeField] private string dashAnimationState = "Dash";
    [SerializeField] private string dashAttackAnimationState = "Dash-Attack";
    [SerializeField] private string wallSlideAnimationState = "Wall-Slide";
    [SerializeField] private string attackAnimationState = "Attack";
    [SerializeField] private string hurtAnimationState = "Hurt";
    [SerializeField] private string deathAnimationState = "Death";
    [SerializeField] private float animationMoveThreshold = 0.05f;
    [SerializeField] private float attackAnimationClipLength = 1.2f;
    [SerializeField] private float dashAttackAnimationClipLength = 1f;
    [SerializeField] private float jumpToFallAnimationClipLength = 0.2f;
    [SerializeField] private float jumpToFallVelocityThreshold = 0.05f;
    [SerializeField] private string blockAnimationState = "Block";

    [Header("Stamina")]
    [SerializeField] private UnityEngine.UI.Slider staminaBar;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 35f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [SerializeField] private float dashStaminaCost = 25f;
    [SerializeField] private float jumpStaminaCost = 10f;
    [SerializeField] private float parryStaminaCost = 25f;
    [SerializeField] private float attackStaminaCost = 15f;

    [SerializeField] private float currentStamina;
    [SerializeField] private float lastStaminaUse;

    [Header("Currency")]
    [SerializeField] TextMeshProUGUI coinCount;
    [SerializeField] private int currentSouls = 0;
    [SerializeField] private GameObject drop;
    [SerializeField] private float coinTweenDuration = 1f;
    private int displayedSouls = 0;
    private Coroutine coinTweenRoutine;

    [Header("Dashing")]
    [SerializeField] private float dashingVelocity = 24f;
    [SerializeField] private float dashingTime = 0.02f;
    [SerializeField] private float dashInvulnerabilityTime = 0.2f;
    [SerializeField] private float dashAttackInputWindow = 0.25f;
    [SerializeField] private float dashAttackStaminaCost = 15f;
    private Vector2 dashingDir;
    private bool isDashing;
    private bool canDash = true;

    [Header("Wall Slide")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private float wallSlideMaxFallSpeed = 3f;
    [SerializeField] private float wallSlideInputThreshold = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource wallSlideSource;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip parryClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip coinPickupClip;

    [SerializeField] private AudioClip healClip;

    private Rigidbody2D rb2D;

    private HeavyDoor currentInteractableDoor;

    private float moveHorizontal;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool jumpRequested;

    private int maxJumps = 2;
    private int jumpsRemaining = 2;

    private bool canMove = true;

    private bool isInvulnerable = false;
    private bool isDashInvulnerable = false;

    private int currentHealth;

    private float originalGravityScale;

    private bool isDead = false;

    [SerializeField] GameObject _gameOverScreen;

    private PlayerCombat combatScript;
    private List<InputAction> ownedInputActions = new List<InputAction>();
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction healAction;
    private InputAction blockAction;
    private Vector2 moveInput;
    private string currentAnimationState;
    private int lastAnimatedAttackSequence;
    private int lastAnimatedDashAttackSequence;
    private bool hasMovedUpThisAirborne;
    private bool hasPlayedJumpToFallThisAirborne;
    private float jumpToFallEndsAt;
    private bool isWallSliding;
    private float lastDashStartedAt = float.NegativeInfinity;
    private bool hasUsedDashAttackThisDash;
    private Coroutine dashInvulnerabilityCoroutine;

    private PlayerInventory inventory;

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
        isDashInvulnerable = false;
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
        InitializeAnimationReferences();
        currentHealth = maxHealth;

        inventory = GetComponent<PlayerInventory>();

        InitializeBar(healthBar, maxHealth);
        UpdateHealthBar();

        currentStamina = maxStamina;
        InitializeBar(staminaBar, maxStamina);
        UpdateStaminaBar();

        originalGravityScale = rb2D.gravityScale;
        if (wallLayer.value == 0)
        {
            wallLayer = groundLayer | LayerMask.GetMask("Default");
        }

        RespawnSouls();

        if (GameManager.Instance != null && GameManager.Instance.HasPersistedPlayerState)
        {
            currentSouls = GameManager.Instance.PersistedSouls;
        }

        UpdateCoinText();
    }

    private void Update()
    {
        if (isDead)
        {
            UpdateAnimationState();
            return;
        }

        UpdateBlocking();
        HandleStaminaRegen();

        moveInput = moveAction.ReadValue<Vector2>();
        moveHorizontal = moveInput.x;

        if (combatScript.IsBlocking && isGrounded && !isDashing)
        {
            moveHorizontal = 0f;
        }
        UpdateAnimationState();

        if (TryStartDashAttack())
        {
            UpdateAnimationState();
            return;
        }

        if (isDashing) return;

        TryQueueAttack();

        if (!canMove)
        {
            moveHorizontal = 0f;
            UpdateAnimationState();
            return;
        }

        if (jumpAction.WasPressedThisFrame() && !isDashing && jumpsRemaining > 0 && canConsumeStamina(jumpStaminaCost))
        {
            jumpRequested = true;
            PlayClip(jumpClip);
        }

        if (healAction.WasPressedThisFrame() && currentHealth < maxHealth)
        {
            inventory.ConsumeFirstHealthPotion();
        }

        if (interactAction.WasPressedThisFrame() && currentInteractableDoor != null)
        {
            StartCoroutine(PerformInteraction());
        }

        if (dashAction.WasPressedThisFrame() && canDash && canConsumeStamina(dashStaminaCost))
        {
            StartCoroutine(Dash());
        }

        TurnCheck();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isDashing)
        {
            isWallSliding = false;
            return;
        }

        if (feetCollider != null)
        {
            isGrounded = feetCollider.IsTouchingLayers(groundLayer);
        }
        UpdateWallSlideState();

        bool isTouchingWallAhead = false;
        float pushCheckDistance = 0.02f;

        Vector2 checkDir = new Vector2(moveHorizontal > 0 ? 1 : -1, 0);
        LayerMask solidObstacle = enemyLayer | wallLayer;

        Collider2D playerCollider = bodyCollider;
        Vector2 boxCastSize = new Vector2(playerCollider.bounds.size.x * 0.9f, playerCollider.bounds.size.y * 0.8f);

        RaycastHit2D hit = Physics2D.BoxCast(
            playerCollider.bounds.center,
            boxCastSize,
            0f,
            checkDir,
            pushCheckDistance,
            solidObstacle
        );


        if (hit.collider != null && moveHorizontal != 0)
        {
            float directionToEnemy = Mathf.Sign(hit.collider.transform.position.x - transform.position.x);
            float movingDirection = Mathf.Sign(moveHorizontal);

            if (directionToEnemy == movingDirection)
            {
                isTouchingWallAhead = true;
            }

        }
 
        float targetSpeed = moveHorizontal * maxMoveSpeed;
        float currentSpeed = rb2D.linearVelocity.x;

        if (isTouchingWallAhead)
        {
            targetSpeed = 0f;
            currentSpeed = 0f;
        }

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

        if (isWallSliding && rb2D.linearVelocity.y < -wallSlideMaxFallSpeed)
        {
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -wallSlideMaxFallSpeed);
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
        if ((moveHorizontal > 0 && !isFacingRight) || (moveHorizontal < 0 && isFacingRight))
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
        CameraManager cam = CameraManager.instance;
        if (cam.IsLerpingYDamping) return;

        float vy = rb2D.linearVelocity.y;
        if (vy < cam.FallSpeedYDampingChangeThreshold && !cam.LerpedFromPlayerFalling)
            cam.LerpYDamping(true);
        else if (vy >= 0f && cam.LerpedFromPlayerFalling)
            cam.LerpYDamping(false);
    }

    public bool IsFacingRight => isFacingRight;
    public float VerticalInput => moveInput.y;
    private bool IsCurrentlyInvulnerable => isInvulnerable || isDashInvulnerable;

    private void UpdateWallSlideState()
    {
        isWallSliding = false;

        if (rb2D == null || bodyCollider == null || isGrounded || !canMove || Mathf.Abs(moveHorizontal) < wallSlideInputThreshold)
        {
            UpdateWallSlideAudio();
            return;
        }

        if (rb2D.linearVelocity.y >= 0f)
        {
            UpdateWallSlideAudio();
            return;
        }

        int moveDirection = moveHorizontal > 0f ? 1 : -1;
        isWallSliding = IsTouchingWall(moveDirection);

        UpdateWallSlideAudio();
    }

    private void UpdateWallSlideAudio()
    {
        if (wallSlideSource == null) return;

        if (isWallSliding && !wallSlideSource.isPlaying)
        {
            wallSlideSource.Play();
        }
        else if (!isWallSliding && wallSlideSource.isPlaying)
        {
            wallSlideSource.Stop();
        }
    }

    private bool IsTouchingWall(int direction)
    {
        int checkedWallLayer = (wallLayer.value != 0 ? wallLayer.value : groundLayer.value) | LayerMask.GetMask("Default");
        Bounds bodyBounds = bodyCollider.bounds;
        Vector2 boxCastSize = new Vector2(bodyBounds.size.x * 0.9f, bodyBounds.size.y * 0.8f);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bodyBounds.center,
            boxCastSize,
            0f,
            new Vector2(direction, 0f),
            wallCheckDistance,
            checkedWallLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider != bodyCollider)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryStartDashAttack()
    {
        if (!attackAction.WasPressedThisFrame() || combatScript == null || !combatScript.CanStartDashAttack)
        {
            return false;
        }

        bool isInDashAttackWindow = isDashing || Time.time <= lastDashStartedAt + dashAttackInputWindow;
        if (!isInDashAttackWindow || (!isDashing && !canMove) || hasUsedDashAttackThisDash || !canConsumeStamina(dashAttackStaminaCost))
        {
            return false;
        }

        hasUsedDashAttackThisDash = true;
        combatScript.StartDashAttack();
        return true;
    }

    private void TryQueueAttack()
    {
        if (!attackAction.WasPressedThisFrame() || combatScript == null || !combatScript.CanQueueAttack)
        {
            return;
        }

        bool canStartOrContinueAttack = canMove || combatScript.isAttacking;
        if (!canStartOrContinueAttack || !canConsumeStamina(attackStaminaCost))
        {
            return;
        }

        combatScript.QueueAttack();
    }

    private void InitializeAnimationReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        SpriteRenderer animatedSpriteRenderer = animator.GetComponent<SpriteRenderer>();
        if (animatedSpriteRenderer != null)
        {
            spriteRenderer = animatedSpriteRenderer;
        }

        attackAnimationClipLength = GetAnimationClipLength(attackAnimationState, attackAnimationClipLength);
        dashAttackAnimationClipLength = GetAnimationClipLength(dashAttackAnimationState, dashAttackAnimationClipLength);
        jumpToFallAnimationClipLength = GetAnimationClipLength(jumpToFallAnimationState, jumpToFallAnimationClipLength);
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
        {
            return;
        }

        if (isDead)
        {
            PlayAnimationState(deathAnimationState);
            return;
        }

        if (combatScript != null && combatScript.IsDashAttacking)
        {
            float dashAttackDuration = Mathf.Max(combatScript.DashAttackDuration, 0.01f);
            bool isNewDashAttack = lastAnimatedDashAttackSequence != combatScript.DashAttackSequence;
            PlayAnimationState(dashAttackAnimationState, dashAttackAnimationClipLength / dashAttackDuration, isNewDashAttack);
            lastAnimatedDashAttackSequence = combatScript.DashAttackSequence;
            return;
        }

        if (combatScript != null && combatScript.isAttacking)
        {
            float attackDuration = Mathf.Max(combatScript.AttackDuration, 0.01f);
            bool isNewAttack = lastAnimatedAttackSequence != combatScript.AttackSequence;
            PlayAnimationState(attackAnimationState, attackAnimationClipLength / attackDuration, isNewAttack);
            lastAnimatedAttackSequence = combatScript.AttackSequence;
            return;
        }

        if (isDashing)
        {
            PlayAnimationState(dashAnimationState);
            return;
        }

        if (isInvulnerable && !canMove)
        {
            PlayAnimationState(hurtAnimationState);
            return;
        }

        if (combatScript != null && combatScript.IsBlocking)
        {
            PlayAnimationState(blockAnimationState);
            return;
        }

        if (isWallSliding)
        {
            hasPlayedJumpToFallThisAirborne = true;
            jumpToFallEndsAt = 0f;
            PlayAnimationState(wallSlideAnimationState);
            return;
        }

        float verticalVelocity = rb2D != null ? rb2D.linearVelocity.y : 0f;
        if (!isGrounded)
        {
            if (verticalVelocity > jumpToFallVelocityThreshold)
            {
                hasMovedUpThisAirborne = true;
                hasPlayedJumpToFallThisAirborne = false;
                PlayAnimationState(jumpAnimationState);
                return;
            }

            if (hasMovedUpThisAirborne && !hasPlayedJumpToFallThisAirborne)
            {
                hasPlayedJumpToFallThisAirborne = true;
                jumpToFallEndsAt = Time.time + jumpToFallAnimationClipLength;
                PlayAnimationState(jumpToFallAnimationState);
                return;
            }

            if (hasPlayedJumpToFallThisAirborne && Time.time < jumpToFallEndsAt)
            {
                PlayAnimationState(jumpToFallAnimationState);
                return;
            }

            PlayAnimationState(fallAnimationState);
            return;
        }

        hasMovedUpThisAirborne = false;
        hasPlayedJumpToFallThisAirborne = false;
        jumpToFallEndsAt = 0f;

        if (Mathf.Abs(moveHorizontal) > animationMoveThreshold)
        {
            PlayAnimationState(runAnimationState);
            return;
        }

        PlayAnimationState(idleAnimationState);
    }

    private float GetAnimationClipLength(string clipName, float fallbackLength)
    {
        if (animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(clipName))
        {
            return fallbackLength;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }

        return fallbackLength;
    }

    private void PlayAnimationState(string stateName, float playbackSpeed = 1f, bool forceRestart = false)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        if (currentAnimationState == stateName && !forceRestart)
        {
            if (animator.speed != playbackSpeed)
            {
                animator.speed = playbackSpeed;
            }
            return;
        }

        animator.speed = playbackSpeed;
        animator.Play(stateName, 0, 0f);
        currentAnimationState = stateName;
    }

    private void InitializeInputActions()
    {
        InputAction move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction = RegisterOwnedAction(move);

        jumpAction = RegisterOwnedAction(CreateButtonAction("Jump", "<Keyboard>/space"));
        dashAction = RegisterOwnedAction(CreateButtonAction("Dash", "<Keyboard>/leftShift"));
        attackAction = RegisterOwnedAction(CreateButtonAction("Attack", "<Mouse>/leftButton"));
        blockAction = RegisterOwnedAction(CreateButtonAction("Block", "<Mouse>/rightButton"));
        interactAction = RegisterOwnedAction(CreateButtonAction("Interact", "<Keyboard>/e"));
        healAction = RegisterOwnedAction(CreateButtonAction("Heal", "<Keyboard>/h"));
    }

    private InputAction RegisterOwnedAction(InputAction action)
    {
        ownedInputActions.Add(action);
        return action;
    }

    private InputAction CreateButtonAction(string actionName, string binding)
    {
        InputAction action = new InputAction(actionName, InputActionType.Button);
        action.AddBinding(binding);
        return action;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private static void InitializeBar(UnityEngine.UI.Slider bar, float max)
    {
        if (bar == null) return;
        bar.minValue = 0f;
        bar.maxValue = max;
        bar.interactable = false;
        if (bar.handleRect != null) bar.handleRect.gameObject.SetActive(false);
    }

    private static void UpdateBar(UnityEngine.UI.Slider bar, float current, float max)
    {
        if (bar == null) return;
        float clamped = Mathf.Clamp(current, 0f, max);
        bar.value = clamped;
        if (bar.fillRect != null) bar.fillRect.gameObject.SetActive(clamped > 0f);
    }

    private void EnableGameplayInput()
    {
        if (moveAction == null) InitializeInputActions();
        foreach (InputAction action in ownedInputActions) action.Enable();
    }

    private void DisableGameplayInput()
    {
        foreach (InputAction action in ownedInputActions) action.Disable();
    }

    private void DisposeOwnedInputActions()
    {
        foreach (InputAction action in ownedInputActions)
        {
            action.Disable();
            action.Dispose();
        }
        ownedInputActions.Clear();
    }

    public bool TryUseHealingItem(int amount)
    {
        if (!canMove) return false;
        StartCoroutine(UseHealingFlask(amount));
        return true;
    }

    private IEnumerator UseHealingFlask(int amount)
    {
        SetMovementLocked(true);

        PlayClip(healClip);

        if (healObject != null) healObject.SetActive(true);
        yield return new WaitForSeconds(healTime);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
        if (healObject != null) healObject.SetActive(false);
        SetMovementLocked(false);
    }

    public bool SpendSouls(int amount)
    {
        if (currentSouls >= amount)
        {
            currentSouls -= amount;
            UpdateCoinText();
            return true;
        }
        return false;
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
        if (collision.gameObject.CompareTag("Hazard") && !IsCurrentlyInvulnerable)
        {
            if (isDashing)
            {
                isDashing = false;
                rb2D.gravityScale = originalGravityScale;
            }
            SetMovementLocked(true);
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
        SetMovementLocked(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Interactable"))
        {
            currentInteractableDoor = collision.GetComponent<HeavyDoor>();
        }
        else if (collision.gameObject.CompareTag("EnemySword") && !IsCurrentlyInvulnerable)
        {
            HandleEnemySwordHit(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Interactable"))
        {
            if (currentInteractableDoor != null && currentInteractableDoor == collision.GetComponent<HeavyDoor>())
            {
                currentInteractableDoor = null;
            }
        }
    }

    private void HandleEnemySwordHit(Collider2D collision)
    {
        if (isDashing)
        {
            isDashing = false;
            rb2D.gravityScale = originalGravityScale;
        }

        Transform attackerTransform = collision.transform.parent != null ? collision.transform.parent : collision.transform;
        float attackDirectionX = attackerTransform.position.x - transform.position.x;

        if (TryParryAttack(collision, attackDirectionX))
        {
            return;
        }

        SetMovementLocked(true);
        isInvulnerable = true;

        BossWeaponDamage weaponDamage = collision.GetComponent<BossWeaponDamage>();
        int enemySwordDamage = 0;
        if (weaponDamage != null)
        {
            enemySwordDamage = weaponDamage.Damage;
        }
        else
        {
            EnemyWeaponDamage contactDamage = collision.GetComponentInParent<EnemyWeaponDamage>();
            if (contactDamage != null) enemySwordDamage = contactDamage.Damage;
        }

        TakeDamage(enemySwordDamage, weaponDamage != null);
        if (isDead) return;

        rb2D.linearVelocity = Vector2.zero;
        float knockbackDirection = transform.position.x < collision.transform.position.x ? -1f : 1f;
        rb2D.AddForce(new Vector2(damageKnockback.x * knockbackDirection, damageKnockback.y), ForceMode2D.Impulse);

        StartCoroutine(StunRecovery());
        StartCoroutine(InvulnerabilityRoutine());
    }

    private bool TryParryAttack(Collider2D collision, float attackDirectionX)
    {
        if (!combatScript.TryBlockAttack(attackDirectionX, isFacingRight)) return false;

        if (!canConsumeStamina(parryStaminaCost))
        {
            combatScript.SetBlocking(false, false);
            return false;
        }

        PlayClip(parryClip);

        float facingDirectionX = isFacingRight ? 1f : -1f;
        bool isBossAttack = collision.GetComponent<BossWeaponDamage>() != null;

        if (isBossAttack)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.AddForce(new Vector2(damageKnockback.x * -facingDirectionX * 0.5f, 0f), ForceMode2D.Impulse);
        }
        else
        {
            Rigidbody2D enemyRb = collision.GetComponentInParent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
                enemyRb.AddForce(new Vector2(damageKnockback.x * facingDirectionX * 0.75f, 0f), ForceMode2D.Impulse);
            }
        }
        return true;
    }

    private void TakeDamage(int damageAmount, bool isBoss = false)
    {
        PlayClip(hurtClip);

        currentHealth = Mathf.Max(currentHealth - damageAmount, 0);
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            if (isBoss)
            {
                Audio audioScript = GetComponentInChildren<Audio>();
                if (audioScript != null)
                {
                    audioScript.isBossDeath = true;
                }
            }
            Die(isBoss);
        }
    }

    private void UpdateHealthBar() => UpdateBar(healthBar, currentHealth, maxHealth);

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

    private void Die(bool isBoss = false)
    {
        isDead = true;

        if (GameManager.Instance != null)
        {
            if (currentSouls > 0) GameManager.Instance.RegisterPlayerDeath(transform.position, currentSouls);
            else GameManager.Instance.ClearDroppedSouls();
        }
        currentSouls = 0;

        PersistPlayerState();
        StartCoroutine(GameOverRoutine(isBoss));
    }

    private void PersistPlayerState()
    {
        if (GameManager.Instance == null) return;

        PlayerStats stats = GetComponent<PlayerStats>();
        PlayerInventory inv = GetComponent<PlayerInventory>();

        GameManager.Instance.SavePlayerState(
            currentSouls,
            stats != null ? stats.HealthLevel : 0,
            stats != null ? stats.StaminaLevel : 0,
            stats != null ? stats.DamageLevel : 0,
            inv != null ? inv.items : null
        );
    }

    private IEnumerator GameOverRoutine(bool isBoss)
    {
        _gameOverScreen.SetActive(true);
        rb2D.simulated = false;

        float waitTime = isBoss ? 8.5f : 2f;
        yield return new WaitForSecondsRealtime(waitTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TeleportToStart()
    {
        PersistPlayerState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public int CurrentSouls => currentSouls;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
    public UnityEngine.UI.Slider HealthBar => healthBar;
    public UnityEngine.UI.Slider StaminaBar => staminaBar;

    public void SetCombatEnabled(bool enabled)
    {
        if (enabled) { attackAction?.Enable(); blockAction?.Enable(); }
        else { attackAction?.Disable(); blockAction?.Disable(); }
    }

    public void AddMaxHealthBonus(int amount)
    {
        maxHealth += amount;
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
        }

        UpdateHealthBar();
    }

    public void AddMaxStaminaBonus(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;

        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
        }

        UpdateStaminaBar();
    }

    public void AddSouls(int amount)
    {
        PlayClip(coinPickupClip);
        currentSouls += amount;

        if (coinTweenRoutine != null) StopCoroutine(coinTweenRoutine);
        coinTweenRoutine = StartCoroutine(TweenCoinDisplay());
    }

    private IEnumerator TweenCoinDisplay()
    {
        int start = displayedSouls;
        int target = currentSouls;

        if (coinTweenDuration <= 0f || start == target)
        {
            displayedSouls = target;
            if (coinCount != null) coinCount.text = displayedSouls.ToString();
            coinTweenRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < coinTweenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / coinTweenDuration);
            displayedSouls = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            if (coinCount != null) coinCount.text = displayedSouls.ToString();
            yield return null;
        }

        displayedSouls = target;
        if (coinCount != null) coinCount.text = displayedSouls.ToString();
        coinTweenRoutine = null;
    }

    private IEnumerator PerformInteraction()
    {
        SetMovementLocked(true);

        currentInteractableDoor.Interact();

        yield return new WaitForSeconds(1.5f);

        SetMovementLocked(false);
        currentInteractableDoor = null;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        lastDashStartedAt = Time.time;
        hasUsedDashAttackThisDash = false;
        StartDashInvulnerability();

        PlayClip(dashClip);

        float originalGravity = rb2D.gravityScale;
        rb2D.gravityScale = 0f;

        dashingDir = moveInput;
        if (dashingDir.sqrMagnitude < 0.01f)
        {
            dashingDir = new Vector2(isFacingRight ? 1 : -1, 0);
        }

        rb2D.linearVelocity = dashingDir.normalized * dashingVelocity;

        yield return new WaitForSeconds(dashingTime);
        rb2D.gravityScale = originalGravity;
        isDashing = false;
    }

    private void StartDashInvulnerability()
    {
        if (dashInvulnerabilityTime <= 0f)
        {
            return;
        }

        if (dashInvulnerabilityCoroutine != null)
        {
            StopCoroutine(dashInvulnerabilityCoroutine);
        }

        dashInvulnerabilityCoroutine = StartCoroutine(DashInvulnerabilityRoutine());
    }

    private IEnumerator DashInvulnerabilityRoutine()
    {
        isDashInvulnerable = true;
        yield return new WaitForSeconds(dashInvulnerabilityTime);
        isDashInvulnerable = false;
        dashInvulnerabilityCoroutine = null;
    }

    private void HandleStaminaRegen()
    {
        if (currentStamina < maxStamina && Time.time >= lastStaminaUse + staminaRegenDelay)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        UpdateStaminaBar();
    }

    private void UpdateBlocking()
    {
        bool isHoldingBlock = blockAction != null && blockAction.ReadValue<float>() > 0.1f;
        combatScript.SetBlocking(isHoldingBlock, currentStamina > 0f);

        if (combatScript.IsBlocking && canMove && isGrounded && !isDashing)
        {
            currentStamina -= 10f * Time.deltaTime;
            lastStaminaUse = Time.time;
            UpdateStaminaBar();
        }
    }

    public bool canConsumeStamina(float amount)
    {
        if (currentStamina > 0f)
        {
            currentStamina -= amount;
            lastStaminaUse = Time.time;

            UpdateStaminaBar();
            return true;
        }
        return false;
    }

    private void UpdateCoinText()
    {
        displayedSouls = currentSouls;

        if (coinTweenRoutine != null)
        {
            StopCoroutine(coinTweenRoutine);
            coinTweenRoutine = null;
        }

        if (coinCount != null)
        {
            coinCount.text = currentSouls.ToString();
        }
        else
        {
            Debug.LogWarning("Coin Count UI is not assigned in the Inspector!");
        }
    }

    private void UpdateStaminaBar() => UpdateBar(staminaBar, currentStamina, maxStamina);

    public bool IsGrounded => isGrounded;
    public float UpwardBounceForce => upwardBounceForce;

    private int movementLockCount = 0;

    public void SetMovementLocked(bool locked)
    {
        if (locked)
        {
            movementLockCount++;
            if (rb2D != null)
            {
                rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
            }
        }
        else if (movementLockCount > 0)
        {
            movementLockCount--;
        }
        canMove = movementLockCount == 0;
    }

    public void PlayFootstep() => PlayClip(walkClip);

}
