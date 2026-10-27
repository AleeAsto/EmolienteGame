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
    [SerializeField] private SpriteRenderer spritePersonaje;
    [SerializeField, Tooltip("Direccion a la que mira el dibujo original sin Flip X.")]
    private bool spriteMiraDerecha = true;

    private BoxCollider2D cuerpo;
    private InputAction accionMovimiento;
    private EstadoCombateJugador estadoCombate;
    private Vector2 empujonPendiente;
    private float rapidezLanzamiento;
    private EntradaJugador entradaJugador;
    private SuccionJugador succion;

    public float DireccionHorizontal { get; private set; } = 1f;
    public InputAction AccionMovimiento => accionMovimiento;
    public BoxCollider2D ZonaMovimiento => zonaMovimiento;
    public Vector3 PosicionPies => cuerpo != null ? cuerpo.transform.TransformPoint(cuerpo.offset) : transform.position;
    private ManoCartasJugador cartas;
    private EfectosJugador efectos;
    private UsarPuertaJugador puertas;
    public bool EnPuerta => puertas != null && puertas.EstaViajando;
    private Vector3 inicioDash;
    private Vector3 destinoDash;
    private float tiempoDash;
    private float duracionDash;
    public bool EstaEnDash { get; private set; }
    public bool EstaCaminando { get; private set; }
    public float VelocidadActual => velocidad * (efectos != null ? efectos.MultiplicadorVelocidad : 1f);

    private void Awake()
    {
        entradaJugador = EntradaJugador.Obtener(this);
        succion = GetComponent<SuccionJugador>();
        cuerpo = GetComponent<BoxCollider2D>();
        estadoCombate = GetComponent<EstadoCombateJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        efectos = GetComponent<EfectosJugador>();
        puertas = GetComponent<UsarPuertaJugador>();
        DireccionHorizontal = direccionInicial < 0f ? -1f : 1f;
        if (spritePersonaje == null)
        {
            Transform visual = transform.Find("Sprite");
            spritePersonaje = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
        }
        ActualizarOrientacion();
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
        EstaCaminando = false;
        if (Time.timeScale <= 0f || EnPuerta)
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

        Vector2 entrada = entradaJugador.TieneFoco &&
            (estadoCombate == null || !estadoCombate.EstaAturdido) &&
            (succion == null || !succion.EstaAbsorbiendo) &&
            (cartas == null || !cartas.MenuAbierto)
            ? Vector2.ClampMagnitude(entradaJugador.LeerMovimiento(accionMovimiento), 1f)
            : Vector2.zero;
        if (Mathf.Abs(entrada.x) > 0.01f)
        {
            DireccionHorizontal = Mathf.Sign(entrada.x);
            ActualizarOrientacion();
        }

        Vector2 empujon = Vector2.ClampMagnitude(empujonPendiente,
            Mathf.Max(0.1f, rapidezEmpujon, rapidezLanzamiento) * Time.deltaTime);
        empujonPendiente -= empujon;
        if (empujonPendiente.sqrMagnitude < 0.0001f)
            rapidezLanzamiento = 0f;
        float multiplicador = efectos != null ? efectos.MultiplicadorVelocidad : 1f;
        Vector3 destino = transform.position + (Vector3)(entrada * velocidad * multiplicador * Time.deltaTime + empujon);
        Vector3 anterior = transform.position;
        transform.position = LimitarPosicion(destino);
        EstaCaminando = entrada.sqrMagnitude > 0.01f &&
            Vector2.Dot((Vector2)(transform.position - anterior), entrada) > 0.00001f;
    }

    public void Empujar(Vector2 desplazamiento)
    {
        empujonPendiente += desplazamiento;
    }

    public void MirarHacia(float direccion)
    {
        if (Mathf.Abs(direccion) < 0.01f || (estadoCombate != null && estadoCombate.EstaAturdido)) return;
        DireccionHorizontal = Mathf.Sign(direccion);
        ActualizarOrientacion();
    }

    private void ActualizarOrientacion()
    {
        if (spritePersonaje != null)
            spritePersonaje.flipX = (DireccionHorizontal > 0f) != spriteMiraDerecha;
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
        EstaCaminando = false;
        CancelarDash();
        empujonPendiente = Vector2.zero;
        rapidezLanzamiento = 0f;
        // El teletransporte no recorre el pasillo ni choca con muebles intermedios.
        transform.position = LimitarPosicion(destino, false);
    }

    public Vector3 LimitarDestino(Vector3 destino) => LimitarPosicion(destino);

    public bool IniciarDash(Vector3 destino, float duracion)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f ||
            (succion != null && succion.EstaAbsorbiendo) ||
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
        EstaCaminando = false;
        accionMovimiento?.Disable();
        accionMovimiento = null;
        empujonPendiente = Vector2.zero;
        rapidezLanzamiento = 0f;
        CancelarDash();
    }

    private Vector3 LimitarPosicion(Vector3 destino, bool comprobarObstaculos = true)
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
        return comprobarObstaculos ? Casillero.LimitarMovimiento(cuerpo, transform.position, destino) : destino;
    }
}

