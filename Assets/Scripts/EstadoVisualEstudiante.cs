using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class EstadoVisualEstudiante : MonoBehaviour
{
    [SerializeField] private Color colorAgotado = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color colorAsustado = new Color(0.65f, 0.85f, 1f, 1f);
    [SerializeField, Min(1f)] private float multiplicadorAnimacionAsustado = 2.5f;

    private AlmaEstudiante alma;
    private SpriteRenderer[] sprites;
    private Color[] coloresOriginales;
    private EstudiantePasillo recorrido;
    private Animator[] animadores;
    private float[] velocidadesOriginales;
    private float inicioAparicion;
    private float duracionAparicion;
    private bool apareciendo;

    public void IniciarAparicion(float duracion)
    {
        inicioAparicion = Time.time;
        duracionAparicion = Mathf.Max(0.05f, duracion);
        apareciendo = true;
        ActualizarColor();
    }

    private void Awake()
    {
        alma = GetComponent<AlmaEstudiante>();
        recorrido = GetComponent<EstudiantePasillo>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        coloresOriginales = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            coloresOriginales[i] = sprites[i].color;
        animadores = GetComponentsInChildren<Animator>(true);
        velocidadesOriginales = new float[animadores.Length];
        for (int i = 0; i < animadores.Length; i++)
            velocidadesOriginales[i] = animadores[i].speed;
    }

    private void OnEnable()
    {
        alma.EstadoCambiado += ActualizarColor;
        ActualizarColor();
    }

    private void ActualizarColor()
    {
        ActualizarAnimacion();
        float aparicion = apareciendo
            ? Mathf.SmoothStep(0f, 1f, (Time.time - inicioAparicion) / duracionAparicion) : 1f;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = alma.Asustado ? colorAsustado :
                (alma.Disponible ? coloresOriginales[i] : colorAgotado);
            color.a = coloresOriginales[i].a * (recorrido != null ? recorrido.OpacidadAgotado : 1f);
            if (apareciendo)
            {
                Color gris = new Color(0.45f, 0.45f, 0.45f, 0f);
                color = Color.Lerp(gris, color, aparicion);
            }
            sprites[i].color = color;
        }
        if (aparicion >= 1f) apareciendo = false;
    }

    private void ActualizarAnimacion()
    {
        for (int i = 0; i < animadores.Length; i++)
            if (animadores[i] != null)
                animadores[i].speed = velocidadesOriginales[i] *
                    (alma.EstaOcupado ? (recorrido != null && recorrido.Alineandose ? 1f : 0f) :
                        alma.Asustado ? Mathf.Max(1f, multiplicadorAnimacionAsustado) : 1f);
    }

    private void LateUpdate()
    {
        ActualizarAnimacion();
        if (alma.Agotado || apareciendo) ActualizarColor();
    }

    private void OnDisable()
    {
        alma.EstadoCambiado -= ActualizarColor;
        apareciendo = false;
        for (int i = 0; i < animadores.Length; i++)
            if (animadores[i] != null)
                animadores[i].speed = velocidadesOriginales[i];
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null)
                sprites[i].color = coloresOriginales[i];
    }
}
