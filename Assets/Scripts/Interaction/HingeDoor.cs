using UnityEngine;

/// <summary>
/// Evrensel menteşe tabanlı kapı/kapak sistemi.
/// Sol tık basılı tutup mouse sürükleyerek açılır/kapanır.
/// Araba kapısı, kaput, bagaj, benzin kapağı, hangar kapısı gibi
/// her türlü menteşeli obje için kullanılabilir.
///
/// Kurulum:
///   1. Kapı/kapak 3D modelini sahneye koy
///   2. Boş bir child GameObject oluştur ve menteşe noktasına (kapının döneceği kenara) yerleştir
///   3. Bu scripti kapı/kapak objesine ekle
///   4. Inspector'dan:
///      - Door Type: Kapı tipi seç
///      - Hinge Point: Oluşturduğun child'ı sürükle
///      - Rotation Axis: Dönme ekseni (Y=yatay kapı, X=kaput/bagaj)
///      - Min/Max Angle: Açılma limitleri
///   5. Objeye Collider ekle (BoxCollider vb.)
///
/// NOT: Script başlangıçta HingePoint'i otomatik olarak kapının parent'ı yapar.
/// Bu sayede kapı menteşe noktasından döner. Hiyerarşiyi elle değiştirme.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HingeDoor : MonoBehaviour
{
    /// <summary>
    /// Kapı/kapak tipi — Inspector'da görsel ayrım ve mantık kontrolü için.
    /// </summary>
    public enum DoorType
    {
        CarDoor,        // Araba kapısı (sağ/sol ön/arka)
        Hood,           // Kaput
        Trunk,          // Bagaj
        FuelCap,        // Benzin deposu kapağı
        HangarDoor,     // Hangar kapısı
        GenericDoor     // Genel kapı (oda kapısı vb.)
    }

    [Header("Kapı Tipi")]
    [Tooltip("Bu kapının tipi (araba kapısı, kaput, benzin kapağı vb.)")]
    [SerializeField] private DoorType doorType = DoorType.GenericDoor;

    [Header("Menteşe Ayarları")]
    [Tooltip("Menteşe noktası (boş child GameObject — kapının dönme merkezi)")]
    [SerializeField] private Transform hingePoint;

    [Tooltip("Dönme ekseni (local space). Y=yatay kapı, X=kaput/bagaj, Z=özel")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Açı Limitleri")]
    [Tooltip("Minimum açı (derece, genellikle 0 = kapalı)")]
    [SerializeField] private float minAngle = 0f;

    [Tooltip("Maksimum açılma açısı (derece)")]
    [SerializeField] private float maxAngle = 70f;

    [Header("Kontrol")]
    [Tooltip("Mouse sürükleme hassasiyeti")]
    [SerializeField] private float dragSensitivity = 0.5f;

    [Tooltip("Kapının yumuşak hareket hızı (yüksek = daha hızlı takip)")]
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Bağlantılar (Opsiyonel)")]
    [Tooltip("Bağlı CarPartSlot — atanırsa kapı sadece slot'a parça takılıyken çalışır")]
    [SerializeField] private CarPartSlot linkedSlot;

    // Durum
    private float currentAngle;
    private float targetAngle;
    private Quaternion initialRotation;
    private Transform rotationTarget;

    /// <summary>
    /// Bu kapının tipi.
    /// </summary>
    public DoorType Type => doorType;

    /// <summary>
    /// Kapı kullanılabilir mi?
    /// LinkedSlot atanmamışsa her zaman true.
    /// Atanmışsa sadece slot'a parça takılıyken true.
    /// </summary>
    public bool CanOperate
    {
        get
        {
            if (linkedSlot == null) return true;
            return linkedSlot.IsInstalled;
        }
    }

    /// <summary>
    /// Kapı şu an açık mı? (açı > minAngle + 1 derece tolerans)
    /// </summary>
    public bool IsOpen => currentAngle > minAngle + 1f;

    private void Awake()
    {
        if (hingePoint != null)
        {
            // HingePoint'i kapının parent'ı yap (runtime'da otomatik)
            // Böylece kapı menteşe noktasından döner
            Transform originalParent = transform.parent;
            hingePoint.SetParent(originalParent);   // HingePoint eski parent'a taşınır
            transform.SetParent(hingePoint);         // Kapı artık HingePoint'in child'ı
            rotationTarget = hingePoint;
        }
        else
        {
            // HingePoint yoksa kendini döndür (pivot noktası objenin origin'i olur)
            rotationTarget = transform;
        }

        initialRotation = rotationTarget.localRotation;
        currentAngle = minAngle;
        targetAngle = minAngle;
    }

    private void Update()
    {
        // Açıyı yumuşak şekilde hedefe yaklaştır
        if (!Mathf.Approximately(currentAngle, targetAngle))
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, smoothSpeed * Time.deltaTime);

            // Çok küçük farkları düzelt (jitter önleme)
            if (Mathf.Abs(currentAngle - targetAngle) < 0.01f)
            {
                currentAngle = targetAngle;
            }

            ApplyRotation();
        }
    }

    /// <summary>
    /// Kapıyı mouse delta'sına göre sürükler.
    /// PlayerInteraction tarafından sol tık basılıyken çağrılır.
    /// </summary>
    /// <param name="mouseDelta">Mouse hareket delta'sı</param>
    public void DragDoor(Vector2 mouseDelta)
    {
        // Mouse X hareketine göre hedef açıyı değiştir
        targetAngle += mouseDelta.x * dragSensitivity;
        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
    }

    /// <summary>
    /// Rotasyonu uygular.
    /// </summary>
    private void ApplyRotation()
    {
        rotationTarget.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}
