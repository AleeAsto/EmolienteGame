using UnityEngine;
using UnityEngine.UI;

// Geometria de UI para mantener la flecha nitida sin depender de una fuente.
public sealed class GraficoFlechaUI : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = rectTransform.rect;
        float escala = Mathf.Min(rect.width / 64f, rect.height / 48f);
        Color borde = new Color(0.12f, 0.015f, 0.02f, color.a);
        Rectangulo(vertices, rect.center, escala, -26, -7, 6, 7, borde);
        Triangulo(vertices, rect.center, escala, new Vector2(2,-18), new Vector2(28,0), new Vector2(2,18), borde);
        Rectangulo(vertices, rect.center, escala, -24, -5, 6, 5, color);
        Triangulo(vertices, rect.center, escala, new Vector2(4,-14), new Vector2(24,0), new Vector2(4,14), color);
    }

    private static void Rectangulo(VertexHelper v, Vector2 centro, float escala, float x1, float y1, float x2, float y2, Color tono)
    {
        int inicio = v.currentVertCount;
        v.AddVert(centro + new Vector2(x1,y1) * escala, tono, Vector2.zero);
        v.AddVert(centro + new Vector2(x1,y2) * escala, tono, Vector2.zero);
        v.AddVert(centro + new Vector2(x2,y2) * escala, tono, Vector2.zero);
        v.AddVert(centro + new Vector2(x2,y1) * escala, tono, Vector2.zero);
        v.AddTriangle(inicio,inicio+1,inicio+2); v.AddTriangle(inicio,inicio+2,inicio+3);
    }

    private static void Triangulo(VertexHelper v, Vector2 centro, float escala, Vector2 a, Vector2 b, Vector2 c, Color tono)
    {
        int inicio = v.currentVertCount;
        v.AddVert(centro + a * escala, tono, Vector2.zero);
        v.AddVert(centro + b * escala, tono, Vector2.zero);
        v.AddVert(centro + c * escala, tono, Vector2.zero);
        v.AddTriangle(inicio,inicio+1,inicio+2);
    }
}
