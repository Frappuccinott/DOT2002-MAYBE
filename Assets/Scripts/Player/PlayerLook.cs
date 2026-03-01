using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS kamera bakış kontrolcüsü.
/// Mouse ile yatay/dikey bakış açısını yönetir.
/// Yatay döndürme player objesine, dikey döndürme kameraya uygulanır.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    #region Settings

    [Header("Bakış Ayarları")]
    [Tooltip("Mouse hassasiyeti")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Tooltip("Yukarı/aşağı bakma açı limiti (derece)")]
    [SerializeField] private float maxLookAngle = 80f;

    [Tooltip("Bakış için kullanılacak kamera (boş bırakılırsa child'dan otomatik bulunur)")]
    [SerializeField] private Transform cameraTransform;

    #endregion

    // Input referansı
    private PlayerInputActions inputActions;

    // Dahili durum
    private float verticalRotation = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Kamera referansı atanmadıysa child'dan bul
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError("[PlayerLook] Kamera bulunamadı! Lütfen Inspector'dan atayın veya child olarak ekleyin.");
            }
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        // İmleci kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();

        // İmleci serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void LateUpdate()
    {
        HandleLook();
    }

    /// <summary>
    /// Mouse girdisine göre bakış açısını günceller.
    /// </summary>
    private void HandleLook()
    {
        if (cameraTransform == null) return;

        // Look action'dan mouse delta'sını al
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        // Yatay döndürme (player objesini Y ekseninde döndür)
        float horizontalRotation = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * horizontalRotation);

        // Dikey döndürme (kamerayı X ekseninde döndür, sınırlı)
        verticalRotation -= lookInput.y * mouseSensitivity * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
