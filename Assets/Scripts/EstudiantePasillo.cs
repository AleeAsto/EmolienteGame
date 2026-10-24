using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class EstudiantePasillo : MonoBehaviour
{
    [SerializeField, Min(1f)] private float multiplicadorHuida = 15f;
    [Tooltip("Tiempo que camina y se desvanece tras completar la extraccion.")]
    [SerializeField, Min(0.1f)] private float duracionSalidaAgotado = 2f;
    private AlmaEstudiante alma;
    private float direccion;
    private float velocidad;
    private float salidaIzquierda;
    private float salidaDerecha;
    private bool configurado;

    public float Direccion => direccion;
    public float OpacidadAgotado => alma != null && alma.Agotado
        ? 1f - Mathf.Clamp01(alma.SegundosAgotado / Mathf.Max(0.1f, duracionSalidaAgotado)) : 1f;

    private void Awake()
    {
        alma = GetComponent<AlmaEstudiante>();
    }

    public void ConfigurarRecorrido(float sentido, float rapidez, float izquierda, float derecha)
    {
        direccion = sentido < 0f ? -1f : 1f;
        velocidad = Mathf.Max(0f, rapidez);
        salidaIzquierda = Mathf.Min(izquierda, derecha);
        salidaDerecha = Mathf.Max(izquierda, derecha);
        configurado = true;
    }

    public void HuirDe(Vector3 posicionJugador)
    {
        float diferencia = transform.position.x - posicionJugador.x;
        // Si comparten X, conservar el sentido del recorrido para no quedar inmovil.
        if (Mathf.Abs(diferencia) > 0.001f)
            direccion = diferencia > 0f ? 1f : -1f;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (alma.Agotado && alma.SegundosAgotado >= Mathf.Max(0.1f, duracionSalidaAgotado))
        {
            Destroy(gameObject);
            return;
        }

        if (!configurado || alma.EstaOcupado)
            return;

        Vector3 posicion = transform.position;
        float rapidez = velocidad * (alma.Asustado ? Mathf.Max(1f, multiplicadorHuida) : 1f);
        posicion.x += direccion * rapidez * Time.deltaTime;
        transform.position = posicion;

        if ((direccion < 0f && posicion.x < salidaIzquierda) ||
            (direccion > 0f && posicion.x > salidaDerecha))
            Destroy(gameObject);
    }
}
