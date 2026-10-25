using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MarcaLatigoJugador : MonoBehaviour
{
    private CicloPartida partida;
    private AtaqueJugador autor;
    private float finMarca;
    private GameObject indicador;
    private Text texto;
    private int ultimoSegundo = -1;

    public bool EstaMarcado => isActiveAndEnabled && partida != null && partida.EnCurso &&
        !partida.Terminada && autor != null && Time.time < finMarca;
    public float SegundosRestantes => EstaMarcado ? Mathf.Max(0f, finMarca - Time.time) : 0f;
    public float AlmasExtra { get; private set; }
    public float Empujon { get; private set; }
    public bool EmpujaHastaExtremo { get; private set; }

    public void Inicializar(CicloPartida ciclo)
    {
        partida = ciclo;
        Limpiar();
    }

    public void Aplicar(AtaqueJugador atacante, float duracion, float almasExtra, float empujon, bool hastaExtremo = false)
    {
        if (!isActiveAndEnabled || partida == null || !partida.EnCurso || partida.Terminada ||
            Time.timeScale <= 0f || atacante == null || duracion <= 0f)
            return;

        if (autor != null && autor != atacante)
            autor.QuitarLatigo(this);
        autor = atacante;
        autor.PrepararLatigo(this);
        finMarca = Mathf.Max(finMarca, Time.time + duracion);
        AlmasExtra = Mathf.Max(0f, almasExtra);
        Empujon = Mathf.Max(0f, empujon);
        EmpujaHastaExtremo = hastaExtremo;
        if (Application.isPlaying && indicador == null)
            CrearIndicador();
        ActualizarIndicador();
    }

    public bool PerteneceA(AtaqueJugador atacante) => EstaMarcado && autor == atacante;

    public bool Consumir(AtaqueJugador atacante)
    {
        if (!PerteneceA(atacante))
            return false;

        Limpiar();
        return true;
    }

    private void LateUpdate() => ActualizarIndicador();

    private void ActualizarIndicador()
    {
        if (indicador == null)
            return;

        indicador.SetActive(EstaMarcado);
        int segundos = Mathf.CeilToInt(SegundosRestantes);
        if (EstaMarcado && segundos != ultimoSegundo)
        {
            ultimoSegundo = segundos;
            texto.text = segundos + " s";
        }
    }

    private void CrearIndicador()
    {
        float altura = 1f;
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
            if (sprite.enabled && sprite.gameObject.activeInHierarchy)
                altura = Mathf.Max(altura, sprite.bounds.max.y - transform.position.y);

        indicador = new GameObject("MarcaLatigo", typeof(RectTransform), typeof(Canvas));
        indicador.layer = gameObject.layer;
        indicador.transform.SetParent(transform, false);
        // Compensar la escala del personaje para mantener el rotulo legible.
        Vector3 escala = transform.lossyScale;
        indicador.transform.localPosition = new Vector3(0f, (altura + 0.8f) / Mathf.Max(0.001f, Mathf.Abs(escala.y)), 0f);
        indicador.transform.localScale = new Vector3(0.015f / Mathf.Max(0.001f, Mathf.Abs(escala.x)),
            0.015f / Mathf.Max(0.001f, Mathf.Abs(escala.y)), 1f);
        ((RectTransform)indicador.transform).sizeDelta = new Vector2(180f, 84f);
        Canvas canvas = indicador.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 40;

        // Mira dibujada con UI para que no dependa de simbolos disponibles en la fuente.
        const float radio = 19f;
        const int segmentos = 32;
        Vector2 centro = new Vector2(0f, 15f);
        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentos;
            CrearTrazoMira(centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio,
                new Vector2(4.2f, 3f), angulo * Mathf.Rad2Deg + 90f);
        }
        CrearTrazoMira(centro, new Vector2(5f, 5f));
        CrearTrazoMira(centro + Vector2.left * 21f, new Vector2(14f, 3f));
        CrearTrazoMira(centro + Vector2.right * 21f, new Vector2(14f, 3f));
        CrearTrazoMira(centro + Vector2.up * 21f, new Vector2(3f, 14f));
        CrearTrazoMira(centro + Vector2.down * 21f, new Vector2(3f, 14f));

        GameObject rotulo = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline));
        rotulo.layer = gameObject.layer;
        rotulo.transform.SetParent(indicador.transform, false);
        texto = rotulo.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 32;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(1f, 0.35f, 0.35f, 1f);
        texto.raycastTarget = false;
        texto.rectTransform.anchorMin = texto.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        texto.rectTransform.sizeDelta = new Vector2(100f, 36f);
        texto.rectTransform.anchoredPosition = new Vector2(0f, -29f);
        Outline borde = rotulo.GetComponent<Outline>();
        borde.effectColor = Color.black;
        borde.effectDistance = new Vector2(2f, -2f);
    }

    private void CrearTrazoMira(Vector2 posicion, Vector2 tamano, float giro = 0f)
    {
        GameObject trazo = new GameObject("Mira", typeof(RectTransform), typeof(Image), typeof(Outline));
        trazo.layer = gameObject.layer;
        trazo.transform.SetParent(indicador.transform, false);
        Image imagen = trazo.GetComponent<Image>();
        imagen.color = new Color(1f, 0.35f, 0.35f, 1f);
        imagen.raycastTarget = false;
        imagen.rectTransform.sizeDelta = tamano;
        imagen.rectTransform.anchoredPosition = posicion;
        imagen.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro);
        Outline borde = trazo.GetComponent<Outline>();
        borde.effectColor = Color.black;
        borde.effectDistance = Vector2.one;
    }

    public void Limpiar()
    {
        if (autor != null)
            autor.QuitarLatigo(this);
        autor = null;
        finMarca = 0f;
        EmpujaHastaExtremo = false;
        ultimoSegundo = -1;
        if (indicador != null)
            indicador.SetActive(false);
    }

    private void OnDisable() => Limpiar();
}
