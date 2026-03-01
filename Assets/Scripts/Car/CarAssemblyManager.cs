using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Arabanın genel montaj durumunu yönetir.
/// Tüm CarPartSlot'ları takip eder ve montaj ilerlemesini bildirir.
/// Inspector'dan seçilen parçalar takıldığında araba çalışabilir duruma gelir.
/// Araba objesinin root'una eklenir.
/// </summary>
public class CarAssemblyManager : MonoBehaviour
{
    [Header("Montaj Ayarları")]
    [Tooltip("Tüm parça slotları (boş bırakılırsa child'lardan otomatik bulunur)")]
    [SerializeField] private CarPartSlot[] partSlots;

    [Header("Çalıştırma Gereksinimleri")]
    [Tooltip("Arabanın çalışması için gerekli parça tipleri")]
    [SerializeField] private CarPartType[] requiredParts;

    // Yerleştirilmiş parçalar
    private HashSet<CarPartType> installedParts = new HashSet<CarPartType>();

    // Gerekli parçalar set'i (hızlı lookup için)
    private HashSet<CarPartType> requiredPartsSet = new HashSet<CarPartType>();

    // Toplam parça sayısı
    private int totalSlotCount;

    /// <summary>
    /// Kaç parça yerleştirildi.
    /// </summary>
    public int InstalledCount => installedParts.Count;

    /// <summary>
    /// Toplam slot sayısı.
    /// </summary>
    public int TotalSlotCount => totalSlotCount;

    /// <summary>
    /// Tüm parçalar yerleştirildi mi?
    /// </summary>
    public bool IsComplete => installedParts.Count >= totalSlotCount && totalSlotCount > 0;

    /// <summary>
    /// Montaj ilerleme oranı (0-1).
    /// </summary>
    public float Progress => totalSlotCount > 0 ? (float)installedParts.Count / totalSlotCount : 0f;

    /// <summary>
    /// Araba çalıştırılabilir mi? (Gerekli parçaların hepsi takılı mı?)
    /// </summary>
    public bool CanStart
    {
        get
        {
            // Gerekli parça listesi boşsa, tüm parçalar gerekli
            if (requiredPartsSet.Count == 0) return IsComplete;

            foreach (CarPartType part in requiredPartsSet)
            {
                if (!installedParts.Contains(part)) return false;
            }
            return true;
        }
    }

    private void Awake()
    {
        // Slotlar Inspector'dan atanmadıysa child'lardan bul
        if (partSlots == null || partSlots.Length == 0)
        {
            partSlots = GetComponentsInChildren<CarPartSlot>(true);
        }

        totalSlotCount = partSlots.Length;

        // Gerekli parçaları set'e aktar
        if (requiredParts != null)
        {
            foreach (CarPartType part in requiredParts)
            {
                requiredPartsSet.Add(part);
            }
        }
    }

    /// <summary>
    /// Bir parça yerleştirildiğinde CarPartSlot tarafından çağrılır.
    /// </summary>
    public void OnPartInstalled(CarPartType partType)
    {
        installedParts.Add(partType);

        Debug.Log($"[CarAssembly] {partType} yerleştirildi! İlerleme: {InstalledCount}/{TotalSlotCount}");

        if (CanStart)
        {
            Debug.Log("[CarAssembly] Araba çalıştırılabilir durumda!");
        }

        if (IsComplete)
        {
            OnAssemblyComplete();
        }
    }

    /// <summary>
    /// Bir parça söküldüğünde CarPartSlot tarafından çağrılır.
    /// </summary>
    public void OnPartRemoved(CarPartType partType)
    {
        installedParts.Remove(partType);

        Debug.Log($"[CarAssembly] {partType} söküldü! İlerleme: {InstalledCount}/{TotalSlotCount}");
    }

    /// <summary>
    /// Tüm parçalar yerleştirildiğinde çağrılır.
    /// </summary>
    private void OnAssemblyComplete()
    {
        Debug.Log("[CarAssembly] Araba montajı tamamlandı!");
    }
}
