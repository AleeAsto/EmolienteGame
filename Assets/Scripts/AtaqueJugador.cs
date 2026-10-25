using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(EstadoCombateJugador))]
public class AtaqueJugador : MonoBehaviour
{
    [Header("Entrada y visual")]
    [SerializeField] private InputActionReference golpear;
    [SerializeField] private SpriteRenderer visualGolpe;

    [Header("Tiempos")]
    [SerializeField, Min(0f)] private float cooldown = 0.3f;
    [SerializeField, Min(0.05f)] private float duracionGolpe = 0.08f;
    [SerializeField, Min(0f)] private float preparacionGolpe = 0.01f;

    [Header("Alcance en el piso")]
    [SerializeField, Min(0.1f)] private float alcance = 3f;
    [SerializeField, Min(0f)] private float toleranciaY = 0.8f;
    [SerializeField, Min(0.05f)] private float radioGolpe = 0.3f;

    [Header("Impacto")]
    [SerializeField, Min(0f)] private float stunNormal = 2f;
    [SerializeField, Min(0f)] private float stunSuccionando = 5f;
    [SerializeField, Min(0f)] private float stunSuccionandoRaro = 7f;
    [SerializeField, Min(0f)] private float stunBloqueo = 0.5f;
    [SerializeField, Min(0f)] private float empujon = 0.3f;
    [SerializeField, Min(0f)] private float empujonBloqueo = 0.15f;

    [Header("Arco respecto a los pies")]
    [SerializeField] private Vector2 inicioArco = new Vector2(0.35f, 0.2f);
    [SerializeField] private Vector2 controlArco = new Vector2(4.4f, 0.05f);
    [SerializeField] private Vector2 finalArco = new Vector2(2.2f, -0.15f);

    [Header("Latigo: apuntado y dash")]
    [SerializeField, Min(0.1f)] private float alcanceLatigo = 4f;
    [SerializeField, Min(0.05f)] private float duracionDashLatigo = 0.14f;
    [SerializeField, Min(0.1f)] private float distanciaFrenoLatigo = 1.1f;
    [SerializeField, Min(0.1f)] private float radioImpactoLatigo = 0.9f;
    [SerializeField, Min(0.1f)] private float altoImpactoLatigo = 0.65f;

    private InputAction accionGolpear;
    private MovimientoJugador2D movimiento;
    private SuccionJugador succion;
    private EstadoCombateJugador estado;
    private float tiempoAtaque;
    private float recargaRestante;
    private float direccion = 1f;
    private bool impactoConsumido;
    private bool finalizarSiguienteFrame;
    private ManoCartasJugador cartas;
    private EfectosJugador efectos;
    private MarcaLatigoJugador objetivoLatigo;
    private bool usandoLatigo;
    private Vector3 puntoLatigo;
    public InputAction AccionGolpear => accionGolpear;

    public bool EstaAtacando { get; private set; }
    public bool PuedeImpactar => EstaAtacando && !impactoConsumido && !EnDash &&
        tiempoAtaque >= Mathf.Min(preparacionGolpe, duracionGolpe * 0.25f);
    public bool PuedeChocar => EstaAtacando && !impactoConsumido && !EnDash;
    private bool EnDash => movimiento != null && movimiento.EstaEnDash;
    public bool TieneLatigo => objetivoLatigo != null && objetivoLatigo.PerteneceA(this);
    public bool EsAtaqueLatigo => EstaAtacando && usandoLatigo;
    public bool MostrarApuntadoLatigo => TieneLatigo && !EstaAtacando && !estado.EstaAturdido &&
        (cartas == null || !cartas.MenuAbierto);
    public float ProgresoGolpe => Mathf.Clamp01(tiempoAtaque / Mathf.Max(0.05f, duracionGolpe));
    public Vector2 RadioApuntadoLatigo => new Vector2(radioImpactoLatigo, altoImpactoLatigo);
    public Vector3 PuntoLatigo => EsAtaqueLatigo ? puntoLatigo : CalcularApuntadoLatigo();
    public Vector3 ManoLatigo => OrigenPiso + new Vector3((EstaAtacando ? direccion : movimiento.DireccionHorizontal) * 0.15f, 1.5f, 0f);
    private Vector3 OrigenPiso => movimiento != null ? movimiento.PosicionPies : transform.position;
    public Vector3 PosicionPiso => OrigenPiso;
    public bool ObjetivoEnApuntadoLatigo => TieneLatigo &&
        EnAreaLatigo(objetivoLatigo.GetComponent<EstadoCombateJugador>().ZonaDano, PuntoLatigo) &&
        recargaRestante <= 0f && !objetivoLatigo.GetComponent<EstadoCombateJugador>().EsInvulnerable;
    public float RecargaRestante => recargaRestante;
    public float Direccion => direccion;
    public float RadioGolpe => radioGolpe;
    public float ToleranciaY => toleranciaY;
    public float StunNormal => stunNormal;
    public float StunSuccionando => stunSuccionando;
    public float StunSuccionandoRaro => stunSuccionandoRaro;
    public float StunBloqueo => stunBloqueo;
    public float Empujon => empujon;
    public float EmpujonBloqueo => empujonBloqueo;
    public EstadoCombateJugador Estado => estado;
    public bool EstaSuccionando => succion != null && succion.Objetivo != null;
    public bool EstaSuccionandoRaro => EstaSuccionando && succion.Objetivo.EsRaro;
    public Vector2 PosicionAnteriorGolpe { get; private set; }
    public Vector2 PosicionGolpe { get; private set; }

    private void OnValidate()
    {
        // Una preparacion antigua no debe ocupar casi toda una rafaga nueva.
        preparacionGolpe = Mathf.Clamp(preparacionGolpe, 0f, duracionGolpe * 0.25f);
    }

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        succion = GetComponent<SuccionJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        cartas = GetComponent<ManoCartasJugador>();
        efectos = GetComponent<EfectosJugador>();
        if (Application.isPlaying)
            gameObject.AddComponent<VisualLatigoJugador>().Inicializar(this);
        if (golpear == null || golpear.action == null || visualGolpe == null)
        {
            Debug.LogError("Asigna Golpear y Visual Golpe en el Inspector del jugador.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        estado = GetComponent<EstadoCombateJugador>();
        estado.Interrumpido += CancelarAtaque;
        if (golpear != null && golpear.action != null)
        {
            accionGolpear = golpear.action;
            accionGolpear.Enable();
        }
        OcultarGolpe();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        float multiplicador = efectos != null ? efectos.MultiplicadorVelocidad : 1f;
        float tiempoCombate = Time.deltaTime * multiplicador;
        recargaRestante = Mathf.Max(0f, recargaRestante - tiempoCombate);
        if (estado.EstaAturdido)
        {
            CancelarAtaque();
            return;
        }

        if (EstaAtacando)
        {
            if (finalizarSiguienteFrame)
                CancelarAtaque();
            else
            {
                PosicionAnteriorGolpe = PosicionGolpe;
                if (!EnDash)
                    tiempoAtaque += tiempoCombate;
                if (tiempoAtaque >= duracionGolpe)
                {
                    tiempoAtaque = duracionGolpe;
                    // CombatePvP resuelve impactos en LateUpdate. Conservar
                    // este ultimo frame evita saltarse el golpe a pocos FPS.
                    finalizarSiguienteFrame = true;
                }
                ActualizarVisual();
            }
        }

        if (cartas == null && Application.isFocused && accionGolpear != null && accionGolpear.WasPressedThisFrame())
            IntentarAtacar();
    }

    public bool IntentarAtacar()
    {
        if (Time.timeScale <= 0f || !isActiveAndEnabled || estado == null || estado.EstaAturdido ||
            EstaAtacando || recargaRestante > 0f || (cartas != null && cartas.MenuAbierto))
            return false;

        succion?.CancelarSuccion();
        direccion = movimiento != null ? movimiento.DireccionHorizontal : 1f;
        usandoLatigo = TieneLatigo;
        if (usandoLatigo)
        {
            puntoLatigo = CalcularApuntadoLatigo();
            Vector3 recorrido = puntoLatigo - OrigenPiso;
            float distancia = recorrido.magnitude;
            if (distancia > distanciaFrenoLatigo)
                movimiento.IniciarDash(transform.position + recorrido - recorrido / distancia * distanciaFrenoLatigo, duracionDashLatigo);
        }
        tiempoAtaque = 0f;
        recargaRestante = Mathf.Max(0f, cooldown);
        impactoConsumido = false;
        finalizarSiguienteFrame = false;
        EstaAtacando = true;
        ActualizarVisual();
        PosicionAnteriorGolpe = PosicionGolpe;
        return true;
    }

    public bool EnAlcance(AtaqueJugador otro)
    {
        ZonaDanoJugador objetivo = otro.Estado.ZonaDano;
        if (objetivo == null || !objetivo.Disponible) return false;
        if (usandoLatigo)
            return (objetivo.Centro.x - OrigenPiso.x) * direccion >= -0.05f && EnAreaLatigo(objetivo, puntoLatigo);
        Vector2 centro = (Vector2)OrigenPiso + Vector2.right * direccion * alcance * 0.5f;
        return objetivo.IntersectaRectangulo(centro, new Vector2(alcance, toleranciaY * 2f));
    }

    public void ConsumirImpacto()
    {
        impactoConsumido = true;
    }

    public void CancelarAtaque()
    {
        if (movimiento != null)
            movimiento.CancelarDash();
        EstaAtacando = false;
        finalizarSiguienteFrame = false;
        usandoLatigo = false;
        impactoConsumido = true;
        OcultarGolpe();
    }

    private void ActualizarVisual()
    {
        if (usandoLatigo)
        {
            float extension = Mathf.Sin(ProgresoGolpe * Mathf.PI);
            Vector3 recogido = ManoLatigo + new Vector3(-direccion * 0.4f, 0.5f, 0f);
            PosicionGolpe = Vector3.Lerp(recogido, puntoLatigo + Vector3.up * 0.8f, extension);
            OcultarGolpe();
            return;
        }
        float t = Mathf.Clamp01(tiempoAtaque / Mathf.Max(0.05f, duracionGolpe));
        Vector2 punto = (1f - t) * (1f - t) * inicioArco +
            2f * (1f - t) * t * controlArco + t * t * finalArco;
        punto.x *= direccion;
        Vector3 posicion = OrigenPiso + (Vector3)punto;
        PosicionGolpe = posicion;
        if (visualGolpe == null)
            return;

        visualGolpe.gameObject.SetActive(true);
        visualGolpe.transform.position = posicion;
        if (visualGolpe.sprite != null)
        {
            Vector3 tamano = visualGolpe.sprite.bounds.size;
            Vector3 escala = visualGolpe.transform.parent.lossyScale;
            visualGolpe.transform.localScale = new Vector3(
                radioGolpe * 2f / Mathf.Max(0.001f, tamano.x * Mathf.Abs(escala.x)),
                radioGolpe * 2f / Mathf.Max(0.001f, tamano.y * Mathf.Abs(escala.y)), 1f);
        }
    }

    private void OcultarGolpe()
    {
        if (visualGolpe != null)
            visualGolpe.gameObject.SetActive(false);
    }

    public void PrepararLatigo(MarcaLatigoJugador objetivo) => objetivoLatigo = objetivo;

    public void QuitarLatigo(MarcaLatigoJugador objetivo)
    {
        if (objetivoLatigo == objetivo)
            objetivoLatigo = null;
    }

    private Vector3 CalcularApuntadoLatigo()
    {
        float frente = movimiento != null ? movimiento.DireccionHorizontal : direccion;
        Vector3 punto = OrigenPiso + Vector3.right * frente * alcanceLatigo;
        if (TieneLatigo)
        {
            var zonaObjetivo = objetivoLatigo.GetComponent<EstadoCombateJugador>().ZonaDano;
            Vector3 diferencia = (zonaObjetivo != null && zonaObjetivo.Disponible ? zonaObjetivo.Centro : objetivoLatigo.transform.position) - OrigenPiso;
            if (diferencia.x * frente >= -0.05f && Mathf.Abs(diferencia.y) <= toleranciaY)
                punto = OrigenPiso + Vector3.ClampMagnitude(diferencia, alcanceLatigo);
        }
        return movimiento != null ? movimiento.LimitarDestino(punto - (OrigenPiso - transform.position)) +
            (OrigenPiso - transform.position) : punto;
    }

    private bool EnAreaLatigo(ZonaDanoJugador zona, Vector3 centro)
    {
        return zona != null && zona.IntersectaElipse(centro, RadioApuntadoLatigo);
    }

    private void OnApplicationFocus(bool enfocado)
    {
        if (!enfocado)
            CancelarAtaque();
    }

    private void OnDisable()
    {
        if (estado != null)
            estado.Interrumpido -= CancelarAtaque;
        accionGolpear?.Disable();
        accionGolpear = null;
        CancelarAtaque();
    }
}
