using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
public class CuentaRegresivaInicial : MonoBehaviour
{
    private Text texto;

    public IEnumerator Mostrar(string mensaje, float duracion)
    {
        if (texto == null)
            texto = GetComponent<Text>();

        gameObject.SetActive(true);
        texto.text = mensaje;
        Color color = mensaje == "GO!"
            ? new Color(1f, 0.83f, 0.2f, 1f)
            : Color.white;
        float transcurrido = 0f;
        while (transcurrido < duracion)
        {
            // Tiempo real: la presentacion sigue animada con la partida congelada.
            float entrada = Mathf.Clamp01(transcurrido / 0.22f);
            float salida = Mathf.Clamp01((transcurrido - duracion + 0.18f) / 0.18f);
            float escala = Mathf.Lerp(1.65f, 1f, 1f - Mathf.Pow(1f - entrada, 3f));
            escala *= Mathf.Lerp(1f, 0.8f, salida);
            transform.localScale = Vector3.one * escala;
            color.a = Mathf.Min(entrada, 1f - salida);
            texto.color = color;
            yield return null;
            transcurrido += Time.unscaledDeltaTime;
        }

        Ocultar();
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
        transform.localScale = Vector3.one;
    }
}
