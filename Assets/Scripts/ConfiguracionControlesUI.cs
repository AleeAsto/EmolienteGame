using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfiguracionControlesUI : MonoBehaviour
{
    [SerializeField] private GameObject contenidoAudio;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button abrir;
    [SerializeField] private Button volver;
    [SerializeField] private Button restaurar;
    [SerializeField] private Button[] teclas;
    [SerializeField] private Text estado;
    private InputActionRebindingExtensions.RebindingOperation operacion;
    private InputAction accion;
    private bool accionActiva, desactivando, terminando;
    private BaseInputModule modulo;
    private bool moduloActivo;
    private int indice = -1;
    private static int frameBloqueado = -1;
    private Coroutine espera;
    public bool Abierto => panel.activeSelf;
    public bool Escuchando => operacion != null || terminando;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() => frameBloqueado = -1;

    private void Awake()
    {
        abrir.onClick.AddListener(Abrir);
        volver.onClick.AddListener(Cerrar);
        restaurar.onClick.AddListener(Restaurar);
        for (int i = 0; i < teclas.Length; i++) { int target = i; teclas[i].onClick.AddListener(() => Escuchar(target)); }
    }

    private void OnEnable()
    {
        desactivando = false;
        contenidoAudio.SetActive(true); panel.SetActive(false);
        ConfiguracionControles.Cambiados += Actualizar;
        Actualizar();
    }

    private void Actualizar()
    {
        for (int i = 0; i < teclas.Length; i++)
            teclas[i].GetComponentInChildren<Text>().text = i == indice && operacion != null ? "PULSA UNA TECLA" : ConfiguracionControles.Nombre(i);
    }

    public void Abrir()
    {
        contenidoAudio.SetActive(false); panel.SetActive(true);
        estado.text = "Cambios guardados automaticamente. Supr elimina una tecla alternativa de P2.";
        Actualizar(); Seleccionar(teclas[0]);
    }

    public void Cerrar()
    {
        if (Escuchando) return;
        panel.SetActive(false); contenidoAudio.SetActive(true); Seleccionar(abrir);
    }

    public bool ProcesarEscape()
    {
        if (Escuchando || Time.frameCount <= frameBloqueado) return true;
        if (!Abierto) return false;
        Cerrar(); return true;
    }

    public void Escuchar(int target)
    {
        if (Escuchando || !Abierto || ConfiguracionControles.Actual == null) return;
        indice = target;
        accion = ConfiguracionControles.Actual.Resolver(target, out int binding);
        accionActiva = accion.enabled; accion.Disable();
        modulo = EventSystem.current != null ? EventSystem.current.currentInputModule : null;
        moduloActivo = modulo != null && modulo.enabled;
        if (modulo != null) modulo.enabled = false;
        foreach (var boton in teclas) boton.interactable = false;
        restaurar.interactable = volver.interactable = false;
        estado.text = "Pulsa una tecla. Escape cancela. Espera maxima: 10 segundos.";
        operacion = accion.PerformInteractiveRebinding(binding)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithMatchingEventsBeingSuppressed()
            .WithTimeout(10f)
            .OnApplyBinding((op, path) =>
            {
                ConfiguracionControles.Actual.IntentarAsignar(target, path, out string mensaje);
                estado.text = mensaje;
            })
            .OnCancel(op => { estado.text = "Cambio cancelado. Se conserva la tecla anterior."; Terminar(); })
            .OnComplete(op => Terminar());
        operacion.Start(); Actualizar();
    }

    private void Terminar()
    {
        operacion?.Dispose(); operacion = null;
        frameBloqueado = Time.frameCount + 1;
        if (desactivando) { RestaurarEntrada(); return; }
        terminando = true;
        espera = StartCoroutine(EsperarSoltar());
    }

    private IEnumerator EsperarSoltar()
    {
        yield return null;
        while (Keyboard.current != null && Keyboard.current.anyKey.isPressed) yield return null;
        RestaurarEntrada(); frameBloqueado = Time.frameCount + 1;
        if (indice >= 0) Seleccionar(teclas[indice]);
        Actualizar(); espera = null;
    }

    private void RestaurarEntrada()
    {
        if (accionActiva && accion != null) accion.Enable();
        accion = null; accionActiva = false;
        if (modulo != null && moduloActivo) modulo.enabled = true;
        modulo = null; moduloActivo = false;
        foreach (var boton in teclas) boton.interactable = true;
        restaurar.interactable = volver.interactable = true;
        terminando = false;
    }

    public void Restaurar()
    {
        if (Escuchando) return;
        ConfiguracionControles.Actual?.Restaurar();
        estado.text = "Controles predeterminados restaurados para ambos jugadores.";
        Actualizar();
    }

    private void OnApplicationFocus(bool foco) { if (!foco && operacion != null) operacion.Cancel(); }

    private static void Seleccionar(Selectable control)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(control.gameObject);
    }

    private void OnDisable()
    {
        desactivando = true;
        ConfiguracionControles.Cambiados -= Actualizar;
        if (espera != null) { StopCoroutine(espera); espera = null; }
        operacion?.Cancel(); operacion?.Dispose(); operacion = null;
        RestaurarEntrada();
        panel.SetActive(false); contenidoAudio.SetActive(true);
    }
}
