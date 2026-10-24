using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class EstudiantePasillo : MonoBehaviour
{
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

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!configurado || alma.EstaOcupado)
            return;

        Vector3 posicion = transform.position;
        posicion.x += direccion * velocidad * Time.deltaTime;
        transform.position = posicion;

        if ((direccion < 0f && posicion.x < salidaIzquierda) ||
            (direccion > 0f && posicion.x > salidaDerecha))
            Destroy(gameObject);
    }
}
