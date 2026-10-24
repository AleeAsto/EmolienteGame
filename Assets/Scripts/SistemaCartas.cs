using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-300)]
public class SistemaCartas : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CicloPartida partida;
    [SerializeField] private ManoCartasJugador jugador1;
    [SerializeField] private ManoCartasJugador jugador2;
    [SerializeField] private Camera camara1;
    [SerializeField] private Camera camara2;
    [SerializeField] private DefinicionCarta[] catalogo;
    [Header("Entrega por desventaja")]
    [SerializeField, Range(0f, 1f)] private float inicioFraccionPartida = 1f / 3f;
    [SerializeField, Min(0f)] private float diferenciaMinima = 25f;
    [SerializeField, Range(0f, 1f)] private float proporcionMaxima = 0.75f;
    [SerializeField, Min(0f)] private float persistencia = 8f;
    [SerializeField, Min(0f)] private float esperaEntreEntregas = 25f;
    [SerializeField, Min(0f)] private float cierreEntregas = 10f;
    [Header("Controles y presentacion")]
    [SerializeField, Min(0.1f)] private float tiempoMantener = 0.35f;
    [SerializeField, Min(0.1f)] private float duracionGiro = 2f;
    [SerializeField, Min(0.1f)] private float duracionVistaFrente = 1.2f;
    [SerializeField] private bool atajosDesarrollo = true;

    private readonly float[] desventaja = new float[2];
    private readonly float[] espera = new float[2];
    private readonly List<Lanzamiento> cola = new List<Lanzamiento>();
    private VistaCartas vista;
    private bool presentando;
    private float tiempoPresentacion;
    private Lanzamiento actual;

    public bool EnCurso => isActiveAndEnabled && partida != null && partida.EnCurso &&
        !partida.Terminada && Time.timeScale > 0f;
    public float TiempoMantener => tiempoMantener;

    private struct Lanzamiento
    {
        public ManoCartasJugador jugador;
        public DefinicionCarta carta;
        public int frame;
        public Vector2 origenPantalla;
    }

    private void Awake()
    {
        if (partida == null || jugador1 == null || jugador2 == null || camara1 == null || camara2 == null ||
            catalogo == null || catalogo.Length == 0 || System.Array.Exists(catalogo, c => c == null))
        {
            Debug.LogError("Asigna partida, manos, camaras y un catalogo sin cartas vacias.", this);
            enabled = false;
            return;
        }
        jugador1.Inicializar(this);
        jugador2.Inicializar(this);
    }

    private void Start()
    {
        vista = new GameObject("PresentacionCartas").AddComponent<VistaCartas>();
        // Debe ser un Canvas raiz: anidarlo en HUDVersus heredaba el tamano y
        // el recorte del marcador central, comprimiendo las cartas sobre la barra.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(vista.gameObject, gameObject.scene);
        vista.Inicializar(jugador1, jugador2, camara1, camara2);
    }

    private void Update()
    {
        if (partida.Terminada)
        {
            CancelarPresentaciones();
            return;
        }
        if (!EnCurso)
            return;

        if (atajosDesarrollo && (Application.isEditor || Debug.isDebugBuild) && Application.isFocused && Keyboard.current != null)
        {
            // Atajos de prueba, sin alterar el reloj de entregas normales.
            if (Keyboard.current.gKey.wasPressedThisFrame) jugador1.Recibir(catalogo[0]);
            if (Keyboard.current.hKey.wasPressedThisFrame) jugador2.Recibir(catalogo[0]);
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                jugador1.Almas.Recibir(100f);
                jugador2.Almas.Recibir(100f);
            }
        }

        EvaluarEntrega(0, jugador1, jugador2);
        EvaluarEntrega(1, jugador2, jugador1);
        ActualizarPresentacion();
    }

    private void EvaluarEntrega(int indice, ManoCartasJugador jugador, ManoCartasJugador rival)
    {
        espera[indice] = Mathf.Max(0f, espera[indice] - Time.deltaTime);
        float transcurrido = partida.DuracionSegundos - partida.TiempoRestante;
        bool elegible = transcurrido >= partida.DuracionSegundos * inicioFraccionPartida &&
            partida.TiempoRestante > cierreEntregas && espera[indice] <= 0f &&
            jugador.Cantidad < ManoCartasJugador.Capacidad && rival.Almas.Cantidad > jugador.Almas.Cantidad &&
            rival.Almas.Cantidad - jugador.Almas.Cantidad >= diferenciaMinima &&
            jugador.Almas.Cantidad <= rival.Almas.Cantidad * proporcionMaxima;
        if (!elegible)
        {
            desventaja[indice] = 0f;
            return;
        }
        desventaja[indice] += Time.deltaTime;
        if (desventaja[indice] < persistencia)
            return;
        if (jugador.Recibir(catalogo[Random.Range(0, catalogo.Length)]))
            espera[indice] = esperaEntreEntregas;
        desventaja[indice] = 0f;
    }

    public void Presentar(ManoCartasJugador jugador, DefinicionCarta carta)
    {
        if (EnCurso && carta != null && (jugador == jugador1 || jugador == jugador2))
        {
            cola.Add(new Lanzamiento { jugador = jugador, carta = carta, frame = Time.frameCount,
                origenPantalla = vista != null ? vista.ObtenerOrigenLanzamiento(jugador) : Vector2.one * 0.5f });
            cola.Sort((a, b) => a.frame != b.frame ? a.frame.CompareTo(b.frame) :
                (a.jugador == jugador1 ? 0 : 1).CompareTo(b.jugador == jugador1 ? 0 : 1));
        }
    }

    private void ActualizarPresentacion()
    {
        if (vista == null)
            return;
        if (!presentando && cola.Count > 0)
        {
            actual = cola[0];
            cola.RemoveAt(0);
            tiempoPresentacion = 0f;
            presentando = true;
        }
        if (!presentando)
            return;
        tiempoPresentacion += Time.deltaTime;
        vista.Animar(actual.jugador, actual.carta, actual.origenPantalla, tiempoPresentacion,
            duracionVistaFrente, duracionGiro);
        if (tiempoPresentacion >= VistaCartas.DuracionAnimacion(duracionVistaFrente, duracionGiro))
        {
            vista.OcultarPresentacion();
            presentando = false;
            // Punto de extension para aplicar la habilidad al llegar. Prueba: ningun efecto.
        }
    }

    private void CancelarPresentaciones()
    {
        cola.Clear();
        presentando = false;
        jugador1?.Cerrar();
        jugador2?.Cerrar();
        if (vista != null) vista.OcultarPresentacion();
    }

    private void OnDisable()
    {
        CancelarPresentaciones();
        if (vista != null) vista.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (vista != null) vista.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (vista != null) Destroy(vista.gameObject);
    }
}
