using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuPrincipal : MonoBehaviour
{
    public static bool DebugHabilitado { get; private set; }
    private const string CodigoDebug = "150506";
    private int digitosDebug;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BloquearDebug() => DebugHabilitado = false;

    [SerializeField] private GameObject[] pantallas;
    [SerializeField] private Button[] primerosBotones;
    [SerializeField] private Slider volumen;
    [SerializeField] private Text estado;
    [SerializeField] private string escenaPartida = "PartidaIntegrada";
    private bool cargando;
    private int pantallaActual;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("VolumenGeneral", 1f);
        volumen.SetValueWithoutNotify(AudioListener.volume);
        foreach (var boton in GetComponentsInChildren<Button>(true))
            boton.onClick.AddListener(ReproducirBoton);
        Mostrar(0);
    }

    private void ReproducirBoton() => AudioJuego.Instancia?.BotonMenu();

    private void Update()
    {
        LeerCodigoDebug();
        if (!cargando && pantallaActual != 0 && Application.isFocused &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            var controles = pantallas[pantallaActual].GetComponent<ConfiguracionControlesUI>();
            if (controles != null && controles.ProcesarEscape()) return;
            ReproducirBoton();
            if (pantallaActual == 5) Mostrar(4);
            else if (pantallaActual == 4) Mostrar(0);
            else GuardarOpciones();
        }
    }

    private void LeerCodigoDebug()
    {
        if (DebugHabilitado) return;
        if (cargando || pantallaActual != 0 || !Application.isFocused || Keyboard.current == null)
        {
            digitosDebug = 0;
            return;
        }

        foreach (var tecla in Keyboard.current.allKeys)
        {
            if (!tecla.wasPressedThisFrame) continue;
            char digito;
            switch (tecla.keyCode)
            {
                case Key.Numpad0: digito = '0'; break;
                case Key.Numpad1: digito = '1'; break;
                case Key.Numpad2: digito = '2'; break;
                case Key.Numpad3: digito = '3'; break;
                case Key.Numpad4: digito = '4'; break;
                case Key.Numpad5: digito = '5'; break;
                case Key.Numpad6: digito = '6'; break;
                case Key.Numpad7: digito = '7'; break;
                case Key.Numpad8: digito = '8'; break;
                case Key.Numpad9: digito = '9'; break;
                default: digitosDebug = 0; continue;
            }
            digitosDebug = digito == CodigoDebug[digitosDebug] ? digitosDebug + 1 : (digito == '1' ? 1 : 0);
            if (digitosDebug != CodigoDebug.Length) continue;
            DebugHabilitado = true;
            AudioJuego.Instancia?.BotonMenu();
            break;
        }
    }

    public void Mostrar(int indice)
    {
        if (cargando || indice < 0 || indice >= pantallas.Length) return;
        pantallaActual = indice;
        for (int i = 0; i < pantallas.Length; i++)
            pantallas[i].SetActive(i == indice);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerosBotones[indice].gameObject);
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("VolumenGeneral", valor);
    }

    public void GuardarOpciones()
    {
        PlayerPrefs.Save();
        Mostrar(0);
    }

    public void Salir()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Jugar()
    {
        if (pantallas.Length > 4) Mostrar(4);
        else JugarPvP();
    }

    public void ElegirCPU() => Mostrar(5);

    public void JugarPvP()
    {
        if (cargando) return;
        ConfiguracionPartida.ElegirPvP();
        CargarPartida();
    }

    public void JugarCPU(int dificultad)
    {
        if (cargando || dificultad < 0 || dificultad > 2) return;
        ConfiguracionPartida.ElegirCPU((DificultadCPU)dificultad);
        CargarPartida();
    }

    private void CargarPartida()
    {
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded(escenaPartida))
        {
            estado.text = $"Falta incluir {escenaPartida} en la lista de escenas del build.";
            return;
        }
        cargando = true;
        CicloPartida.ReiniciarVictorias();
        estado.text = "Cargando partida…";
        foreach (Button boton in GetComponentsInChildren<Button>(true))
            boton.interactable = false;
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        TransicionEscena.Cargar(escenaPartida);
    }
}
