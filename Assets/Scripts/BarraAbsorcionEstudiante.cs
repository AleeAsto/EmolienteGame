using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class BarraAbsorcionEstudiante : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float ancho = 1.2f;
    [SerializeField, Min(0.02f)] private float alto = 0.14f;
    [SerializeField, Min(0f)] private float margenCabeza = 0.25f;
    [SerializeField] private Color colorProgreso = new Color(0.4f, 0.9f, 1f, 1f);

    private AlmaEstudiante alma;
    private GameObject barra;
    private RectTransform relleno;
    private AlmasJugador ocupante;
    private SuccionJugador succion;

    private void Awake()
    {
        alma = GetComponent<AlmaEstudiante>();
        // Medir solo el personaje, antes de crear la UI.
        Bounds limites = new Bounds(transform.position, Vector3.zero);
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
            limites.Encapsulate(sprite.bounds);

        barra = new GameObject("BarraAbsorcion", typeof(RectTransform), typeof(Canvas));
        barra.layer = gameObject.layer;
        RectTransform rect = (RectTransform)barra.transform;
        rect.SetParent(transform, false);
        rect.position = new Vector3(limites.center.x, limites.max.y + margenCabeza, transform.position.z);
        rect.sizeDelta = new Vector2(ancho * 100f, alto * 100f);
        rect.localScale = Vector3.one * 0.01f;

        Canvas canvas = barra.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        CrearImagen("Fondo", rect, new Color(0.04f, 0.05f, 0.07f, 0.95f));
        RectTransform interior = CrearImagen("Interior", rect, new Color(0.2f, 0.22f, 0.25f, 1f));
        interior.offsetMin = new Vector2(2f, 2f);
        interior.offsetMax = new Vector2(-2f, -2f);
        relleno = CrearImagen("Progreso", interior, colorProgreso);
        barra.SetActive(false);
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (ocupante != alma.Ocupante)
        {
            ocupante = alma.Ocupante;
            succion = ocupante != null ? ocupante.GetComponent<SuccionJugador>() : null;
        }

        bool visible = alma.Disponible && succion != null &&
            succion.isActiveAndEnabled && succion.Objetivo == alma;
        barra.SetActive(visible);
        if (visible)
            relleno.anchorMax = new Vector2(succion.Progreso01, 1f);
    }

    private static RectTransform CrearImagen(string nombre, RectTransform padre, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.layer = padre.gameObject.layer;
        RectTransform rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return rect;
    }

    private void OnDisable()
    {
        if (barra != null)
            barra.SetActive(false);
    }
}
