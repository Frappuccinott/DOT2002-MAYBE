using UnityEngine;

/// <summary>
/// Ekranın ortasında basit bir crosshair (nişangah) çizer.
/// Kameraya veya herhangi bir objeye eklenebilir.
/// </summary>
public class PlayerCrosshair : MonoBehaviour
{
    [Header("Crosshair Ayarları")]
    [Tooltip("Crosshair boyutu (piksel)")]
    [SerializeField] private float size = 16f;

    [Tooltip("Crosshair çizgi kalınlığı (piksel)")]
    [SerializeField] private float thickness = 2f;

    [Tooltip("Crosshair ortasındaki boşluk (piksel)")]
    [SerializeField] private float gap = 4f;

    [Tooltip("Crosshair rengi")]
    [SerializeField] private Color color = Color.white;

    [Tooltip("Crosshair dış çizgi (gölge) rengi")]
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.5f);

    [Tooltip("Dış çizgi kalınlığı (piksel)")]
    [SerializeField] private float outlineThickness = 1f;

    // Çizim için texture
    private Texture2D crosshairTexture;

    private void Awake()
    {
        // 1x1 beyaz texture oluştur (renklendirme için)
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
        crosshairTexture.Apply();
    }

    private void OnDestroy()
    {
        if (crosshairTexture != null)
        {
            Destroy(crosshairTexture);
        }
    }

    private void OnGUI()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Dış çizgi (outline) — Okunabilirlik için arka plan gölgesi
        if (outlineThickness > 0f)
        {
            DrawCrosshair(centerX, centerY, outlineColor, outlineThickness);
        }

        // Ana crosshair çizgileri
        DrawCrosshair(centerX, centerY, color, 0f);
    }

    /// <summary>
    /// Crosshair çizgilerini çizer.
    /// </summary>
    /// <param name="cx">Ekran merkezi X</param>
    /// <param name="cy">Ekran merkezi Y</param>
    /// <param name="drawColor">Çizim rengi</param>
    /// <param name="expand">Genişletme miktarı (outline için)</param>
    private void DrawCrosshair(float cx, float cy, Color drawColor, float expand)
    {
        GUI.color = drawColor;

        float halfThickness = (thickness + expand * 2f) / 2f;
        float innerGap = gap - expand;
        float outerEnd = size / 2f + expand;

        // Üst çizgi
        GUI.DrawTexture(new Rect(
            cx - halfThickness,
            cy - outerEnd,
            thickness + expand * 2f,
            outerEnd - innerGap
        ), crosshairTexture);

        // Alt çizgi
        GUI.DrawTexture(new Rect(
            cx - halfThickness,
            cy + innerGap,
            thickness + expand * 2f,
            outerEnd - innerGap
        ), crosshairTexture);

        // Sol çizgi
        GUI.DrawTexture(new Rect(
            cx - outerEnd,
            cy - halfThickness,
            outerEnd - innerGap,
            thickness + expand * 2f
        ), crosshairTexture);

        // Sağ çizgi
        GUI.DrawTexture(new Rect(
            cx + innerGap,
            cy - halfThickness,
            outerEnd - innerGap,
            thickness + expand * 2f
        ), crosshairTexture);

        // GUI rengini sıfırla
        GUI.color = Color.white;
    }
}
