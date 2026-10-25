using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovimientoJugador2D))]
[DefaultExecutionOrder(-50)]
public class UsarPuertaJugador : MonoBehaviour
{
    [SerializeField] private RedPuertas red;
    [SerializeField, Tooltip("El BoxCollider2D de los pies, no el sprite ni la zona de ataque.")]
    private BoxCollider2D colliderPies;
    [SerializeField, Min(0f)] private float cooldownPersonal = 5f;
    [Header("Animacion de entrada")]
    [SerializeField, Min(0.05f)] private float duracionEntrada = 0.45f;
    [SerializeField, Min(0.05f)] private float duracionFlash = 0.35f;
    [SerializeField] private Camera camaraJugador;

    private MovimientoJugador2D movimiento;
    private EstadoCombateJugador estado;
    private SuccionJugador succion;
    private AtaqueJugador ataque;
    private ManoCartasJugador cartas;
    public BoxCollider2D ColliderPies => colliderPies;
    public Vector3 PosicionPies => colliderPies != null
        ? colliderPies.transform.TransformPoint(colliderPies.offset)
        : transform.position;
    private bool requiereSoltar;
    private float proximoUso;
    public bool EstaViajando { get; private set; }
    private Transform visual;
    private Vector3 escalaVisual;
    private Vector3 posicionVisual;
    private SpriteRenderer[] sprites;
    private Color[] colores;
    private GameObject lienzoFlash;

    public float CooldownRestante => Mathf.Max(0f, proximoUso - Time.time);

    public bool PuedeUsar => !EstaViajando && isActiveAndEnabled && Time.timeScale > 0f && CooldownRestante <= 0f &&
        movimiento != null && movimiento.isActiveAndEnabled &&
        (estado == null || !estado.EstaAturdido) && (cartas == null || !cartas.BloqueaSuccion);

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        if (colliderPies == null)
            colliderPies = GetComponent<BoxCollider2D>();
        estado = GetComponent<EstadoCombateJugador>();
        succion = GetComponent<SuccionJugador>();
        ataque = GetComponent<AtaqueJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        visual = transform.Find("Sprite");
        if (visual == null) visual = transform;
        if (camaraJugador == null)
            foreach (CamaraPasillo seguimiento in FindObjectsByType<CamaraPasillo>(FindObjectsSortMode.None))
                if (seguimiento.Jugador == transform) camaraJugador = seguimiento.GetComponent<Camera>();
        if (red == null)
        {
            Debug.LogError("Asigna la red de puertas del jugador.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (EstaViajando || !Application.isFocused || movimiento.AccionMovimiento == null)
            return;

        Vector2 entrada = movimiento.AccionMovimiento.ReadValue<Vector2>();
        bool arriba = entrada.y > 0.5f;
        if (!arriba)
        {
            requiereSoltar = false;
            return;
        }

        // Las diagonales no activan puertas. Tras viajar,
        // hay que soltar la direccion para evitar encadenar otro viaje.
        if (!requiereSoltar && Mathf.Abs(entrada.x) < 0.01f)
            IntentarUsarPuerta();
    }

    public bool IntentarUsarPuerta()
    {
        if (requiereSoltar || !Application.isFocused || movimiento == null || movimiento.AccionMovimiento == null)
            return false;

        Vector2 entrada = movimiento.AccionMovimiento.ReadValue<Vector2>();
        return entrada.y > 0.5f && Mathf.Abs(entrada.x) < 0.01f &&
            red != null && red.IntentarTransportar(this);
    }

    internal void TransportarA(Vector3 destino, Vector3 entrada)
    {
        if (EstaViajando) return;
        EstaViajando = true;
        requiereSoltar = true;
        succion?.CancelarSuccion();
        ataque?.CancelarAtaque();
        cartas?.Cerrar();
        StartCoroutine(AnimarViaje(destino, entrada));
    }

    private IEnumerator AnimarViaje(Vector3 destino, Vector3 entrada)
    {
        escalaVisual = visual.localScale;
        posicionVisual = visual.localPosition;
        sprites = visual.GetComponentsInChildren<SpriteRenderer>(true);
        colores = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) colores[i] = sprites[i].color;
        Vector3 inicio = transform.position;
        Vector3 desplazamientoPies = PosicionPies - inicio;
        Vector3 haciaPuerta = entrada + Vector3.up * 0.7f - desplazamientoPies;
        Image flash = CrearFlash();
        if (flash != null) flash.color = new Color(0f, 0f, 0f, 0f);
        float tiempo = 0f;
        while (tiempo < duracionEntrada)
        {
            tiempo = Mathf.Min(duracionEntrada, tiempo + Time.deltaTime);
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionEntrada);
            movimiento.Teletransportar(Vector3.Lerp(inicio, haciaPuerta, t));
            float escala = Mathf.Lerp(1f, 0.72f, t);
            // Alejarse hacia el interior sin contraer el dibujo contra los pies.
            if (visual != transform)
            {
                visual.localScale = escalaVisual * escala;
                visual.localPosition = posicionVisual;
            }
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color color = Color.Lerp(colores[i], Color.black, Mathf.Clamp01(t * 2f));
                color.a = colores[i].a * (1f - Mathf.InverseLerp(0.35f, 1f, t));
                sprites[i].color = color;
            }
            if (flash != null) flash.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }

        // Cubrir el cambio de posicion antes de actualizar la camara.
        if (flash != null) flash.color = Color.black;
        movimiento.Teletransportar(destino - desplazamientoPies);
        RestaurarVisual();
        if (camaraJugador != null)
            camaraJugador.GetComponent<CamaraPasillo>()?.ActualizarSeguimiento();
        proximoUso = Time.time + Mathf.Max(0f, cooldownPersonal);
        yield return null;
        tiempo = 0f;
        while (tiempo < duracionFlash)
        {
            tiempo = Mathf.Min(duracionFlash, tiempo + Time.deltaTime);
            if (flash != null) flash.color = new Color(0f, 0f, 0f,
                1f - Mathf.SmoothStep(0f, 1f, tiempo / duracionFlash));
            yield return null;
        }
        if (lienzoFlash != null) Destroy(lienzoFlash);
        EstaViajando = false;
    }

    private Image CrearFlash()
    {
        if (camaraJugador == null) return null;
        lienzoFlash = new GameObject("FlashPuerta_" + name, typeof(Canvas));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lienzoFlash, gameObject.scene);
        Canvas canvas = lienzoFlash.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        GameObject pantalla = new GameObject("SoloPantallaPropia", typeof(RectTransform), typeof(Image));
        pantalla.transform.SetParent(lienzoFlash.transform, false);
        RectTransform rect = (RectTransform)pantalla.transform;
        rect.anchorMin = camaraJugador.rect.min;
        rect.anchorMax = camaraJugador.rect.max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image imagen = pantalla.GetComponent<Image>();
        imagen.raycastTarget = false;
        return imagen;
    }

    private void RestaurarVisual()
    {
        if (visual != null && sprites != null)
        {
            if (visual != transform)
            {
                visual.localScale = escalaVisual;
                visual.localPosition = posicionVisual;
            }
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = colores[i];
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestaurarVisual();
        EstaViajando = false;
        if (lienzoFlash != null) Destroy(lienzoFlash);
    }

    private void OnApplicationFocus(bool foco)
    {
        if (!foco)
            requiereSoltar = true;
    }

    public void BloquearHastaSoltarArriba() => requiereSoltar = true;
}
