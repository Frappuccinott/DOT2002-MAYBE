using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{
    [Header("Crosshair Ayarları")]
    [SerializeField] private float size = 16f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private float outlineThickness = 1f;

    private Texture2D crosshairTexture;

    private void Awake()
    {
        // 64x64 çözünürlüklü ve içi dolu beyaz bir daire dokusu (texture) oluşturuyoruz
        int texSize = 64;
        crosshairTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        
        float radius = texSize / 2f;
        float center = (texSize - 1) / 2f;

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Kenarları yumuşatmak (anti-aliasing) için alfayı aradaki farka göre ayarlıyoruz
                float alpha = Mathf.Clamp01(radius - dist);
                crosshairTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        crosshairTexture.Apply();
    }

    private void OnDestroy()
    {
        if (crosshairTexture != null) Destroy(crosshairTexture);
    }

    private void OnGUI()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        if (outlineThickness > 0f) DrawCircle(cx, cy, outlineColor, outlineThickness);
        DrawCircle(cx, cy, defaultColor, 0f);
    }

    private void DrawCircle(float cx, float cy, Color color, float expand)
    {
        GUI.color = color;
        
        float currentSize = size + (expand * 2f);
        float halfSize = currentSize / 2f;

        // Daireyi tam ekranın ortasına yerleştirecek şekilde çiziyoruz
        GUI.DrawTexture(new Rect(cx - halfSize, cy - halfSize, currentSize, currentSize), crosshairTexture);

        GUI.color = Color.white;
    }
}
