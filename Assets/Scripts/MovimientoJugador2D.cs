using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public class MovimientoJugador2D : MonoBehaviour
{
    [SerializeField] private InputActionReference movimiento;
    [SerializeField, Min(0f)] private float velocidad = 5f;
    [SerializeField] private BoxCollider2D zonaMovimiento;

    private BoxCollider2D cuerpo;
    private InputAction accionMovimiento;

    private void Awake()
    {
        cuerpo = GetComponent<BoxCollider2D>();
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
        Vector2 entrada = Application.isFocused && accionMovimiento != null
            ? Vector2.ClampMagnitude(accionMovimiento.ReadValue<Vector2>(), 1f)
            : Vector2.zero;
        Vector3 destino = transform.position + (Vector3)(entrada * velocidad * Time.deltaTime);
        transform.position = LimitarPosicion(destino);
    }

    private void OnDisable()
    {
        accionMovimiento?.Disable();
        accionMovimiento = null;
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

