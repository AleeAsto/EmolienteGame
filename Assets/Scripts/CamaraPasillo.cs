using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
[DefaultExecutionOrder(50)]
public class CamaraPasillo : MonoBehaviour
{
    [SerializeField] private Transform jugador;
    [SerializeField] private Transform extremoIzquierdo;
    [SerializeField] private Transform extremoDerecho;
    [Tooltip("Opcional. Sin asignar usa la zona de movimiento del jugador; los extremos quedan como respaldo.")]
    [SerializeField] private BoxCollider2D zonaPasillo;
    [SerializeField] private bool seguirEnEditor = true;

    private Camera camara;

    private void Awake()
    {
        camara = GetComponent<Camera>();

        if (jugador == null && Application.isPlaying)
        {
            Debug.LogError("Asigna Jugador.", this);
            enabled = false;
        }
        else if (!camara.orthographic && Application.isPlaying)
        {
            Debug.LogError("CamaraPasillo necesita una camara ortografica.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying || seguirEnEditor) ActualizarSeguimiento();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying && !seguirEnEditor)
            return;
        ActualizarSeguimiento();
    }

    [ContextMenu("Actualizar seguimiento")]
    public void ActualizarSeguimiento()
    {
        if (camara == null) camara = GetComponent<Camera>();
        if (jugador == null || camara == null || !camara.orthographic)
            return;
        BoxCollider2D zona = zonaPasillo;
        if (zona == null && jugador.TryGetComponent<MovimientoJugador2D>(out var movimiento))
            zona = movimiento.ZonaMovimiento;
        float mitadAncho = camara.orthographicSize * camara.aspect;
        float bordeIzquierdo = zona != null ? zona.bounds.min.x :
            extremoIzquierdo != null ? extremoIzquierdo.position.x : float.NegativeInfinity;
        float bordeDerecho = zona != null ? zona.bounds.max.x :
            extremoDerecho != null ? extremoDerecho.position.x : float.PositiveInfinity;
        float minimo = bordeIzquierdo + mitadAncho;
        float maximo = bordeDerecho - mitadAncho;

        Vector3 posicion = transform.position;
        posicion.x = minimo <= maximo
            ? Mathf.Clamp(jugador.position.x, minimo, maximo)
            : (minimo + maximo) * 0.5f;
        transform.position = posicion;
    }
}
