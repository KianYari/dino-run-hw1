using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    static readonly int GroundedParam = Animator.StringToHash("Grounded");
    static readonly int HitParam = Animator.StringToHash("Hit");
    static readonly int DieParam = Animator.StringToHash("Die");

    [Header("Lives")]
    [SerializeField] int maxLives = 3;
    [SerializeField] float hitInvulnerability = 0.8f;

    [Header("Movement")]
    [SerializeField] float jumpImpulse = 14.5f;
    [SerializeField] float jumpBuffer = 0.12f;

    [Header("Dash")]
    [SerializeField] float dashDuration = 0.35f;
    [SerializeField] float dashCooldown = 1f;
    [SerializeField] float dashSpeedMultiplier = 2f;

    [Header("References")]
    [SerializeField] Collider2D groundCollider;
    [SerializeField] Animator animator;

    Rigidbody2D rb;
    Collider2D body;
    InputAction jumpAction;
    InputAction dashAction;

    bool grounded = true;
    float jumpBufferTimer;
    float lastJumpTime = -1f;
    float dashTimer;
    float dashCooldownTimer;
    float invulnerableTimer;

    public int Lives { get; private set; }
    public int MaxLives => maxLives;
    public bool IsDashing => dashTimer > 0f;
    public float DashMultiplier => IsDashing ? dashSpeedMultiplier : 1f;
    public float DashReady01 => 1f - Mathf.Clamp01(dashCooldownTimer / dashCooldown);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        Lives = maxLives;

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/upArrow");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Keyboard>/rightShift");
        dashAction.AddBinding("<Gamepad>/buttonWest");
    }

    void OnEnable()
    {
        jumpAction.Enable();
        dashAction.Enable();
    }

    void OnDisable()
    {
        jumpAction.Disable();
        dashAction.Disable();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        jumpBufferTimer -= dt;
        dashTimer -= dt;
        dashCooldownTimer -= dt;
        invulnerableTimer -= dt;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Playing && Lives > 0)
        {
            if (jumpAction.WasPressedThisFrame()) jumpBufferTimer = jumpBuffer;

            if (dashAction.WasPressedThisFrame() && dashCooldownTimer <= 0f)
            {
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }
        }

        animator.SetBool(GroundedParam, grounded);
    }

    void FixedUpdate()
    {
        grounded = Time.time - lastJumpTime > 0.1f && body.IsTouching(groundCollider);

        if (jumpBufferTimer > 0f && grounded)
        {
            jumpBufferTimer = 0f;
            rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
            grounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable interactable = other.GetComponentInParent<Interactable>();
        if (interactable != null) interactable.OnPlayerContact(this);
    }

    public void TakeDamage(int amount)
    {
        if (Lives <= 0 || invulnerableTimer > 0f) return;

        Lives = Mathf.Max(0, Lives - amount);
        if (Lives == 0)
        {
            animator.SetTrigger(DieParam);
            GameManager.Instance.GameOver();
        }
        else
        {
            invulnerableTimer = hitInvulnerability;
            animator.SetTrigger(HitParam);
        }
    }
}
