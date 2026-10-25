using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuPrincipal : MonoBehaviour
{
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
        if (!cargando && pantallaActual != 0 && Application.isFocused &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReproducirBoton();
            GuardarOpciones();
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
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded(escenaPartida))
        {
            estado.text = $"Falta incluir {escenaPartida} en la lista de escenas del build.";
            return;
        }
        cargando = true;
        estado.text = "Cargando partida…";
        foreach (Button boton in pantallas[0].GetComponentsInChildren<Button>())
            boton.interactable = false;
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(escenaPartida);
    }
}
