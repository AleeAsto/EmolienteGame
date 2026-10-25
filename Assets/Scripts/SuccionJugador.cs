using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmasJugador))]
[DefaultExecutionOrder(200)]
public class SuccionJugador : MonoBehaviour
{
    [SerializeField] private InputActionReference succionar;
    [SerializeField, Min(0.1f)] private float alcance = 2.2f;
    [Tooltip("Desplazamiento vertical del centro en unidades del mundo. Positivo sube; negativo baja.")]
    [SerializeField] private float offsetVertical;
    [Tooltip("Separacion del estudiante frente al jugador durante la absorcion.")]
    [SerializeField, Min(0f)] private float distanciaAlineacion = 1.9f;
    [Tooltip("Ajuste de altura del estudiante respecto a los pies del jugador. Positivo sube.")]
    [SerializeField] private float offsetVerticalAlineacion = 0.3f;
    [SerializeField, Min(0.1f)] private float tiempoDrenaje = 3f;
    [Tooltip("Puntos por segundo de absorcion, adicionales al premio por convertir.")]
    [SerializeField, Min(0f)] private float puntosPorSegundo = 2f;

    private AlmasJugador almas;
    private InputAction accionSuccionar;
    private AlmaEstudiante objetivo;
    private float tiempoAcumulado;
    private EstadoCombateJugador estadoCombate;
    private AtaqueJugador ataque;
    private ManoCartasJugador cartas;
    private EfectosJugador efectos;
    private MovimientoJugador2D movimiento;
    public InputAction AccionSuccionar => accionSuccionar;

    public AlmaEstudiante Objetivo => objetivo;
    private Vector3 CentroSuccion => transform.position + Vector3.up * offsetVertical;
    public Vector3 PosicionAlineacion =>
        (movimiento != null ? movimiento.PosicionPies : transform.position) +
        Vector3.up * offsetVerticalAlineacion + Vector3.right *
        (movimiento != null ? movimiento.DireccionHorizontal : 1f) *
        Mathf.Clamp(distanciaAlineacion, 0f, alcance * 0.9f);
    public float DuracionDrenajeActual => Mathf.Max(0.1f, tiempoDrenaje) *
        (objetivo != null ? objetivo.MultiplicadorTiempoDrenaje : 1f);
    public float Progreso01 => objetivo != null
        ? Mathf.Clamp01(tiempoAcumulado / DuracionDrenajeActual)
        : 0f;

    private void Awake()
    {
        almas = GetComponent<AlmasJugador>();
        estadoCombate = GetComponent<EstadoCombateJugador>();
        ataque = GetComponent<AtaqueJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        efectos = GetComponent<EfectosJugador>();
        movimiento = GetComponent<MovimientoJugador2D>();
        if (succionar == null || succionar.action == null)
        {
            Debug.LogError("Asigna la accion Succionar del jugador en el Inspector.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        estadoCombate = GetComponent<EstadoCombateJugador>();
        if (estadoCombate != null)
            estadoCombate.Interrumpido += CancelarSuccion;
        if (succionar == null || succionar.action == null)
            return;

        accionSuccionar = succionar.action;
        accionSuccionar.Enable();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!Application.isFocused || almas == null || !almas.isActiveAndEnabled ||
            accionSuccionar == null || !accionSuccionar.IsPressed() ||
            (estadoCombate != null && estadoCombate.EstaAturdido) ||
            (ataque != null && ataque.EstaAtacando) ||
            (cartas != null && cartas.BloqueaSuccion))
        {
            CancelarSuccion();
            return;
        }

        if (objetivo == null)
        {
            // Una pulsacion inicia un solo drenaje; mantener no encadena estudiantes.
            if (!accionSuccionar.WasPressedThisFrame())
                return;

            objetivo = BuscarEstudianteCercano();
            float distanciaReal = objetivo != null
                ? ((Vector2)(objetivo.transform.position - CentroSuccion)).sqrMagnitude : alcance * alcance;
            if (efectos != null && efectos.IntentarDisiparFantasma(CentroSuccion, distanciaReal))
            {
                objetivo = null;
                return;
            }
            if (objetivo == null || !objetivo.IntentarOcupar(almas))
            {
                objetivo = null;
                return;
            }
            tiempoAcumulado = 0f;
        }

        if (!objetivo.Disponible || objetivo.Ocupante != almas ||
            ((Vector2)(objetivo.transform.position - CentroSuccion)).sqrMagnitude > alcance * alcance)
        {
            CancelarSuccion();
            return;
        }

        float duracion = DuracionDrenajeActual;
        // Limitar el ultimo frame para no premiar tiempo posterior a la conversion.
        float multiplicador = efectos != null ? efectos.MultiplicadorVelocidad : 1f;
        float tiempoAbsorbido = Mathf.Min(Time.deltaTime * multiplicador, duracion - tiempoAcumulado);
        tiempoAcumulado = Mathf.Min(duracion, tiempoAcumulado + tiempoAbsorbido);
        almas.Recibir(tiempoAbsorbido * Mathf.Max(0f, puntosPorSegundo));
        if (tiempoAcumulado < duracion)
            return;

        AlmaEstudiante completado = objetivo;
        objetivo = null;
        tiempoAcumulado = 0f;
        if (!completado.IntentarExtraer(almas))
            completado.Liberar(almas);
    }

    private AlmaEstudiante BuscarEstudianteCercano()
    {
        AlmaEstudiante cercano = null;
        float mejorDistancia = alcance * alcance;
        // Se consulta solo al pulsar; no depende de sincronizar colliders movidos con Transform.
        foreach (AlmaEstudiante estudiante in FindObjectsByType<AlmaEstudiante>(FindObjectsSortMode.None))
        {
            if (!estudiante.Disponible || estudiante.EstaOcupado)
                continue;

            float distancia = ((Vector2)(estudiante.transform.position - CentroSuccion)).sqrMagnitude;
            if (distancia <= mejorDistancia)
            {
                cercano = estudiante;
                mejorDistancia = distancia;
            }
        }
        return cercano;
    }

    public void CancelarSuccion()
    {
        AlmaEstudiante anterior = objetivo;
        objetivo = null;
        tiempoAcumulado = 0f;
        if (anterior != null)
            anterior.Liberar(almas);
    }

    private void OnApplicationFocus(bool enfocado)
    {
        if (!enfocado)
            CancelarSuccion();
    }

    private void OnDisable()
    {
        if (estadoCombate != null)
            estadoCombate.Interrumpido -= CancelarSuccion;
        CancelarSuccion();
        accionSuccionar?.Disable();
        accionSuccionar = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CentroSuccion, alcance);
    }
}
