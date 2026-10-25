using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class ProfesoraPasillo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BoxCollider2D zonaPasillo;
    [SerializeField] private EstadoCombateJugador[] jugadores;
    [SerializeField] private Camera[] camarasJugadores;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private BoxCollider2D zonaContacto;

    [Header("Recorrido horizontal")]
    [SerializeField, Min(0f)] private float velocidad = 2.5f;
    [SerializeField] private float direccionInicial = 1f;
    [SerializeField, Min(0f)] private float margenFueraDeVista = 2f;
    [SerializeField, Min(0f)] private float intervaloEntrePasadas = 4f;

    [Header("Contacto en el piso")]
    [SerializeField, Min(0f)] private float duracionStun = 3.5f;
    [SerializeField, Min(0f)] private float empujon = 0.5f;

    private float direccion;
    private float esperaRestante;
    private bool enRecorrido;

    private void Start()
    {
        if (zonaContacto == null || zonaPasillo == null || jugadores == null || jugadores.Length == 0 || visual == null ||
            camarasJugadores == null || camarasJugadores.Length == 0)
        {
            Debug.LogError("Asigna pasillo, jugadores, camaras y visual a la profesora.", this);
            enabled = false;
            return;
        }

        foreach (Camera camara in camarasJugadores)
        {
            if (camara == null || !camara.orthographic)
            {
                Debug.LogError("La profesora necesita las camaras ortograficas de ambos jugadores.", this);
                enabled = false;
                return;
            }
        }

        direccion = direccionInicial < 0f ? -1f : 1f;
        visual.enabled = false;
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!enRecorrido)
        {
            esperaRestante = Mathf.Max(0f, esperaRestante - Time.deltaTime);
            if (esperaRestante <= 0f)
            {
                CalcularExtremosFueraDeVista(out float izquierda, out float derecha);
                Vector3 inicio = transform.position;
                inicio.x = direccion > 0f ? izquierda : derecha;
                transform.position = inicio;
                visual.enabled = true;
                enRecorrido = true;
            }
            // La reaparicion no debe barrer el pasillo como si fuera un golpe.
            return;
        }

        Vector3 anterior = transform.position;
        Vector3 siguiente = anterior;
        siguiente.x += direccion * Mathf.Max(0f, velocidad) * Time.deltaTime;
        transform.position = siguiente;

        // Barrer el trayecto evita saltarse jugadores entre dos frames.
        Physics2D.SyncTransforms();
        Bounds contacto = zonaContacto.bounds;
        float avance = siguiente.x - anterior.x;
        Vector2 centroBarrido = (Vector2)contacto.center - Vector2.right * avance * 0.5f;
        Vector2 tamanoBarrido = new Vector2(contacto.size.x + Mathf.Abs(avance), contacto.size.y);
        foreach (EstadoCombateJugador jugador in jugadores)
        {
            if (jugador == null || !jugador.isActiveAndEnabled)
                continue;

            if (zonaContacto.isActiveAndEnabled && jugador.ZonaDano != null &&
                jugador.ZonaDano.IntersectaRectangulo(centroBarrido, tamanoBarrido))
                jugador.RecibirGolpe(duracionStun, Vector2.right * direccion * Mathf.Max(0f, empujon));
        }

        CalcularExtremosFueraDeVista(out float salidaIzquierda, out float salidaDerecha);
        if ((direccion > 0f && siguiente.x >= salidaDerecha) ||
            (direccion < 0f && siguiente.x <= salidaIzquierda))
        {
            visual.enabled = false;
            enRecorrido = false;
            esperaRestante = Mathf.Max(0f, intervaloEntrePasadas);
            direccion *= -1f;
        }
    }

    private void CalcularExtremosFueraDeVista(out float izquierda, out float derecha)
    {
        Bounds zona = zonaPasillo.bounds;
        izquierda = zona.min.x;
        derecha = zona.max.x;
        foreach (Camera camara in camarasJugadores)
        {
            if (camara == null || !camara.isActiveAndEnabled)
                continue;

            float distancia = Mathf.Abs(transform.position.z - camara.transform.position.z);
            float x1 = camara.ViewportToWorldPoint(new Vector3(0f, 0.5f, distancia)).x;
            float x2 = camara.ViewportToWorldPoint(new Vector3(1f, 0.5f, distancia)).x;
            izquierda = Mathf.Min(izquierda, Mathf.Min(x1, x2));
            derecha = Mathf.Max(derecha, Mathf.Max(x1, x2));
        }

        float mitadVisual = visual.sprite != null
            ? visual.sprite.bounds.extents.x * Mathf.Abs(visual.transform.lossyScale.x) : 0f;
        float margen = Mathf.Max(0f, margenFueraDeVista) + Mathf.Max(mitadVisual,
            zonaContacto != null ? zonaContacto.bounds.extents.x : 0f);
        izquierda -= margen;
        derecha += margen;
    }

    private void OnDrawGizmosSelected()
    {
        if (zonaContacto == null)
            return;

        Bounds zona = zonaContacto.bounds;
        Gizmos.color = new Color(0.7f, 0.25f, 1f, 0.3f);
        Gizmos.DrawWireCube(zona.center, zona.size);
    }
}
