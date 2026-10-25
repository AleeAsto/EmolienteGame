using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovimientoJugador2D))]
[DefaultExecutionOrder(-50)]
public class UsarPuertaJugador : MonoBehaviour
{
    [SerializeField] private RedPuertas red;
    [SerializeField, Min(0f)] private float cooldownPersonal = 5f;

    private MovimientoJugador2D movimiento;
    private EstadoCombateJugador estado;
    private SuccionJugador succion;
    private AtaqueJugador ataque;
    private ManoCartasJugador cartas;
    private bool requiereSoltar;
    private float proximoUso;

    public float CooldownRestante => Mathf.Max(0f, proximoUso - Time.time);

    public bool PuedeUsar => isActiveAndEnabled && Time.timeScale > 0f && CooldownRestante <= 0f &&
        movimiento != null && movimiento.isActiveAndEnabled &&
        (estado == null || !estado.EstaAturdido) && (cartas == null || !cartas.BloqueaSuccion);

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
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

        bool arriba = movimiento.AccionMovimiento.ReadValue<Vector2>().y > 0.5f;
        if (!arriba)
        {
            requiereSoltar = false;
            return;
        }

        // Se puede caminar hacia la puerta manteniendo arriba. Tras viajar,
        // hay que soltar la direccion para evitar encadenar otro viaje.
        if (!requiereSoltar)
            IntentarUsarPuerta();
    }

    public bool IntentarUsarPuerta()
    {
        return !requiereSoltar && red != null && red.IntentarTransportar(this);
    }

    internal void TransportarA(Vector3 destino)
    {
        succion?.CancelarSuccion();
        ataque?.CancelarAtaque();
        cartas?.Cerrar();
        movimiento.Teletransportar(destino);
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
