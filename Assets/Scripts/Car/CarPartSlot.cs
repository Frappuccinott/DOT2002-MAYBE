using UnityEngine;

/// <summary>
/// Arabanın üzerindeki parça yerleştirme noktası.
/// F tuşuyla parça yerleştirme ve sökme.
/// Oyuncu doğru parçayı taşırken imleci slot'a yöneltince saydam yeşil önizleme gösterir.
///
/// Kurulum:
///   1. Arabanın child'ı olarak boş GameObject oluştur
///   2. Bu scripti ekle
///   3. Accepted Part Type = hangi parçayı kabul edecek
///   4. Part Visual = takılınca görünecek 3D model (child olarak, başta gizli)
///   5. Bir Collider ekle (BoxCollider vb.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class CarPartSlot : MonoBehaviour, IInteractable
{
    [Header("Slot Ayarları")]
    [SerializeField] private CarPartType acceptedPartType;
    [SerializeField] private GameObject partVisual;

    [Header("Etkileşim Metinleri")]
    [SerializeField] private string installPromptText = "Parçayı Yerleştir [F]";
    [SerializeField] private string removePromptText = "Parçayı Sök [F]";

    [Header("Yeşil Önizleme")]
    [SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.35f);

    // Durum
    private bool isInstalled;
    private PickupableCarPart installedPart;
    private PlayerInteraction cachedPlayer;

    // Önizleme
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material previewMaterial;
    private bool isPreviewing;

    public CarPartType AcceptedPartType => acceptedPartType;
    public bool IsInstalled => isInstalled;

    #region IInteractable

    public string InteractionPrompt => isInstalled ? removePromptText : installPromptText;
    public InteractionType Type => InteractionType.Pickup;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<PlayerInteraction>();
            if (cachedPlayer == null) return false;

            return isInstalled
                ? !cachedPlayer.HasCarPart
                : cachedPlayer.HasCarPart && cachedPlayer.HeldPartType == acceptedPartType;
        }
    }

    public void Interact() { }

    #endregion

    #region Install / Uninstall

    /// <summary>
    /// Parçayı slot'a yerleştirir.
    /// partVisual varsa onu gösterir, yoksa parçanın modelini slot pozisyonuna taşır.
    /// </summary>
    public void Install(PickupableCarPart part)
    {
        isInstalled = true;
        installedPart = part;

        // Preview'u kapat
        if (isPreviewing) HidePreview();

        if (partVisual != null)
        {
            partVisual.SetActive(true);
            RestoreOriginalMaterials();
            if (part != null) part.gameObject.SetActive(false);
        }
        else if (part != null)
        {
            // partVisual yoksa parçanın kendisini slot pozisyonuna yerleştir
            GameObject obj = part.gameObject;
            obj.SetActive(true);
            obj.transform.SetParent(transform);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.transform.localScale = Vector3.one;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetCollidersEnabled(obj, false);
        }

        GetComponentInParent<CarAssemblyManager>()?.OnPartInstalled(acceptedPartType);
        Debug.Log($"[CarPartSlot] {acceptedPartType} yerleştirildi!");
    }

    /// <summary>
    /// Takılı parçayı söker ve döndürür.
    /// </summary>
    public PickupableCarPart Uninstall()
    {
        isInstalled = false;

        if (partVisual != null) partVisual.SetActive(false);

        PickupableCarPart part = installedPart;
        installedPart = null;

        if (part != null)
        {
            GameObject obj = part.gameObject;
            obj.transform.SetParent(null);
            obj.SetActive(true);
            SetCollidersEnabled(obj, true);
        }

        GetComponentInParent<CarAssemblyManager>()?.OnPartRemoved(acceptedPartType);
        Debug.Log($"[CarPartSlot] {acceptedPartType} söküldü!");
        return part;
    }

    #endregion

    #region Önizleme

    /// <summary>
    /// PlayerInteraction'ın raycast'i bu slot'a geldiğinde çağrılır.
    /// Doğru parça eldeyse yeşil preview gösterir.
    /// </summary>
    public void SetLookedAt(bool isLooking, bool hasCorrectPart)
    {
        if (partVisual == null || isInstalled) return;

        bool shouldPreview = isLooking && hasCorrectPart;

        if (shouldPreview && !isPreviewing)
            ShowPreview();
        else if (!shouldPreview && isPreviewing)
            HidePreview();
    }

    private void Start()
    {
        CreatePreviewMaterial();

        if (partVisual != null)
        {
            renderers = partVisual.GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterials;
            partVisual.SetActive(false);
        }
    }

    private void CreatePreviewMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader != null)
        {
            previewMaterial = new Material(shader);
            previewMaterial.SetFloat("_Surface", 1);
            previewMaterial.SetFloat("_Blend", 0);
            previewMaterial.SetFloat("_AlphaClip", 0);
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            previewMaterial.SetColor("_BaseColor", previewColor);
            previewMaterial.color = previewColor;
        }
        else
        {
            previewMaterial = new Material(Shader.Find("Sprites/Default"));
            previewMaterial.color = previewColor;
            previewMaterial.renderQueue = 3000;
        }
    }

    private void ShowPreview()
    {
        partVisual.SetActive(true);
        ApplyPreviewMaterials();
        isPreviewing = true;
    }

    private void HidePreview()
    {
        partVisual.SetActive(false);
        RestoreOriginalMaterials();
        isPreviewing = false;
    }

    private void ApplyPreviewMaterials()
    {
        if (renderers == null || previewMaterial == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = new Material[renderers[i].sharedMaterials.Length];
            for (int j = 0; j < mats.Length; j++) mats[j] = previewMaterial;
            renderers[i].materials = mats;
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (renderers == null || originalMaterials == null) return;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].materials = originalMaterials[i];
    }

    #endregion

    #region Yardımcı

    private static void SetCollidersEnabled(GameObject obj, bool enabled)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = enabled;
    }

    private void OnDestroy()
    {
        if (previewMaterial != null) Destroy(previewMaterial);
    }

    #endregion
}
