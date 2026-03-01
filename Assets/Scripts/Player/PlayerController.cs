using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS karakter hareket kontrolcüsü.
/// CharacterController kullanarak WASD hareket, zıplama, koşma ve eğilme işlemlerini yönetir.
/// Tüm parametreler Inspector üzerinden ayarlanabilir.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Movement Settings

    [Header("Hareket Ayarları")]
    [Tooltip("Normal yürüme hızı")]
    [SerializeField] private float walkSpeed = 5f;

    [Tooltip("Koşma hızı")]
    [SerializeField] private float runSpeed = 9f;

    [Tooltip("Eğilme sırasında hareket hızı")]
    [SerializeField] private float crouchSpeed = 2.5f;

    [Tooltip("Hareket yumuşatma hızı (0 = anlık, yüksek = daha yumuşak)")]
    [SerializeField] private float moveSmoothTime = 0.1f;

    #endregion

    #region Carry Settings

    [Header("Taşıma Ayarları")]
    [Tooltip("Ağır parça taşırken hız çarpanı (Inspector'dan ayarlanmaz, kod tarafından set edilir)")]
    private float carrySpeedMultiplier = 1f;

    #endregion

    #region Jump Settings

    [Header("Zıplama Ayarları")]
    [Tooltip("Zıplama kuvveti")]
    [SerializeField] private float jumpForce = 7f;

    [Tooltip("Yerçekimi kuvveti")]
    [SerializeField] private float gravity = 20f;

    #endregion

    #region Crouch Settings

    [Header("Eğilme Ayarları")]
    [Tooltip("Ayakta karakter yüksekliği")]
    [SerializeField] private float standHeight = 2f;

    [Tooltip("Eğilme yüksekliği")]
    [SerializeField] private float crouchHeight = 1f;

    [Tooltip("Eğilme geçiş hızı (ne kadar hızlı eğilir/kalkar)")]
    [SerializeField] private float crouchTransitionSpeed = 8f;

    #endregion

    // Bileşen referansları
    private CharacterController controller;
    private PlayerInputActions inputActions;

    // Dahili durum değişkenleri
    private Vector3 velocity;
    private Vector2 currentMoveInput;
    private Vector2 smoothMoveVelocity;
    private Vector2 smoothedMoveInput;
    private bool isSprinting;
    private bool isCrouching;
    private float targetHeight;

    // Başlangıç CharacterController ayarları
    private Vector3 defaultCenter;
    private float defaultHeight;

    /// <summary>
    /// Karakterin şu an yerde olup olmadığını döndürür.
    /// </summary>
    public bool IsGrounded => controller.isGrounded;

    /// <summary>
    /// Karakterin şu an koşup koşmadığını döndürür.
    /// </summary>
    public bool IsSprinting => isSprinting && !isCrouching && currentMoveInput.sqrMagnitude > 0.01f;

    /// <summary>
    /// Karakterin şu an eğilip eğilmediğini döndürür.
    /// </summary>
    public bool IsCrouching => isCrouching;

    /// <summary>
    /// Karakterin hareket edip etmediğini döndürür.
    /// </summary>
    public bool IsMoving => currentMoveInput.sqrMagnitude > 0.01f;

    /// <summary>
    /// Eğilme oranını döndürür (0 = ayakta, 1 = tam eğilmiş).
    /// Kamera yüksekliği hesaplaması için kullanılır.
    /// </summary>
    public float CrouchRatio
    {
        get
        {
            if (defaultHeight <= crouchHeight) return 0f;
            return 1f - (controller.height - crouchHeight) / (defaultHeight - crouchHeight);
        }
    }

    /// <summary>
    /// Mevcut hareket hızını döndürür.
    /// </summary>
    public float CurrentSpeed
    {
        get
        {
            float baseSpeed;
            if (isCrouching) baseSpeed = crouchSpeed;
            else if (IsSprinting) baseSpeed = runSpeed;
            else baseSpeed = walkSpeed;

            return baseSpeed * carrySpeedMultiplier;
        }
    }

    /// <summary>
    /// Taşıma hız çarpanını ayarlar. Motor gibi ağır parçalar için kullanılır.
    /// 1.0 = normal hız, 0.5 = yarı hız.
    /// </summary>
    public void SetCarrySpeedMultiplier(float multiplier)
    {
        carrySpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    private void Awake()
    {
        // Bileşenleri al
        controller = GetComponent<CharacterController>();

        // Input Actions oluştur
        inputActions = new PlayerInputActions();

        // CharacterController'ın mevcut ayarlarını kaydet (Inspector'daki değerleri korur)
        defaultCenter = controller.center;
        defaultHeight = controller.height;
        standHeight = defaultHeight;
        targetHeight = standHeight;
    }

    private void OnEnable()
    {
        // Input'ları etkinleştir ve callback'leri bağla
        inputActions.Player.Enable();

        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Sprint.started += OnSprintStarted;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;
        inputActions.Player.Crouch.started += OnCrouchStarted;
        inputActions.Player.Crouch.canceled += OnCrouchCanceled;
    }

    private void OnDisable()
    {
        // Input'ları devre dışı bırak ve callback'leri kaldır
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Sprint.started -= OnSprintStarted;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Crouch.started -= OnCrouchStarted;
        inputActions.Player.Crouch.canceled -= OnCrouchCanceled;

        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        // Input Actions'ı temizle
        inputActions?.Dispose();
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleCrouch();
    }

    /// <summary>
    /// Yatay hareket hesaplamasını yapar.
    /// </summary>
    private void HandleMovement()
    {
        // Move action'dan Vector2 input'u al
        currentMoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Input'u yumuşat
        smoothedMoveInput = Vector2.SmoothDamp(
            smoothedMoveInput,
            currentMoveInput,
            ref smoothMoveVelocity,
            moveSmoothTime
        );

        // Hareket yönünü hesapla (karakter yönüne göre)
        float speed = CurrentSpeed;
        Vector3 moveDirection = transform.right * smoothedMoveInput.x + transform.forward * smoothedMoveInput.y;
        moveDirection *= speed;

        // Dikey hızı koru, yatay hareketi uygula
        moveDirection.y = velocity.y;
        velocity = moveDirection;

        // CharacterController ile hareket ettir
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Yerçekimi ve dikey hız hesaplamasını yapar.
    /// </summary>
    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            // Yerdeyken hafif aşağı kuvvet uygula (isGrounded stabilitesi için)
            velocity.y = -2f;
        }
        else
        {
            // Havadayken yerçekimi uygula
            velocity.y -= gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// Eğilme geçişini yönetir (CharacterController yükseklik ve center değişimi).
    /// </summary>
    private void HandleCrouch()
    {
        // Mevcut ve hedef yükseklik arasında yumuşak geçiş
        float currentHeight = controller.height;
        float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Ayakların yerde sabit kalması için center'ı aşağı kaydır
        // Yükseklik farkının yarısı kadar center aşağı iner
        float heightDifference = defaultHeight - newHeight;
        controller.height = newHeight;
        controller.center = defaultCenter - new Vector3(0f, heightDifference / 2f, 0f);
    }

    #region Input Callbacks

    /// <summary>
    /// Zıplama input callback'i.
    /// </summary>
    private void OnJump(InputAction.CallbackContext context)
    {
        // Yerdeyken ve eğilmiyorken zıpla
        if (controller.isGrounded && !isCrouching)
        {
            velocity.y = jumpForce;
        }
    }

    /// <summary>
    /// Koşma başlangıç callback'i.
    /// </summary>
    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    /// <summary>
    /// Koşma bitiş callback'i.
    /// </summary>
    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    /// <summary>
    /// Eğilme başlangıç callback'i.
    /// </summary>
    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        isCrouching = true;
        targetHeight = crouchHeight;
    }

    /// <summary>
    /// Eğilme bitiş callback'i.
    /// </summary>
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        isCrouching = false;
        targetHeight = standHeight;
    }

    #endregion
}
