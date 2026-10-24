using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class EstudiantePasillo : MonoBehaviour
{
    [SerializeField, Min(1f)] private float multiplicadorHuida = 15f;
    private AlmaEstudiante alma;
    private float direccion;
    private float velocidad;
    private float salidaIzquierda;
    private float salidaDerecha;
    private bool configurado;

    public float Direccion => direccion;

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
