using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MarcadorVersus : MonoBehaviour
{
    [Header("Puntuacion")]
    [SerializeField] private AlmasJugador jugador1;
    [SerializeField] private AlmasJugador jugador2;

    [Header("Barra")]
    [SerializeField] private RectTransform carril;
    [SerializeField] private Image tramoJugador1;
    [SerializeField] private Image tramoJugador2;
    [SerializeField] private RectTransform indicador;
    [SerializeField] private Text puntosJugador1;
    [SerializeField] private Text puntosJugador2;

    [Header("Aspecto")]
    [SerializeField] private Color colorJugador1 = new Color(0.48f, 0.83f, 1f, 1f);
    [SerializeField] private Color colorJugador2 = new Color(0.95f, 0.20f, 0.25f, 1f);
    [SerializeField] private Color colorNumeros = new Color(0.04f, 0.05f, 0.07f, 1f);
    [SerializeField, Min(0f)] private float tiempoTransicion = 0.18f;
    [Tooltip("Base por lado solo para la proporcion visual; no suma puntos reales.")]
    [SerializeField, Min(0f)] private float baseVisualPorJugador = 20f;

    private float proporcionObjetivo = 0.5f;
    private float proporcionVisible = 0.5f;
    private float velocidadTransicion;

    private bool ReferenciasValidas => jugador1 != null && jugador2 != null &&
        carril != null && tramoJugador1 != null && tramoJugador2 != null &&
        indicador != null && puntosJugador1 != null && puntosJugador2 != null;

    private void Awake()
    {
        if (ReferenciasValidas)
            return;

        Debug.LogError("Asigna ambos jugadores y las referencias UI del marcador versus.", this);
        enabled = false;
    }

    private void OnEnable()
    {
        if (!ReferenciasValidas)
            return;

        jugador1.CantidadCambiada += ActualizarPuntuaciones;
        jugador2.CantidadCambiada += ActualizarPuntuaciones;
        ActualizarInmediato();
    }

    private void OnDisable()
    {
        if (jugador1 != null)
            jugador1.CantidadCambiada -= ActualizarPuntuaciones;
        if (jugador2 != null)
            jugador2.CantidadCambiada -= ActualizarPuntuaciones;
    }

    private void ActualizarPuntuaciones(float cantidad)
    {
        float puntos1 = Mathf.Max(0f, jugador1.Cantidad);
        float puntos2 = Mathf.Max(0f, jugador2.Cantidad);
        double baseVisual = Mathf.Max(0f, baseVisualPorJugador);
        double total = (double)puntos1 + puntos2 + 2d * baseVisual;
        proporcionObjetivo = total > 0d ? (float)((puntos1 + baseVisual) / total) : 0.5f;
        puntosJugador1.text = puntos1.ToString("0.#");
        puntosJugador2.text = puntos2.ToString("0.#");
        puntosJugador1.enabled = Mathf.Round(puntos1 * 10f) > 0f;
        puntosJugador2.enabled = Mathf.Round(puntos2 * 10f) > 0f;
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!ReferenciasValidas)
            return;

        proporcionVisible = tiempoTransicion <= 0f
            ? proporcionObjetivo
            : Mathf.SmoothDamp(proporcionVisible, proporcionObjetivo, ref velocidadTransicion,
                tiempoTransicion, Mathf.Infinity, Time.unscaledDeltaTime);
        if (Mathf.Abs(proporcionVisible - proporcionObjetivo) < 0.0001f)
            proporcionVisible = proporcionObjetivo;

        ActualizarGeometria();
    }

    [ContextMenu("Actualizar marcador")]
    public void ActualizarInmediato()
    {
        if (!ReferenciasValidas)
            return;

        ActualizarPuntuaciones(0);
        proporcionVisible = proporcionObjetivo;
        velocidadTransicion = 0f;
        ActualizarGeometria();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (ReferenciasValidas)
            ActualizarGeometria();
    }

    private void ActualizarGeometria()
    {
        float division = Mathf.Clamp01(proporcionVisible);
        tramoJugador1.color = colorJugador1;
        tramoJugador2.color = colorJugador2;
        tramoJugador1.rectTransform.anchorMax = new Vector2(division, 1f);
        tramoJugador2.rectTransform.anchorMin = new Vector2(division, 0f);
        indicador.anchorMin = indicador.anchorMax = new Vector2(division, 0.5f);
        indicador.anchoredPosition = Vector2.zero;

        float ancho = carril.rect.width;
        ColocarNumero(puntosJugador1, division * ancho * 0.5f,
            division * ancho, ancho, colorJugador1);
        ColocarNumero(puntosJugador2, (1f + division) * ancho * 0.5f,
            (1f - division) * ancho, ancho, colorJugador2);
    }

    private void ColocarNumero(Text texto, float centro, float anchoTramo, float anchoCarril, Color colorJugador)
    {
        // Si un jugador tiene muy pocos puntos, su numero sigue visible
        // por encima del carril, separado del aro que llega al extremo.
        float espacioTexto = Mathf.Max(40f, texto.preferredWidth);
        bool estrecho = anchoTramo < espacioTexto + indicador.sizeDelta.x + 24f;
        float margen = Mathf.Min(espacioTexto * 0.5f + 12f, anchoCarril * 0.5f);
        float x = Mathf.Clamp(centro, margen, Mathf.Max(margen, anchoCarril - margen));
        float altura = indicador.sizeDelta.y * 0.5f + texto.fontSize * 0.5f + 10f;
        texto.rectTransform.anchoredPosition = new Vector2(x, estrecho ? altura : 0f);
        texto.color = estrecho ? colorJugador : colorNumeros;
    }
}
