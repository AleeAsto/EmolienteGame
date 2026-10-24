using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraPasillo : MonoBehaviour
{
    [SerializeField] private Transform jugador;
    [SerializeField] private Transform extremoIzquierdo;
    [SerializeField] private Transform extremoDerecho;

    private Camera camara;

    private void Awake()
    {
        camara = GetComponent<Camera>();

        if (jugador == null)
        {
            Debug.LogError("Asigna Jugador.", this);
            enabled = false;
        }
        else if (!camara.orthographic)
        {
            Debug.LogError("CamaraPasillo necesita una camara ortografica.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        float mitadAncho = camara.orthographicSize * camara.aspect;
        float minimo = extremoIzquierdo != null
            ? extremoIzquierdo.position.x + mitadAncho
            : float.NegativeInfinity;
        float maximo = extremoDerecho != null
            ? extremoDerecho.position.x - mitadAncho
            : float.PositiveInfinity;

        Vector3 posicion = transform.position;
        posicion.x = minimo <= maximo
            ? Mathf.Clamp(jugador.position.x, minimo, maximo)
            : (minimo + maximo) * 0.5f;
        transform.position = posicion;
    }
}
