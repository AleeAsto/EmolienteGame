using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransicionEscena : MonoBehaviour
{
    private static TransicionEscena actual;
    public static bool EnCurso => actual != null;
    private Image cortina;
    private const float DuracionSalida = 0.35f;
    private const float DuracionLlegada = 0.65f;

    public static void Cargar(string escena, bool reiniciarMusica = false)
    {
        if (EnCurso) return;
        GameObject objeto = new GameObject("TransicionEntreEscenas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(objeto);
        actual = objeto.AddComponent<TransicionEscena>();
        Canvas canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        GameObject fondo = new GameObject("FundidoNegro", typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(objeto.transform, false);
        RectTransform rect = (RectTransform)fondo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        actual.cortina = fondo.GetComponent<Image>();
        actual.cortina.color = Color.clear;
        actual.cortina.raycastTarget = true;
        actual.StartCoroutine(actual.Cambiar(escena, reiniciarMusica));
    }

    private IEnumerator Cambiar(string escena, bool reiniciarMusica)
    {
        Coroutine salidaMusical = reiniciarMusica && AudioJuego.Instancia != null
            ? AudioJuego.Instancia.DesvanecerMusica() : null;
        AsyncOperation carga = SceneManager.LoadSceneAsync(escena);
        carga.allowSceneActivation = false;
        yield return Fundir(0f, 1f, DuracionSalida);
        if (salidaMusical != null) yield return salidaMusical;
        while (carga.progress < 0.9f) yield return null;
        carga.allowSceneActivation = true;
        yield return carga;
        // La nueva escena ya construyo el VS antes de revelar el primer frame.
        yield return null;
        Canvas.ForceUpdateCanvases();
        // Descartar el frame largo de carga antes de empezar a revelar el VS.
        cortina.color = Color.black;
        yield return null;
        yield return Fundir(1f, 0f, DuracionLlegada);
        actual = null;
        Destroy(gameObject);
    }

    private IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracion);
            cortina.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t));
            yield return null;
            // La carga puede producir un delta enorme; no saltarse el fundido entero.
            tiempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }
        cortina.color = new Color(0f, 0f, 0f, hasta);
    }

    private void OnDestroy()
    {
        if (actual == this) actual = null;
    }
}
