using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
[DefaultExecutionOrder(-100)]
public class MovimientoJugador2D : MonoBehaviour
{
    [SerializeField] private InputActionReference movimiento;
    [SerializeField, Min(0f)] private float velocidad = 5f;
    [SerializeField] private BoxCollider2D zonaMovimiento;
    [SerializeField] private float direccionInicial = 1f;
    [SerializeField, Min(0.1f)] private float rapidezEmpujon = 3f;

    private BoxCollider2D cuerpo;
    private InputAction accionMovimiento;
    private EstadoCombateJugador estadoCombate;
    private Vector2 empujonPendiente;
    private float rapidezLanzamiento;

    public float DireccionHorizontal { get; private set; } = 1f;
    public InputAction AccionMovimiento => accionMovimiento;
    public BoxCollider2D ZonaMovimiento => zonaMovimiento;
    private ManoCartasJugador cartas;
    private EfectosJugador efectos;
    private Vector3 inicioDash;
    private Vector3 destinoDash;
    private float tiempoDash;
    private float duracionDash;
    public bool EstaEnDash { get; private set; }

    private void Awake()
    {
        cuerpo = GetComponent<BoxCollider2D>();
        estadoCombate = GetComponent<EstadoCombateJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        efectos = GetComponent<EfectosJugador>();
        DireccionHorizontal = direccionInicial < 0f ? -1f : 1f;
        if (zonaMovimiento == null || movimiento == null || movimiento.action == null)
        {
            Debug.LogError("Asigna Zona Movimiento y Movimiento en el Inspector del jugador.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (enabled)
            transform.position = LimitarPosicion(transform.position);
    }

    private void OnEnable()
    {
        if (movimiento == null || movimiento.action == null)
            return;

        accionMovimiento = movimiento.action;
        accionMovimiento.Enable();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (EstaEnDash)
        {
            if (estadoCombate != null && estadoCombate.EstaAturdido)
                CancelarDash();
            else
            {
                tiempoDash = Mathf.Min(duracionDash, tiempoDash + Time.deltaTime);
                float t = tiempoDash / duracionDash;
                transform.position = LimitarPosicion(Vector3.Lerp(inicioDash, destinoDash, 1f - (1f - t) * (1f - t)));
                if (tiempoDash >= duracionDash)
                    CancelarDash();
                return;
            }
        }

        Vector2 entrada = Application.isFocused && accionMovimiento != null &&
            (estadoCombate == null || !estadoCombate.EstaAturdido) &&
            (cartas == null || !cartas.MenuAbierto)
            ? Vector2.ClampMagnitude(accionMovimiento.ReadValue<Vector2>(), 1f)
            : Vector2.zero;
        if (Mathf.Abs(entrada.x) > 0.01f)
            DireccionHorizontal = Mathf.Sign(entrada.x);

        Vector2 empujon = Vector2.ClampMagnitude(empujonPendiente,
            Mathf.Max(0.1f, rapidezEmpujon, rapidezLanzamiento) * Time.deltaTime);
        empujonPendiente -= empujon;
        if (empujonPendiente.sqrMagnitude < 0.0001f)
            rapidezLanzamiento = 0f;
        float multiplicador = efectos != null ? efectos.MultiplicadorVelocidad : 1f;
        Vector3 destino = transform.position + (Vector3)(entrada * velocidad * multiplicador * Time.deltaTime + empujon);
        transform.position = LimitarPosicion(destino);
    }

    public void Empujar(Vector2 desplazamiento)
    {
        empujonPendiente += desplazamiento;
    }

    public void AcelerarEmpujon(float duracion)
    {
        rapidezLanzamiento = Mathf.Max(rapidezLanzamiento,
            empujonPendiente.magnitude / Mathf.Max(0.05f, duracion));
    }

    public void EmpujarHastaExtremo(float direccion)
    {
        if (zonaMovimiento == null || cuerpo == null || Mathf.Approximately(direccion, 0f))
            return;
        CancelarDash();
        Vector3 destino = transform.position;
        destino.x = direccion > 0f ? zonaMovimiento.bounds.max.x : zonaMovimiento.bounds.min.x;
        empujonPendiente = (Vector2)(LimitarPosicion(destino) - transform.position);
        rapidezLanzamiento = empujonPendiente.magnitude / 0.65f;
    }

    public void Teletransportar(Vector3 destino)
    {
        CancelarDash();
        empujonPendiente = Vector2.zero;
        rapidezLanzamiento = 0f;
        transform.position = LimitarPosicion(destino);
    }

    public Vector3 LimitarDestino(Vector3 destino) => LimitarPosicion(destino);

    public bool IniciarDash(Vector3 destino, float duracion)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f ||
            (estadoCombate != null && estadoCombate.EstaAturdido))
            return false;

        inicioDash = transform.position;
        destinoDash = LimitarPosicion(destino);
        if ((destinoDash - inicioDash).sqrMagnitude < 0.0001f)
            return false;
        duracionDash = Mathf.Max(0.05f, duracion);
        tiempoDash = 0f;
        empujonPendiente = Vector2.zero;
        rapidezLanzamiento = 0f;
        EstaEnDash = true;
        return true;
    }

    public void CancelarDash() => EstaEnDash = false;

    private void OnDisable()
    {
        accionMovimiento?.Disable();
        accionMovimiento = null;
        empujonPendiente = Vector2.zero;
        rapidezLanzamiento = 0f;
        CancelarDash();
    }

    private Vector3 LimitarPosicion(Vector3 destino)
    {
        Bounds zona = zonaMovimiento.bounds;
        Vector3 offset = transform.TransformPoint(cuerpo.offset) - transform.position;
        Vector3 ejeX = transform.TransformVector(Vector3.right * cuerpo.size.x * 0.5f);
        Vector3 ejeY = transform.TransformVector(Vector3.up * cuerpo.size.y * 0.5f);
        Vector2 margen = new Vector2(Mathf.Abs(ejeX.x) + Mathf.Abs(ejeY.x),
            Mathf.Abs(ejeX.y) + Mathf.Abs(ejeY.y));

        float minX = zona.min.x + margen.x - offset.x;
        float maxX = zona.max.x - margen.x - offset.x;
        float minY = zona.min.y + margen.y - offset.y;
        float maxY = zona.max.y - margen.y - offset.y;
        destino.x = minX <= maxX ? Mathf.Clamp(destino.x, minX, maxX) : zona.center.x - offset.x;
        destino.y = minY <= maxY ? Mathf.Clamp(destino.y, minY, maxY) : zona.center.y - offset.y;
        return destino;
    }
}

