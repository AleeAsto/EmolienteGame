using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class CicloPartida : MonoBehaviour
{
    public static int VictoriasJugador1 { get; private set; }
    public static int VictoriasJugador2 { get; private set; }
    public static bool MostrarVictorias { get; private set; }
    private static bool siguienteEsRevancha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ReiniciarVictorias()
    {
        VictoriasJugador1 = VictoriasJugador2 = 0;
        MostrarVictorias = siguienteEsRevancha = false;
    }

    public static void PrepararRevancha()
    {
        siguienteEsRevancha = true;
    }

    [SerializeField, Min(1f)] private float duracionSegundos = 180f;
    [SerializeField] private AlmasJugador jugador1;
    [SerializeField] private AlmasJugador jugador2;
    [SerializeField] private Text textoTiempo;
    [SerializeField] private CuentaRegresivaInicial cuentaRegresiva;
    [Header("Presentacion VS y marcos")]
    [SerializeField] private Sprite retratoMorado;
    [SerializeField] private Sprite retratoRojo;
    [SerializeField] private string nombreJugador1 = "JUGADOR 1";
    [SerializeField] private string nombreJugador2 = "JUGADOR 2";
    [SerializeField, Min(0.1f)] private float duracionVS = 1.3f;
    [SerializeField, Min(0.1f)] private float duracionAperturaVS = 0.85f;
    private PresentacionVersus presentacionVS;
    [Header("Victoria")]
    [SerializeField] private Sprite victoriaJugador1;
    [SerializeField] private Sprite victoriaJugador2;
    [Header("Empate")]
    [SerializeField] private Sprite empateJugador1;
    [SerializeField] private Sprite empateJugador2;
    public bool TieneVictoriaPersonalizada => presentacionVS != null && victoriaJugador1 != null && victoriaJugador2 != null;

    public float TiempoRestante { get; private set; }
    public bool Terminada { get; private set; }
    public bool EnCurso { get; private set; }
    public float DuracionSegundos => duracionSegundos;
    private bool controlaTiempo;
    // 1 = jugador 1, 2 = jugador 2, 0 = empate.
    public event Action<int> PartidaTerminada;

    private void Awake()
    {
        if (siguienteEsRevancha)
        {
            siguienteEsRevancha = false;
            MostrarVictorias = true;
        }
        else ReiniciarVictorias();

        if (jugador1 == null || jugador2 == null || textoTiempo == null || cuentaRegresiva == null)
        {
            Debug.LogError("Asigna ambos jugadores, el timer y el texto de cuenta regresiva.", this);
            enabled = false;
            return;
        }

        TiempoRestante = Mathf.Max(1f, duracionSegundos);
        ActualizarTexto();
        controlaTiempo = true;
        Time.timeScale = 0f;
        cuentaRegresiva.Ocultar();
        if (retratoMorado != null && retratoRojo != null)
        {
            var vistas = new System.Collections.Generic.List<Camera>();
            foreach (CamaraPasillo seguimiento in FindObjectsByType<CamaraPasillo>(FindObjectsSortMode.None))
                if (seguimiento.gameObject.scene == gameObject.scene)
                    vistas.Add(seguimiento.GetComponent<Camera>());
            var objeto = new GameObject("PresentacionVersus");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(objeto, gameObject.scene);
            presentacionVS = objeto.AddComponent<PresentacionVersus>();
            presentacionVS.Inicializar(retratoMorado, retratoRojo, nombreJugador1, nombreJugador2, vistas.ToArray());
        }
    }

    private IEnumerator Start()
    {
        if (!controlaTiempo)
            yield break;

        while (TransicionEscena.EnCurso) yield return null;

        if (presentacionVS != null)
            yield return presentacionVS.Mostrar(duracionVS, duracionAperturaVS);

        for (int numero = 3; numero >= 1; numero--)
        {
            yield return cuentaRegresiva.Mostrar(numero.ToString(), 1f);
        }

        EnCurso = true;
        Time.timeScale = 1f;
        yield return cuentaRegresiva.Mostrar("GO!", 0.6f);
    }

    private void Update()
    {
        if (!EnCurso || Terminada || Time.timeScale <= 0f)
            return;

        TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
        // Atajo de prueba: adelantar el final sin aumentar el tiempo restante.
        if (Application.isFocused && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame &&
            !ConfiguracionControles.TeclaEnUso(Key.T))
            TiempoRestante = Mathf.Min(TiempoRestante, 3f);

        ActualizarTexto();
        if (TiempoRestante > 0f)
            return;

        Terminada = true;
        EnCurso = false;
        Time.timeScale = 0f;
        float puntos1 = jugador1.Cantidad;
        float puntos2 = jugador2.Cantidad;
        int ganador = Mathf.Approximately(puntos1, puntos2) ? 0 : (puntos1 > puntos2 ? 1 : 2);
        if (ganador == 1) VictoriasJugador1++;
        else if (ganador == 2) VictoriasJugador2++;
        string resultado = ganador == 0 ? "Empate" : $"El jugador {ganador} gano";
        Debug.Log($"Fin de la partida. {resultado}. P1: {puntos1:0.#} | P2: {puntos2:0.#}", this);
        PartidaTerminada?.Invoke(ganador);
        if (TieneVictoriaPersonalizada)
            StartCoroutine(presentacionVS.MostrarVictoria(ganador, victoriaJugador1, victoriaJugador2, empateJugador1, empateJugador2,
                ganador == 1 ? nombreJugador1 : nombreJugador2, Revancha, VolverMenu));
    }

    private void Revancha()
    {
        if (TransicionEscena.EnCurso) return;
        PrepararRevancha();
        TransicionEscena.Cargar(gameObject.scene.name, true);
    }

    private void VolverMenu()
    {
        foreach (MenuPartida menu in FindObjectsByType<MenuPartida>(FindObjectsSortMode.None))
            if (menu.gameObject.scene == gameObject.scene) { menu.VolverMenu(); return; }
        TransicionEscena.Cargar("MenuPrincipal");
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (cuentaRegresiva != null)
            cuentaRegresiva.Ocultar();
        EnCurso = false;
        // No dejar la siguiente escena (o partida) con el tiempo congelado.
        if (controlaTiempo)
            Time.timeScale = 1f;
    }

    private void ActualizarTexto()
    {
        int segundos = Mathf.CeilToInt(TiempoRestante);
        textoTiempo.text = $"{segundos / 60:00}:{segundos % 60:00}";
    }
}
