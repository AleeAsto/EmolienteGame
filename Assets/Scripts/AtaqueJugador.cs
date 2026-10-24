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
    [SerializeField, Min(0f)] private float cooldown = 2f;
    [SerializeField, Min(0.05f)] private float duracionGolpe = 0.4f;
    [SerializeField, Min(0f)] private float preparacionGolpe = 0.16f;

    [Header("Alcance en el piso")]
    [SerializeField, Min(0.1f)] private float alcance = 1.8f;
    [SerializeField, Min(0f)] private float toleranciaY = 0.8f;
    [SerializeField, Min(0.05f)] private float radioGolpe = 0.3f;

    [Header("Impacto")]
    [SerializeField, Min(0f)] private float stunNormal = 2f;
    [SerializeField, Min(0f)] private float stunSuccionando = 5f;
    [SerializeField, Min(0f)] private float stunBloqueo = 0.5f;
    [SerializeField, Min(0f)] private float empujon = 0.3f;
    [SerializeField, Min(0f)] private float empujonBloqueo = 0.15f;

    [Header("Arco respecto a los pies")]
    [SerializeField] private Vector2 inicioArco = new Vector2(0.15f, 3.2f);
    [SerializeField] private Vector2 controlArco = new Vector2(2f, 2.3f);
    [SerializeField] private Vector2 finalArco = new Vector2(1.1f, 0.5f);

    private InputAction accionGolpear;
    private MovimientoJugador2D movimiento;
    private SuccionJugador succion;
    private EstadoCombateJugador estado;
    private float tiempoAtaque;
    private float recargaRestante;
    private float direccion = 1f;
    private bool impactoConsumido;
    private ManoCartasJugador cartas;
    public InputAction AccionGolpear => accionGolpear;

    public bool EstaAtacando { get; private set; }
    public bool PuedeImpactar => EstaAtacando && !impactoConsumido &&
        tiempoAtaque >= Mathf.Min(preparacionGolpe, duracionGolpe * 0.8f);
    public bool PuedeChocar => EstaAtacando && !impactoConsumido;
    public float RecargaRestante => recargaRestante;
    public float Direccion => direccion;
    public float RadioGolpe => radioGolpe;
    public float ToleranciaY => toleranciaY;
    public float StunNormal => stunNormal;
    public float StunSuccionando => stunSuccionando;
    public float StunBloqueo => stunBloqueo;
    public float Empujon => empujon;
    public float EmpujonBloqueo => empujonBloqueo;
    public EstadoCombateJugador Estado => estado;
    public bool EstaSuccionando => succion != null && succion.Objetivo != null;
    public Vector2 PosicionAnteriorGolpe { get; private set; }
    public Vector2 PosicionGolpe { get; private set; }

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        succion = GetComponent<SuccionJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        cartas = GetComponent<ManoCartasJugador>();
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

        recargaRestante = Mathf.Max(0f, recargaRestante - Time.deltaTime);
        if (estado.EstaAturdido)
        {
            CancelarAtaque();
            return;
        }

        if (EstaAtacando)
        {
            PosicionAnteriorGolpe = PosicionGolpe;
            tiempoAtaque += Time.deltaTime;
            if (tiempoAtaque >= duracionGolpe)
                CancelarAtaque();
            else
                ActualizarVisual();
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
        tiempoAtaque = 0f;
        recargaRestante = Mathf.Max(0f, cooldown);
        impactoConsumido = false;
        EstaAtacando = true;
        ActualizarVisual();
        PosicionAnteriorGolpe = PosicionGolpe;
        return true;
    }

    public bool EnAlcance(AtaqueJugador otro)
    {
        Vector2 distancia = otro.transform.position - transform.position;
        return distancia.x * direccion >= -0.05f && Mathf.Abs(distancia.x) <= alcance &&
            Mathf.Abs(distancia.y) <= toleranciaY;
    }

    public void ConsumirImpacto()
    {
        impactoConsumido = true;
    }

    public void CancelarAtaque()
    {
        EstaAtacando = false;
        impactoConsumido = true;
        OcultarGolpe();
    }

    private void ActualizarVisual()
    {
        float t = Mathf.Clamp01(tiempoAtaque / Mathf.Max(0.05f, duracionGolpe));
        Vector2 punto = (1f - t) * (1f - t) * inicioArco +
            2f * (1f - t) * t * controlArco + t * t * finalArco;
        punto.x *= direccion;
        Vector3 posicion = transform.position + (Vector3)punto;
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
