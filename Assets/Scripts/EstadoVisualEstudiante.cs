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

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = alma.Asustado ? colorAsustado :
                (alma.Disponible ? coloresOriginales[i] : colorAgotado);
            color.a = coloresOriginales[i].a * (recorrido != null ? recorrido.OpacidadAgotado : 1f);
            sprites[i].color = color;
        }
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
        if (alma.Agotado) ActualizarColor();
    }

    private void OnDisable()
    {
        alma.EstadoCambiado -= ActualizarColor;
        for (int i = 0; i < animadores.Length; i++)
            if (animadores[i] != null)
                animadores[i].speed = velocidadesOriginales[i];
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null)
                sprites[i].color = coloresOriginales[i];
    }
}
