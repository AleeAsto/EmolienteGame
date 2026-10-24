using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EstudianteNPC : MonoBehaviour
{
    private float velocidad;
    private float salida;
    private int direccion;
    private bool inicializado;
    private SpriteRenderer sprite;

    public void Inicializar(int sentido, float rapidez, float extremoSalida)
    {
        sprite = GetComponent<SpriteRenderer>();
        direccion = sentido;
        velocidad = rapidez;
        salida = extremoSalida;
        inicializado = true;
    }

    private void Update()
    {
        if (!inicializado)
            return;

        transform.position += Vector3.right * (direccion * velocidad * Time.deltaTime);

        Bounds limites = sprite.bounds;
        if (direccion > 0 ? limites.min.x > salida : limites.max.x < salida)
            Destroy(gameObject);
    }
}
