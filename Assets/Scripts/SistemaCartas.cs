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
    [Tooltip("Sin asignar se usa el catalogo compartido de Resources/CatalogoCartas.")]
    [SerializeField] private CatalogoCartas catalogoCompartido;
    [SerializeField, HideInInspector] private DefinicionCarta[] catalogo;
    private bool catalogoInicializado;
    [Header("Repartos a ambos (segundos transcurridos)")]
    [SerializeField] private float[] momentosReparto = { 60f, 90f, 120f, 150f };
    [SerializeField, HideInInspector] private List<int> repartosRealizados = new List<int>();
    [Header("Entrega por desventaja")]
    [SerializeField, Range(0f, 1f)] private float inicioFraccionPartida = 1f / 3f;
    [SerializeField, Min(0f)] private float diferenciaMinima = 25f;
    [SerializeField, Range(0f, 1f)] private float proporcionMaxima = 0.75f;
    [SerializeField, Min(0f)] private float persistencia = 8f;
    [SerializeField, Min(0f)] private float esperaEntreEntregas = 25f;
    [SerializeField, Min(0f)] private float cierreEntregas = 10f;
    [Header("Cartas potentes")]
    [SerializeField, Range(0.5f, 1f)] private float inicioPotentes = 0.5f;
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
    private EfectosJugador efectos1;
    private EfectosJugador efectos2;

    public bool EnCurso => isActiveAndEnabled && partida != null && partida.EnCurso &&
        !partida.Terminada && Time.timeScale > 0f;
    public float TiempoMantener => tiempoMantener;

    private struct Lanzamiento
    {
        public ManoCartasJugador jugador;
        public DefinicionCarta carta;
        public int frame;
        public Vector2 origenPantalla;
        public bool esEntrega;
    }

    private void Awake()
    {
        CargarCatalogo();
        if (partida == null || jugador1 == null || jugador2 == null || camara1 == null || camara2 == null ||
            catalogo == null || catalogo.Length == 0 || System.Array.Exists(catalogo, c => c == null))
        {
            Debug.LogError("Asigna partida, manos, camaras y un catalogo sin cartas vacias.", this);
            enabled = false;
            return;
        }
        jugador1.Inicializar(this);
        jugador2.Inicializar(this);
        efectos1 = PrepararEfectos(jugador1);
        efectos2 = PrepararEfectos(jugador2);
        PrepararIlusiones(jugador1, efectos1, camara1, camara2, 8);
        PrepararIlusiones(jugador2, efectos2, camara2, camara1, 9);
        repartosRealizados.Clear();
    }

    private void CargarCatalogo()
    {
        if (catalogoInicializado) return;
        if (catalogoCompartido == null) catalogoCompartido = Resources.Load<CatalogoCartas>("CatalogoCartas");
        if (catalogoCompartido != null && catalogoCompartido.Cartas != null && catalogoCompartido.Cartas.Length > 0)
        {
            var validas = System.Array.FindAll(catalogoCompartido.Cartas, c => c != null);
            if (validas.Length == 0) return;
            if (validas.Length != catalogoCompartido.Cartas.Length)
                Debug.LogWarning("El catalogo compartido contiene referencias vacias; se omiten sin volver a la lista antigua de la escena.", this);
            catalogo = validas;
            catalogoInicializado = true;
        }
    }

    private EfectosJugador PrepararEfectos(ManoCartasJugador jugador)
    {
        EfectosJugador efectos = jugador.GetComponent<EfectosJugador>();
        if (efectos == null) efectos = jugador.gameObject.AddComponent<EfectosJugador>();
        efectos.Inicializar(partida);
        return efectos;
    }

    private void PrepararIlusiones(ManoCartasJugador jugador, EfectosJugador efectos,
        Camera propia, Camera otra, int capa)
    {
        GeneradorEstudiantes generador = null;
        foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
        {
            generador = raiz.GetComponentInChildren<GeneradorEstudiantes>();
            if (generador != null) break;
        }
        var ilusiones = jugador.GetComponent<IlusionesConfusionJugador>();
        if (ilusiones == null) ilusiones = jugador.gameObject.AddComponent<IlusionesConfusionJugador>();
        ilusiones.Inicializar(efectos, propia, jugador.GetComponent<MovimientoJugador2D>().ZonaMovimiento,
            generador, capa);
        propia.cullingMask |= 1 << capa;
        otra.cullingMask &= ~(1 << capa);
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
            if (efectos1 != null) efectos1.Limpiar();
            if (efectos2 != null) efectos2.Limpiar();
            return;
        }
        if (!EnCurso)
            return;
        CargarCatalogo();

        if (atajosDesarrollo && (Application.isEditor || Debug.isDebugBuild) && Application.isFocused && Keyboard.current != null)
        {
            // Atajos de prueba, sin alterar el reloj de entregas normales.
            bool forzarPotente = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            if (Keyboard.current.gKey.wasPressedThisFrame) EntregarCarta(jugador1, true, forzarPotente ? true : (bool?)null);
            if (Keyboard.current.hKey.wasPressedThisFrame) EntregarCarta(jugador2, true, forzarPotente ? true : (bool?)null);
            if (Keyboard.current.fKey.wasPressedThisFrame) EntregarCarta(jugador1, true, forzarPotente, EfectoCarta.Perfume);
            if (Keyboard.current.uKey.wasPressedThisFrame) EntregarCarta(jugador2, true, forzarPotente, EfectoCarta.Perfume);
            if (Keyboard.current.rKey.wasPressedThisFrame) EntregarCarta(jugador1, true, forzarPotente, EfectoCarta.BombaApestosa);
            if (Keyboard.current.pKey.wasPressedThisFrame) EntregarCarta(jugador2, true, forzarPotente, EfectoCarta.BombaApestosa);
            if (Keyboard.current.digit1Key.wasPressedThisFrame) EntregarCarta(jugador1, true, false, EfectoCarta.Latigo);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) EntregarCarta(jugador2, true, false, EfectoCarta.Latigo);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) EntregarCarta(jugador1, true, false, EfectoCarta.IntercambioPosicion);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) EntregarCarta(jugador2, true, false, EfectoCarta.IntercambioPosicion);
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                jugador1.Almas.Recibir(100f);
                jugador2.Almas.Recibir(100f);
            }
        }

        EvaluarRepartosProgramados();
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
            jugador.EspaciosOcupados < ManoCartasJugador.Capacidad && rival.Almas.Cantidad > jugador.Almas.Cantidad &&
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
        if (EntregarCarta(jugador))
            espera[indice] = esperaEntreEntregas;
        desventaja[indice] = 0f;
    }

    public bool PuedeEntregar(ManoCartasJugador jugador, DefinicionCarta carta, bool debug = false)
    {
        if (!EnCurso || carta == null || (jugador != jugador1 && jugador != jugador2) ||
            jugador.EspaciosOcupados >= ManoCartasJugador.Capacidad)
            return false;
        if (!carta.EsPotente) return true;
        return !jugador.TienePotente && (debug ||
            partida.DuracionSegundos - partida.TiempoRestante >= partida.DuracionSegundos * inicioPotentes);
    }

    private void EvaluarRepartosProgramados()
    {
        if (!EnCurso || momentosReparto == null) return;
        float transcurrido = partida.DuracionSegundos - partida.TiempoRestante;
        for (int i = 0; i < momentosReparto.Length; i++)
        {
            float momento = momentosReparto[i];
            if (float.IsNaN(momento) || float.IsInfinity(momento) || momento < 0f ||
                momento >= partida.DuracionSegundos || transcurrido < momento || repartosRealizados.Contains(i))
                continue;
            // Se consume el evento aunque una mano este llena; no guarda entregas atrasadas.
            repartosRealizados.Add(i);
            RepartirAmbos();
        }
    }

    private bool HayCartaElegible(ManoCartasJugador jugador, bool? potente = null)
    {
        foreach (DefinicionCarta carta in catalogo)
            if (PuedeEntregar(jugador, carta) && (!potente.HasValue || carta.EsPotente == potente.Value))
                return true;
        return false;
    }

    private void RepartirAmbos()
    {
        bool recibe1 = HayCartaElegible(jugador1);
        bool recibe2 = HayCartaElegible(jugador2);
        if (!recibe1 && !recibe2) return;
        // Sortear un nivel comun; las habilidades concretas pueden ser distintas.
        bool normal = (!recibe1 || HayCartaElegible(jugador1, false)) &&
            (!recibe2 || HayCartaElegible(jugador2, false));
        bool potente = (!recibe1 || HayCartaElegible(jugador1, true)) &&
            (!recibe2 || HayCartaElegible(jugador2, true));
        if (!normal && !potente) return;
        bool nivelPotente = potente && (!normal || Random.value < 0.5f);
        if (recibe1) EntregarCarta(jugador1, false, nivelPotente);
        if (recibe2) EntregarCarta(jugador2, false, nivelPotente);
    }

    public bool EntregarCarta(ManoCartasJugador jugador, bool debug = false, bool? potente = null, EfectoCarta? efecto = null)
    {
        CargarCatalogo();
        var candidatas = new List<DefinicionCarta>();
        foreach (DefinicionCarta carta in catalogo)
            if (PuedeEntregar(jugador, carta, debug) && (!potente.HasValue || carta.EsPotente == potente.Value) &&
                (!efecto.HasValue || carta.Efecto == efecto.Value))
                candidatas.Add(carta);
        if (candidatas.Count == 0)
        {
            if (debug) Debug.Log($"[Cartas debug] {jugador.name}: sin cartas elegibles. Cupos {jugador.EspaciosOcupados}/3; potente en mano o pendiente: {jugador.TienePotente}; forzar potente: {potente == true}.", this);
            return false;
        }
        DefinicionCarta elegida = candidatas[Random.Range(0, candidatas.Count)];
        if (debug)
        {
            int potentes = candidatas.FindAll(c => c.EsPotente).Count;
            Debug.Log($"[Cartas debug] {jugador.name}: {elegida.TituloVisible}; costo {elegida.PuntosNecesarios}; {elegida.DuracionEfecto} s. Candidatas: {candidatas.Count - potentes} normales, {potentes} potentes. Tiempo ignorado.", this);
        }
        if (!elegida.EsPotente) return jugador.Recibir(elegida);
        if (!jugador.ReservarEntrega(elegida)) return false;
        Encolar(new Lanzamiento { jugador = jugador, carta = elegida, frame = Time.frameCount, esEntrega = true });
        return true;
    }

    private void Encolar(Lanzamiento lanzamiento)
    {
        cola.Add(lanzamiento);
        cola.Sort((a, b) => a.frame != b.frame ? a.frame.CompareTo(b.frame) :
            (a.jugador == jugador1 ? 0 : 1).CompareTo(b.jugador == jugador1 ? 0 : 1));
    }

    public void Presentar(ManoCartasJugador jugador, DefinicionCarta carta)
    {
        if (EnCurso && carta != null && (jugador == jugador1 || jugador == jugador2))
        {
            Encolar(new Lanzamiento { jugador = jugador, carta = carta, frame = Time.frameCount,
                origenPantalla = vista != null ? vista.ObtenerOrigenLanzamiento(jugador) : Vector2.one * 0.5f });
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
        if (actual.esEntrega)
        {
            vista.AnimarEntregaPotente(actual.jugador, actual.carta, tiempoPresentacion);
            if (tiempoPresentacion >= VistaCartas.DuracionEntregaPotente)
            {
                vista.OcultarPresentacion();
                presentando = false;
                actual.jugador.CompletarEntrega(actual.carta);
            }
            return;
        }
        vista.Animar(actual.jugador, actual.carta, actual.origenPantalla, tiempoPresentacion,
            duracionVistaFrente, duracionGiro);
        if (tiempoPresentacion >= VistaCartas.DuracionAnimacion(duracionVistaFrente, duracionGiro))
        {
            vista.OcultarPresentacion();
            presentando = false;
            AplicarCarta(actual.jugador, actual.carta);
        }
    }

    private void AplicarCarta(ManoCartasJugador lanzador, DefinicionCarta carta)
    {
        if (!EnCurso)
            return;
        bool objetivoEsP1 = carta.Destino == DestinoCarta.Propio
            ? lanzador == jugador1 : lanzador != jugador1;
        EfectosJugador objetivo = objetivoEsP1 ? efectos1 : efectos2;
        if (carta.Efecto == EfectoCarta.Velocidad)
            objetivo?.AplicarVelocidad(carta.MultiplicadorVelocidad, carta.DuracionEfecto);
        else if (carta.Efecto == EfectoCarta.Confusion)
            objetivo?.AplicarConfusion(carta.DuracionEfecto);
        else if (carta.Efecto == EfectoCarta.Perfume || carta.Efecto == EfectoCarta.BombaApestosa)
            objetivo?.AplicarOlor(carta.Efecto == EfectoCarta.BombaApestosa, carta.DuracionEfecto,
                carta.RadioOlor, carta.MultiplicadorRetirada, carta.MultiplicadorAtraccion);
        else if (carta.Efecto == EfectoCarta.Latigo)
            objetivo?.GetComponent<MarcaLatigoJugador>()?.Aplicar(lanzador.GetComponent<AtaqueJugador>(),
                carta.DuracionEfecto, carta.AlmasExtraLatigo, carta.EmpujonLatigo);
        else if (carta.Efecto == EfectoCarta.IntercambioPosicion)
            IntercambiarPosiciones();
    }

    private void IntercambiarPosiciones()
    {
        var movimiento1 = jugador1.GetComponent<MovimientoJugador2D>();
        var movimiento2 = jugador2.GetComponent<MovimientoJugador2D>();
        Vector3 posicion1 = jugador1.transform.position;
        Vector3 posicion2 = jugador2.transform.position;
        foreach (var jugador in new[] { jugador1, jugador2 })
        {
            jugador.GetComponent<SuccionJugador>()?.CancelarSuccion();
            jugador.GetComponent<AtaqueJugador>()?.CancelarAtaque();
            jugador.Cerrar();
            jugador.GetComponent<UsarPuertaJugador>()?.BloquearHastaSoltarArriba();
        }
        movimiento1.Teletransportar(posicion2);
        movimiento2.Teletransportar(posicion1);
        camara1.GetComponent<CamaraPasillo>()?.ActualizarSeguimiento();
        camara2.GetComponent<CamaraPasillo>()?.ActualizarSeguimiento();
    }

    private void CancelarPresentaciones()
    {
        cola.Clear();
        presentando = false;
        jugador1?.Cerrar();
        jugador2?.Cerrar();
        jugador1?.CancelarEntregas();
        jugador2?.CancelarEntregas();
        if (vista != null) vista.OcultarPresentacion();
    }

    private void OnDisable()
    {
        CancelarPresentaciones();
        // Unity conserva el wrapper C# de los componentes destruidos; evitar
        // ?. aqui porque no comprueba la nulidad propia de UnityEngine.Object.
        if (efectos1 != null) efectos1.Limpiar();
        if (efectos2 != null) efectos2.Limpiar();
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
