using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OpcionesAudioUI : MonoBehaviour
{
    [SerializeField] private Slider general;
    [SerializeField] private Slider musica;
    [SerializeField] private Slider efectos;
    [SerializeField] private Text valorGeneral;
    [SerializeField] private Text valorMusica;
    [SerializeField] private Text valorEfectos;
    private bool conectado;

    private void OnEnable()
    {
        if (!conectado)
        {
            general.onValueChanged.AddListener(CambiarGeneral);
            musica.onValueChanged.AddListener(CambiarMusica);
            efectos.onValueChanged.AddListener(CambiarEfectos);
            conectado = true;
        }
        float volumen = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenGeneral", 1f));
        general.SetValueWithoutNotify(volumen);
        musica.SetValueWithoutNotify(AudioJuego.Instancia != null ? AudioJuego.Instancia.VolumenMusica : 1f);
        efectos.SetValueWithoutNotify(AudioJuego.Instancia != null ? AudioJuego.Instancia.VolumenEfectos : 1f);
        ActualizarValores();
    }

    private void CambiarGeneral(float valor)
    {
        AudioListener.volume = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenGeneral", AudioListener.volume);
        ActualizarValores();
    }

    private void CambiarMusica(float valor)
    {
        AudioJuego.Instancia?.CambiarVolumenMusica(valor);
        ActualizarValores();
    }

    private void CambiarEfectos(float valor)
    {
        AudioJuego.Instancia?.CambiarVolumenEfectos(valor);
        ActualizarValores();
    }

    private void ActualizarValores()
    {
        valorGeneral.text = Mathf.RoundToInt(general.value * 100f) + "%";
        valorMusica.text = Mathf.RoundToInt(musica.value * 100f) + "%";
        valorEfectos.text = Mathf.RoundToInt(efectos.value * 100f) + "%";
    }

    private void OnDisable() => PlayerPrefs.Save();

    private void OnDestroy()
    {
        if (!conectado) return;
        general.onValueChanged.RemoveListener(CambiarGeneral);
        musica.onValueChanged.RemoveListener(CambiarMusica);
        efectos.onValueChanged.RemoveListener(CambiarEfectos);
    }
}
