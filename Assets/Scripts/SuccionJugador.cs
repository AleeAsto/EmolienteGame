using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmasJugador))]
[DefaultExecutionOrder(200)]
public class SuccionJugador : MonoBehaviour
{
    [SerializeField] private InputActionReference succionar;
    [SerializeField, Min(0.1f)] private float alcance = 2.2f;
    [Tooltip("Multiplica solo el ancho del alcance. 1 = circulo; 2 = doble de ancho.")]
    [SerializeField, Min(0.1f)] private float escalaHorizontal = 1f;
    [Tooltip("Desplazamiento vertical desde los pies en unidades del mundo. Positivo sube; negativo baja.")]
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
    private bool intentoConsumido;
    public InputAction AccionSuccionar => accionSuccionar;

    public AlmaEstudiante Objetivo => objetivo;
    private Vector3 CentroSuccion
    {
        get
        {
            // Tambien resolver en el editor para que el Gizmo coincida con el juego.
            if (movimiento == null) movimiento = GetComponent<MovimientoJugador2D>();
            BoxCollider2D pies = GetComponent<BoxCollider2D>();
            Vector3 centro = pies != null ? pies.transform.TransformPoint(pies.offset) : transform.position;
            return centro + Vector3.up * offsetVertical;
        }
    }
    public Vector3 PosicionAlineacion =>
        (movimiento != null ? movimiento.PosicionPies : transform.position) +
        Vector3.up * offsetVerticalAlineacion + Vector3.right *
        (movimiento != null ? movimiento.DireccionHorizontal : 1f) *
        Mathf.Clamp(distanciaAlineacion, 0f, alcance * Mathf.Max(0.1f, escalaHorizontal) * 0.9f);
    public float DuracionDrenajeActual => Mathf.Max(0.1f, tiempoDrenaje) *
        (objetivo != null ? objetivo.MultiplicadorTiempoDrenaje : 1f);
    public float Progreso01 => objetivo != null
        ? Mathf.Clamp01(tiempoAcumulado / DuracionDrenajeActual)
        : 0f;

    public Vector3 LimitarAlineacion(Vector3 posicion)
    {
        // El punto que detecta la succion debe permanecer dentro del radio,
        // incluso si los pies o la escala de cada jugador son distintos.
        Vector2 desplazamiento = (Vector2)(posicion - CentroSuccion);
        float escalaX = Mathf.Max(0.1f, escalaHorizontal);
        desplazamiento.x /= escalaX;
        Vector2 limitado = Vector2.ClampMagnitude(desplazamiento, alcance * 0.9f);
        limitado.x *= escalaX;
        return new Vector3(CentroSuccion.x + limitado.x, CentroSuccion.y + limitado.y, posicion.z);
    }

    private float DistanciaAlcanceCuadrada(Vector3 posicion)
    {
        Vector2 diferencia = posicion - CentroSuccion;
        diferencia.x /= Mathf.Max(0.1f, escalaHorizontal);
        return diferencia.sqrMagnitude;
    }

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

        if (accionSuccionar == null || !accionSuccionar.IsPressed() || !Application.isFocused)
            intentoConsumido = false;

        if ((movimiento != null && movimiento.EnPuerta) || !Application.isFocused || almas == null || !almas.isActiveAndEnabled ||
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
            // Buscar mientras se mantiene pulsado, pero consumir solo un objetivo por pulsacion.
            if (intentoConsumido)
                return;

            objetivo = BuscarEstudianteCercano();
            float distanciaReal = objetivo != null
                ? DistanciaAlcanceCuadrada(objetivo.transform.position) : alcance * alcance;
            if (efectos != null && efectos.IntentarDisiparFantasma(CentroSuccion, distanciaReal, escalaHorizontal))
            {
                intentoConsumido = true;
                objetivo = null;
                return;
            }
            if (objetivo == null || !objetivo.IntentarOcupar(almas))
            {
                objetivo = null;
                return;
            }
            intentoConsumido = true;
            tiempoAcumulado = 0f;
        }

        if (!objetivo.Disponible || objetivo.Ocupante != almas ||
            DistanciaAlcanceCuadrada(objetivo.transform.position) > alcance * alcance)
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
        // No depende de sincronizar colliders movidos con Transform.
        foreach (AlmaEstudiante estudiante in FindObjectsByType<AlmaEstudiante>(FindObjectsSortMode.None))
        {
            if (!estudiante.Disponible || estudiante.EstaOcupado)
                continue;

            float distancia = DistanciaAlcanceCuadrada(estudiante.transform.position);
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
        intentoConsumido = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 centro = CentroSuccion;
        float radioX = alcance * Mathf.Max(0.1f, escalaHorizontal);
        Vector3 anterior = centro + Vector3.right * radioX;
        for (int i = 1; i <= 64; i++)
        {
            float angulo = i * Mathf.PI * 2f / 64f;
            Vector3 punto = centro + new Vector3(Mathf.Cos(angulo) * radioX, Mathf.Sin(angulo) * alcance, 0f);
            Gizmos.DrawLine(anterior, punto);
            anterior = punto;
        }
    }
}
