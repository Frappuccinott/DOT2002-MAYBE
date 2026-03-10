using UnityEngine;

/// <summary>
/// Kamera head bobbing efekti ve eğilme sırasında kamera yükseklik ayarı.
/// Karakter hareket ederken kameranın yukarı-aşağı sallanmasını sağlar.
/// Yürüme, koşma ve eğilme durumlarına göre farklı bob parametreleri kullanır.
/// Bu script kamera objesine eklenmelidir.
/// </summary>
public class HeadBob : MonoBehaviour
{
    #region Walk Bob Settings

    [Header("Yürüme Bob Ayarları")]
    [Tooltip("Yürüme sırasında bob hızı")]
    [SerializeField] private float walkBobSpeed = 10f;

    [Tooltip("Yürüme sırasında bob miktarı (yükseklik)")]
    [SerializeField] private float walkBobAmount = 0.03f;

    #endregion

    #region Run Bob Settings

    [Header("Koşma Bob Ayarları")]
    [Tooltip("Koşma sırasında bob hızı")]
    [SerializeField] private float runBobSpeed = 14f;

    [Tooltip("Koşma sırasında bob miktarı (yükseklik)")]
    [SerializeField] private float runBobAmount = 0.05f;

    #endregion

    #region Crouch Bob Settings

    [Header("Eğilme Bob Ayarları")]
    [Tooltip("Eğilme sırasında bob hızı")]
    [SerializeField] private float crouchBobSpeed = 6f;

    [Tooltip("Eğilme sırasında bob miktarı (yükseklik)")]
    [SerializeField] private float crouchBobAmount = 0.015f;

    #endregion

    #region General Settings

    [Header("Genel Ayarlar")]
    [Tooltip("Bob geçiş yumuşaklığı (yüksek değer = daha yumuşak geçiş)")]
    [SerializeField] private float smoothTransition = 10f;

    [Tooltip("Eğilme sırasında kameranın ineceği mesafe")]
    [SerializeField] private float crouchCameraOffset = 0.5f;

    [Tooltip("Kamera eğilme geçiş yumuşaklığı")]
    [SerializeField] private float crouchCameraSmooth = 8f;

    [Tooltip("PlayerController referansı (boş bırakılırsa parent'tan otomatik bulunur)")]
    [SerializeField] private PlayerController playerController;

    #endregion

    // Dahili durum değişkenleri
    private float bobTimer;
    private float defaultYPosition;

    private void Awake()
    {
        // Başlangıç Y pozisyonunu kaydet (kameranın lokal Y'si)
        defaultYPosition = transform.localPosition.y;

        // PlayerController referansı atanmadıysa parent'tan bul
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[HeadBob] PlayerController bulunamadı! Lütfen Inspector'dan atayın veya parent objeye ekleyin.");
            }
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        HandleHeadBob();
    }

    /// <summary>
    /// Head bob efektini ve eğilme kamera offset'ini hesaplar ve uygular.
    /// </summary>
    private void HandleHeadBob()
    {
        // Eğilme oranına göre kamera Y offset'ini hesapla
        float crouchOffset = playerController.CrouchRatio * -crouchCameraOffset;
        float baseY = defaultYPosition + crouchOffset;

        // Karakter yerdeyse ve hareket ediyorsa bob yap
        if (playerController.IsGrounded && playerController.IsMoving)
        {
            // Mevcut duruma göre bob hız ve miktarını belirle
            float bobSpeed;
            float bobAmount;

            if (playerController.IsCrouching)
            {
                bobSpeed = crouchBobSpeed;
                bobAmount = crouchBobAmount;
            }
            else if (playerController.IsSprinting)
            {
                bobSpeed = runBobSpeed;
                bobAmount = runBobAmount;
            }
            else
            {
                bobSpeed = walkBobSpeed;
                bobAmount = walkBobAmount;
            }

            // Sinüs dalgası ile bob hesapla
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;

            // Hedef pozisyonu hesapla ve yumuşak geçiş uygula
            float targetY = baseY + bobOffset;
            Vector3 localPos = transform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, targetY, smoothTransition * Time.deltaTime);
            transform.localPosition = localPos;
        }
        else
        {
            // Karakter duruyor veya havada — kamerayı base pozisyonuna döndür
            bobTimer = 0f;

            Vector3 localPos = transform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, baseY, crouchCameraSmooth * Time.deltaTime);
            transform.localPosition = localPos;
        }
    }
}
