using UnityEngine;

// Geometria reutilizada: no modifica sprites ni animaciones del personaje.
[DisallowMultipleComponent]
public class VisualPerfumeJugador : MonoBehaviour
{
    private const int Corazones = 7;
    private readonly LineRenderer[] particulas = new LineRenderer[Corazones + 3];
    private SpriteRenderer cuerpo;
    private float tiempo;

    public void Inicializar(SpriteRenderer fuente)
    {
        cuerpo = fuente;
        Material material = Resources.Load<Material>("MaterialLatigo");
        if (cuerpo == null || material == null) return;
        for (int i = 0; i < particulas.Length; i++)
        {
            var objeto = new GameObject(i < Corazones ? "CorazonPerfume" : "DestelloPerfume", typeof(LineRenderer));
            objeto.layer = gameObject.layer;
            objeto.transform.SetParent(transform, false);
            var linea = objeto.GetComponent<LineRenderer>();
            linea.sharedMaterial = material;
            linea.useWorldSpace = true;
            linea.loop = true;
            linea.positionCount = i < Corazones ? 32 : 4;
            linea.numCornerVertices = 2;
            linea.numCapVertices = 2;
            linea.enabled = false;
            particulas[i] = linea;
        }
    }

    public void Actualizar(bool visible)
    {
        visible &= isActiveAndEnabled && cuerpo != null && cuerpo.enabled && cuerpo.gameObject.activeInHierarchy;
        if (visible) tiempo += Time.deltaTime;
        for (int i = 0; i < particulas.Length; i++)
        {
            LineRenderer linea = particulas[i];
            if (linea == null) continue;
            linea.enabled = visible;
            if (!visible) continue;
            Bounds figura = cuerpo.bounds;
            float altura = Mathf.Clamp(figura.size.y, 0.8f, 6f);
            float fase = Mathf.Repeat(tiempo / 2.4f + i * 0.381966f, 1f);
            float lado = Mathf.Sin(i * 2.4f) * 0.75f;
            Vector3 centro = figura.center + new Vector3(
                lado * Mathf.Max(figura.extents.x, altura * 0.3f) + Mathf.Sin(tiempo * 2f + i) * altura * 0.06f,
                Mathf.Lerp(-0.12f, 0.85f, fase) * altura, -0.02f);
            float escala = altura * (i < Corazones ? 0.16f : 0.075f) *
                (0.8f + 0.2f * Mathf.Sin(fase * Mathf.PI));
            float opacidad = Mathf.Clamp01(fase * 7f) * Mathf.Clamp01((1f - fase) * 5f) * cuerpo.color.a;
            Color color = i < Corazones ? new Color(1f, 0.35f + fase * 0.3f, 0.72f, opacidad) :
                new Color(1f, 0.86f, 0.96f, opacidad);
            linea.startColor = linea.endColor = color;
            linea.startWidth = linea.endWidth = altura * (i < Corazones ? 0.012f : 0.008f);
            linea.sortingLayerID = cuerpo.sortingLayerID;
            linea.sortingOrder = cuerpo.sortingOrder + 2;
            for (int punto = 0; punto < linea.positionCount; punto++)
            {
                float angulo = punto * Mathf.PI * 2f / linea.positionCount;
                Vector2 forma;
                if (i < Corazones)
                {
                    float seno = Mathf.Sin(angulo);
                    forma = new Vector2(16f * seno * seno * seno,
                        13f * Mathf.Cos(angulo) - 5f * Mathf.Cos(2f * angulo) -
                        2f * Mathf.Cos(3f * angulo) - Mathf.Cos(4f * angulo)) / 32f;
                }
                else forma = new Vector2(Mathf.Cos(angulo) * 0.6f, Mathf.Sin(angulo));
                linea.SetPosition(punto, centro + (Vector3)(forma * escala));
            }
        }
    }

    private void OnDisable() => Actualizar(false);
}
