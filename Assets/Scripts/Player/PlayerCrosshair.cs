using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{
    [Header("Crosshair Ayarları")]
    [SerializeField] private float size = 16f;
    [SerializeField] private float thickness = 2f;
    [SerializeField] private float gap = 4f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private float outlineThickness = 1f;

    private Texture2D crosshairTexture;

    private void Awake()
    {
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
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

        if (outlineThickness > 0f) DrawCrosshair(cx, cy, outlineColor, outlineThickness);
        DrawCrosshair(cx, cy, defaultColor, 0f);
    }

    private void DrawCrosshair(float cx, float cy, Color color, float expand)
    {
        GUI.color = color;

        float half = (thickness + expand * 2f) / 2f;
        float inner = gap - expand;
        float outer = size / 2f + expand;
        float w = thickness + expand * 2f;
        float len = outer - inner;

        GUI.DrawTexture(new Rect(cx - half, cy - outer, w, len), crosshairTexture);
        GUI.DrawTexture(new Rect(cx - half, cy + inner, w, len), crosshairTexture);
        GUI.DrawTexture(new Rect(cx - outer, cy - half, len, w), crosshairTexture);
        GUI.DrawTexture(new Rect(cx + inner, cy - half, len, w), crosshairTexture);

        GUI.color = Color.white;
    }
}
