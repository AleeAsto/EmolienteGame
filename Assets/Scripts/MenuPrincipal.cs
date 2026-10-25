using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private GameObject[] pantallas;
    [SerializeField] private Button[] primerosBotones;
    [SerializeField] private Slider volumen;
    [SerializeField] private Text estado;
    private bool cargando;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("VolumenGeneral", 1f);
        volumen.SetValueWithoutNotify(AudioListener.volume);
        Mostrar(0);
    }

    public void Mostrar(int indice)
    {
        if (cargando) return;
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

    public void Jugar()
    {
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded("PartidaIntegrada"))
        {
            estado.text = "Falta incluir PartidaIntegrada en la lista de escenas del build.";
            return;
        }
        cargando = true;
        estado.text = "Cargando partida…";
        foreach (Button boton in pantallas[0].GetComponentsInChildren<Button>())
            boton.interactable = false;
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("PartidaIntegrada");
    }
}
