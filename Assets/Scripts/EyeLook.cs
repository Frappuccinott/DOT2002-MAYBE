using UnityEngine;

/// <summary>
/// "Eye" adlý objeye ekle.
/// Karakteri takip eder ve ona doðru bakar.
/// </summary>
public class EyeLook : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Takip edilecek karakter objesi")]
    public Transform target;

    [Header("Ayarlar")]
    [Tooltip("Gözün hedefe dönme hýzý (0 = anlýk)")]
    public float rotationSpeed = 10f;

    void Update()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (rotationSpeed <= 0f)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}