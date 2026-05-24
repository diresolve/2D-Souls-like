using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private string blockActionName = "Block";

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

    [SerializeField] private float currentStamina;
    [SerializeField] private float lastStaminaUse;

    [Header("Currency")]
    [SerializeField] TextMeshProUGUI coinCount;
    [SerializeField] private int currentSouls = 0;
    [SerializeField] private GameObject drop;

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

    private Rigidbody2D rb2D;

    private HeavyDoor currentInteractableDoor;

    private float moveHorizontal;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool jumpRequested;

    private int maxJumps = 2;
    private int jumpsRemaining = 2;

    private bool canMove = true;
    private float originalMaxSpeed;

    private bool isInvulnerable = false;
    private bool isDashInvulnerable = false;

    private int currentHealth;

    private float originalJumpForce;

    private float originalGravityScale;

    private bool isDead = false;

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
        originalMaxSpeed = maxMoveSpeed;
        currentHealth = maxHealth;

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

        currentStamina = maxStamina;

        if (staminaBar != null)
        {
            staminaBar.minValue = 0f;
            staminaBar.maxValue = maxStamina;
            staminaBar.interactable = false;
            if (staminaBar.handleRect != null)
            {
                staminaBar.handleRect.gameObject.SetActive(false);
            }
            UpdateStaminaBar();
        }

        originalJumpForce = jumpForce;
        originalGravityScale = rb2D.gravityScale;
        if (wallLayer.value == 0)
        {
            wallLayer = groundLayer | LayerMask.GetMask("Default");
        }

        RespawnSouls();
        UpdateCoinText();
    }

    private void Update()
    {
        if (isDead)
        {
            UpdateAnimationState();
            return;
        }

        bool isHoldingBlock = blockAction != null && blockAction.ReadValue<float>() > 0.1f;
        combatScript.SetBlocking(isHoldingBlock, currentStamina > 0f);

        if (combatScript.IsBlocking && canMove && isGrounded && !isDashing)
        {
            currentStamina -= 10f * Time.deltaTime;
            lastStaminaUse = Time.time;
            UpdateStaminaBar();

            moveHorizontal *= 0.5f;
        }

        HandleStaminaRegen();
        ReadMoveInput();
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

        if (WasActionPressedThisFrame(dashAction) && canDash && currentStamina > 0f)
        {
            if (canConsumeStamina(dashStaminaCost))
            {
                StartCoroutine(Dash());
            }
            
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
        // pokusaj fixanja

        bool isTouchingWallAhead = false;
        //float pushCheckDistance = 0.05f;
        float pushCheckDistance = 0.02f;

        Vector2 checkDir = new Vector2(moveHorizontal > 0 ? 1 : -1, 0);
        LayerMask solidObstacle = enemyLayer | wallLayer;

        Collider2D playerCollider = bodyCollider;
        //Vector2 boxCastSize = new Vector2(playerCollider.bounds.size.x * 0.9f, playerCollider.bounds.size.y * 0.8f);
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

        // ovo se isto dodalo
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
    private bool IsCurrentlyInvulnerable { get { return isInvulnerable || isDashInvulnerable; } }

    private void UpdateWallSlideState()
    {
        isWallSliding = false;

        if (rb2D == null || bodyCollider == null || isGrounded || !canMove || Mathf.Abs(moveHorizontal) < wallSlideInputThreshold)
        {
            return;
        }

        if (rb2D.linearVelocity.y >= 0f)
        {
            return;
        }

        int moveDirection = moveHorizontal > 0f ? 1 : -1;
        isWallSliding = IsTouchingWall(moveDirection);
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
        if (!WasActionPressedThisFrame(attackAction) || combatScript == null || !combatScript.CanStartDashAttack)
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
        if (!WasActionPressedThisFrame(attackAction) || combatScript == null || !combatScript.CanQueueAttack)
        {
            return;
        }

        bool canStartOrContinueAttack = canMove || combatScript.isAttacking;
        if (!canStartOrContinueAttack || !canConsumeStamina(15f))
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
        InputActionMap playerActionMap = inputActions != null
            ? inputActions.FindActionMap(playerActionMapName, false)
            : null;

        moveAction = FindInputAction(playerActionMap, moveActionName, CreateMoveAction);
        jumpAction = FindInputAction(playerActionMap, jumpActionName, CreateJumpAction);
        dashAction = FindInputAction(playerActionMap, dashActionName, CreateDashAction);
        attackAction = FindInputAction(playerActionMap, attackActionName, CreateAttackAction);
        interactAction = FindInputAction(playerActionMap, interactActionName, CreateInteractAction);
        healAction = FindInputAction(playerActionMap, healActionName, CreateHealAction);
        blockAction = FindInputAction(playerActionMap, blockActionName, CreateBlockAction);
    }

    private InputAction CreateBlockAction()
    {
        return CreateButtonAction(blockActionName, "<Mouse>/rightButton", "<Keyboard>/leftAlt", "<Gamepad>/rightShoulder");
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
        EnableInputAction(blockAction);
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
        if (collision.gameObject.CompareTag("Hazard") && !IsCurrentlyInvulnerable)
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
        else if (collision.gameObject.CompareTag("EnemySword") && !IsCurrentlyInvulnerable)
        {
            if (isDashing)
            {
                isDashing = false;
                rb2D.gravityScale = originalGravityScale;
            }

            Transform attackerTransform = collision.transform.parent != null ? collision.transform.parent : collision.transform;
            float attackDirectionX = attackerTransform.position.x - transform.position.x;

            if (combatScript.TryBlockAttack(attackDirectionX, isFacingRight))
            {
                if (canConsumeStamina(25f))
                {

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

                    return;
                }
                else
                {
                    combatScript.SetBlocking(false, false);
                }
            }

            canMove = false;
            isInvulnerable = true;
            int enemySwordDamage = 25;
            BossWeaponDamage weaponDamage = collision.GetComponent<BossWeaponDamage>();
            if (weaponDamage != null)
            {
                enemySwordDamage = weaponDamage.Damage;
            }

            TakeDamage(enemySwordDamage);
            if (isDead) return;
            rb2D.linearVelocity = Vector2.zero;
            float knockbackDirection = transform.position.x < collision.transform.position.x ? -1f : 1f;
            rb2D.AddForce(new Vector2(damageKnockback.x * knockbackDirection, damageKnockback.y), ForceMode2D.Impulse);

            StartCoroutine(StunRecovery());
            StartCoroutine(InvulnerabilityRoutine());
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
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0);
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
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
        UpdateCoinText();
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
        lastDashStartedAt = Time.time;
        hasUsedDashAttackThisDash = false;
        StartDashInvulnerability();

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
        if (coinCount != null)
        {
            coinCount.text = currentSouls.ToString();
        }
        else
        {
            Debug.LogWarning("Coin Count UI is not assigned in the Inspector!");
        }
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar != null)
        {
            float clampedStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            staminaBar.value = clampedStamina;

            if (staminaBar.fillRect != null)
            {
                staminaBar.fillRect.gameObject.SetActive(clampedStamina > 0f);
            }
        }
    }

    public bool IsGrounded { get { return isGrounded; } }
    public float UpwardBounceForce { get { return upwardBounceForce; } }

}
