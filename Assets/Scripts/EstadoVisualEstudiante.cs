using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class EstadoVisualEstudiante : MonoBehaviour
{
    [SerializeField] private Color colorAgotado = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color colorAsustado = new Color(1f, 0.45f, 0.05f, 1f);

    private AlmaEstudiante alma;
    private SpriteRenderer[] sprites;
    private Color[] coloresOriginales;

    private void Awake()
    {
        alma = GetComponent<AlmaEstudiante>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        coloresOriginales = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            coloresOriginales[i] = sprites[i].color;
    }

    private void OnEnable()
    {
        alma.EstadoCambiado += ActualizarColor;
        ActualizarColor();
    }

    private void ActualizarColor()
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            Color color = alma.Asustado ? colorAsustado :
                (alma.Disponible ? coloresOriginales[i] : colorAgotado);
            color.a = coloresOriginales[i].a;
            sprites[i].color = color;
        }
    }

    private void OnDisable()
    {
        alma.EstadoCambiado -= ActualizarColor;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null)
                sprites[i].color = coloresOriginales[i];
    }
}
