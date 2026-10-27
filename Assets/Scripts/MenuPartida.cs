using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class MenuPartida : MonoBehaviour
{
    [SerializeField] private CicloPartida partida;
    [SerializeField] private AlmasJugador jugador1;
    [SerializeField] private AlmasJugador jugador2;
    [SerializeField] private GameObject pausa;
    [SerializeField] private GameObject opciones;
    [SerializeField] private GameObject resultado;
    [SerializeField] private Button botonPausa;
    [SerializeField] private Button continuar;
    [SerializeField] private Button volverOpciones;
    [SerializeField] private Button revancha;
    [SerializeField] private Text tituloResultado;
    [SerializeField] private Text puntuacion1;
    [SerializeField] private Text puntuacion2;
    [SerializeField] private Text mensaje;
    [SerializeField] private string escenaMenu = "MenuAudioMenus";
    private bool pausado;
    private bool cargando;
    public bool EstaPausado => pausado;

    private void Awake()
    {
        pausa.SetActive(false);
        opciones.SetActive(false);
        resultado.SetActive(false);
    }

    private void OnEnable() => partida.PartidaTerminada += MostrarResultado;

    private void Start()
    {
        foreach (var boton in GetComponentsInChildren<Button>(true))
            boton.onClick.AddListener(SonarBoton);
    }

    private void Update()
    {
        botonPausa.interactable = !cargando && !pausado && partida.EnCurso && !partida.Terminada;
        if (!cargando && Application.isFocused && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            var controles = opciones.GetComponent<ConfiguracionControlesUI>();
            if (controles != null && controles.ProcesarEscape()) return;
            if (opciones.activeSelf) VolverPausa();
            else if (pausado) Continuar();
            else Pausar();
        }
    }

    private void SonarBoton() => AudioJuego.Instancia?.BotonMenu();

    public void Pausar()
    {
        if (cargando || pausado || !partida.EnCurso || partida.Terminada) return;
        pausado = true;
        botonPausa.interactable = false;
        Time.timeScale = 0f;
        AudioJuego.Instancia?.PausarEfectos(true);
        pausa.SetActive(true);
        Seleccionar(continuar);
    }

    public void Continuar()
    {
        if (cargando || !pausado || partida.Terminada) return;
        pausa.SetActive(false);
        opciones.SetActive(false);
        pausado = false;
        botonPausa.interactable = true;
        Time.timeScale = 1f;
        AudioJuego.Instancia?.PausarEfectos(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void MostrarOpciones()
    {
        if (cargando || !pausado) return;
        pausa.SetActive(false);
        opciones.SetActive(true);
        Seleccionar(opciones.GetComponentInChildren<Slider>());
    }

    public void VolverPausa()
    {
        if (cargando || !pausado) return;
        opciones.SetActive(false);
        pausa.SetActive(true);
        Seleccionar(volverOpciones);
    }

    private void MostrarResultado(int ganador)
    {
        pausado = false;
        botonPausa.interactable = false;
        pausa.SetActive(false);
        opciones.SetActive(false);
        AudioJuego.Instancia?.PausarEfectos(true);
        if (partida.TieneVictoriaPersonalizada) return;
        tituloResultado.text = ganador == 0 ? "EMPATE" :
            ganador == 2 && ConfiguracionPartida.ContraCPU ? "GANA LA CPU" : $"GANA JUGADOR {ganador}";
        tituloResultado.color = ganador == 0 ? new Color32(244, 187, 83, 255) :
            ganador == 1 ? new Color32(120, 197, 255, 255) : new Color32(255, 117, 117, 255);
        puntuacion1.text = jugador1.Cantidad.ToString("0.#");
        puntuacion2.text = jugador2.Cantidad.ToString("0.#");
        resultado.SetActive(true);
        Seleccionar(revancha);
    }

    public void Revancha() => CargarEscena(gameObject.scene.name, true);
    public void VolverMenu() => CargarEscena(escenaMenu);

    private void CargarEscena(string nombre, bool reiniciarMusica = false)
    {
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded(nombre))
        {
            mensaje.text = $"No se pudo cargar {nombre}.";
            return;
        }
        cargando = true;
        if (reiniciarMusica) CicloPartida.PrepararRevancha();
        foreach (var boton in GetComponentsInChildren<Button>(true)) boton.interactable = false;
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        TransicionEscena.Cargar(nombre, reiniciarMusica);
    }

    private static void Seleccionar(Selectable control)
    {
        if (EventSystem.current != null && control != null)
            EventSystem.current.SetSelectedGameObject(control.gameObject);
    }

    private void OnDisable()
    {
        if (partida != null) partida.PartidaTerminada -= MostrarResultado;
        if (pausado) Time.timeScale = 1f;
    }
}
