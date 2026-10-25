using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovimientoJugador2D))]
[DefaultExecutionOrder(-50)]
public class UsarPuertaJugador : MonoBehaviour
{
    [SerializeField] private RedPuertas red;
    [SerializeField, Tooltip("El BoxCollider2D de los pies, no el sprite ni la zona de ataque.")]
    private BoxCollider2D colliderPies;
    [SerializeField, Min(0f)] private float cooldownPersonal = 5f;

    private MovimientoJugador2D movimiento;
    private EstadoCombateJugador estado;
    private SuccionJugador succion;
    private AtaqueJugador ataque;
    private ManoCartasJugador cartas;
    public BoxCollider2D ColliderPies => colliderPies;
    public Vector3 PosicionPies => colliderPies != null
        ? colliderPies.transform.TransformPoint(colliderPies.offset)
        : transform.position;
    private bool requiereSoltar;
    private float proximoUso;

    public float CooldownRestante => Mathf.Max(0f, proximoUso - Time.time);

    public bool PuedeUsar => isActiveAndEnabled && Time.timeScale > 0f && CooldownRestante <= 0f &&
        movimiento != null && movimiento.isActiveAndEnabled &&
        (estado == null || !estado.EstaAturdido) && (cartas == null || !cartas.BloqueaSuccion);

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        if (colliderPies == null)
            colliderPies = GetComponent<BoxCollider2D>();
        estado = GetComponent<EstadoCombateJugador>();
        succion = GetComponent<SuccionJugador>();
        ataque = GetComponent<AtaqueJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        if (red == null)
        {
            Debug.LogError("Asigna la red de puertas del jugador.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isFocused || movimiento.AccionMovimiento == null)
            return;

        Vector2 entrada = movimiento.AccionMovimiento.ReadValue<Vector2>();
        bool arriba = entrada.y > 0.5f;
        if (!arriba)
        {
            requiereSoltar = false;
            return;
        }

        // Las diagonales no activan puertas. Tras viajar,
        // hay que soltar la direccion para evitar encadenar otro viaje.
        if (!requiereSoltar && Mathf.Abs(entrada.x) < 0.01f)
            IntentarUsarPuerta();
    }

    public bool IntentarUsarPuerta()
    {
        if (requiereSoltar || !Application.isFocused || movimiento == null || movimiento.AccionMovimiento == null)
            return false;

        Vector2 entrada = movimiento.AccionMovimiento.ReadValue<Vector2>();
        return entrada.y > 0.5f && Mathf.Abs(entrada.x) < 0.01f &&
            red != null && red.IntentarTransportar(this);
    }

    internal void TransportarA(Vector3 destino)
    {
        succion?.CancelarSuccion();
        ataque?.CancelarAtaque();
        cartas?.Cerrar();
        // Salida indica donde apoyar los pies, no donde colocar el pivot del sprite.
        Vector3 desplazamientoPies = PosicionPies - transform.position;
        movimiento.Teletransportar(destino - desplazamientoPies);
        proximoUso = Time.time + Mathf.Max(0f, cooldownPersonal);
        requiereSoltar = true;
    }

    private void OnApplicationFocus(bool foco)
    {
        if (!foco)
            requiereSoltar = true;
    }

    public void BloquearHastaSoltarArriba() => requiereSoltar = true;
}
