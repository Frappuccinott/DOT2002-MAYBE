using UnityEngine;

/// <summary>
/// Yerde duran, oyuncunun F tuşuyla alıp arabaya takabileceği bir araba parçası.
/// IInteractable interface'ini uygular (InteractionType.Pickup).
///
/// Kurulum:
///   1. Parça 3D modelini sahneye koy
///   2. Bu scripti ekle
///   3. Part Type = bu parçanın tipi (Engine, WheelFrontLeft vb.)
///   4. Collider olduğundan emin ol (MeshCollider veya BoxCollider)
///   5. (Opsiyonel) Rigidbody ekle — bırakılınca fizikle düşsün
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickupableCarPart : MonoBehaviour, IInteractable
{
    [Header("Parça Ayarları")]
    [Tooltip("Bu parçanın tipi (hangi slot'a yerleşecek)")]
    [SerializeField] private CarPartType partType;

    [Tooltip("Etkileşim metni")]
    [SerializeField] private string promptText = "Parçayı Al [F]";

    private PlayerInteraction cachedPlayer;

    public CarPartType PartType => partType;

    #region IInteractable

    public string InteractionPrompt => promptText;
    public InteractionType Type => InteractionType.Pickup;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<PlayerInteraction>();
            return cachedPlayer == null || !cachedPlayer.HasCarPart;
        }
    }

    public void Interact() { }

    #endregion
}
