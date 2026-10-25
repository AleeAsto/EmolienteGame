using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[DefaultExecutionOrder(110)]
public class Casillero : MonoBehaviour
{
    [SerializeField] private Sprite spriteAbierto;
    [SerializeField] private Sprite spriteCerrado;
    [SerializeField, Min(0.1f)] private float segundosAbierto = 4f;
    [SerializeField, Range(0f, 1f)] private float probabilidadEstudiante = 0.3f;
    [SerializeField] private Vector2 offsetSalida = new Vector2(0f, -1f);
    [Header("Recepcion del golpe")]
    [SerializeField] private Vector2 offsetHitbox = new Vector2(0f, -0.7f);
    [SerializeField, Min(0.1f)] private float altoHitbox = 1.4f;
    [SerializeField, Min(0.05f)] private float duracionTemblor = 0.3f;
    [SerializeField, Min(0f)] private float anguloTemblor = 4f;
    [SerializeField, Min(0.05f)] private float duracionAparicionEstudiante = 0.65f;
    private static readonly List<Casillero> activos = new List<Casillero>();
    private BoxCollider2D obstaculo;
    private AtaqueJugador[] ataques;
    private bool abierto;
    private bool tiradaRealizada;
    private float cierreRestante;
    private float temblorRestante;
    private Quaternion rotacionOriginal;
    private Bounds Hitbox
    {
        get
        {
            Bounds caja = obstaculo.bounds;
            caja.center += (Vector3)offsetHitbox;
            caja.size = new Vector3(caja.size.x, Mathf.Max(caja.size.y, altoHitbox), caja.size.z);
            return caja;
        }
    }

    private void OnEnable()
    {
        obstaculo = GetComponent<BoxCollider2D>();
        if (spriteCerrado == null) spriteCerrado = GetComponent<SpriteRenderer>().sprite;
        rotacionOriginal = transform.localRotation;
        activos.Add(this);
    }

    private void Start() => ataques = FindObjectsByType<AtaqueJugador>(FindObjectsSortMode.None);
    private void OnDisable()
    {
        activos.Remove(this);
        if (temblorRestante > 0f) transform.localRotation = rotacionOriginal;
        temblorRestante = 0f;
    }

    private void LateUpdate()
    {
        if (abierto && Time.timeScale > 0f)
        {
            cierreRestante -= Time.deltaTime;
            if (cierreRestante <= 0f)
            {
                Cerrar();
                return;
            }
        }
        if (temblorRestante > 0f && Time.timeScale > 0f)
        {
            temblorRestante = Mathf.Max(0f, temblorRestante - Time.deltaTime);
            float t = 1f - temblorRestante / Mathf.Max(0.05f, duracionTemblor);
            float giro = Mathf.Sin(t * Mathf.PI * 8f) * anguloTemblor * (1f - t);
            transform.localRotation = rotacionOriginal * Quaternion.Euler(0f, 0f, giro);
        }
        if (abierto || Time.timeScale <= 0f || ataques == null) return;
        foreach (AtaqueJugador ataque in ataques)
        {
            if (ataque == null || ataque.gameObject.scene != gameObject.scene ||
                !ataque.isActiveAndEnabled || !ataque.PuedeImpactar || !ataque.EnAlcance(Hitbox)) continue;
            ataque.ConsumirImpacto();
            Abrir();
            break;
        }
    }

    public void Abrir()
    {
        if (abierto || spriteAbierto == null) return;
        abierto = true;
        cierreRestante = Mathf.Max(0.1f, segundosAbierto);
        if (Application.isPlaying) temblorRestante = Mathf.Max(0.05f, duracionTemblor);
        GetComponent<SpriteRenderer>().sprite = spriteAbierto;
        if (Application.isPlaying) AudioJuego.Instancia?.Casillero(true);
        // Una sola tirada por casillero; golpearlo de nuevo no genera mas estudiantes.
        if (tiradaRealizada) return;
        tiradaRealizada = true;
        if (Random.value >= probabilidadEstudiante) return;
        foreach (GeneradorEstudiantes generador in FindObjectsByType<GeneradorEstudiantes>(FindObjectsSortMode.None))
        {
            if (generador.gameObject.scene != gameObject.scene) continue;
            EstudiantePasillo estudiante = generador.CrearDesdeCasillero(transform.TransformPoint(offsetSalida));
            if (estudiante != null && Application.isPlaying)
                estudiante.GetComponent<EstadoVisualEstudiante>()?.IniciarAparicion(duracionAparicionEstudiante);
            break;
        }
    }

    public void Cerrar()
    {
        if (!abierto) return;
        abierto = false;
        cierreRestante = 0f;
        temblorRestante = 0f;
        transform.localRotation = rotacionOriginal;
        if (spriteCerrado != null) GetComponent<SpriteRenderer>().sprite = spriteCerrado;
        if (Application.isPlaying) AudioJuego.Instancia?.Casillero(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (obstaculo == null) obstaculo = GetComponent<BoxCollider2D>();
        if (obstaculo == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Hitbox.center, Hitbox.size);
    }

    // El jugador se mueve por Transform: un collider estatico solo no frena ese movimiento.
    // Resolver por ejes permite deslizarse alrededor y evita atravesarlo con dash o empujones.
    public static Vector3 LimitarMovimiento(BoxCollider2D cuerpo, Vector3 origen, Vector3 destino)
    {
        Bounds pies = cuerpo.bounds;
        Vector3 offset = pies.center - origen;
        Vector3 centro = pies.center;
        foreach (Casillero casillero in activos)
        {
            if (casillero == null || casillero.gameObject.scene != cuerpo.gameObject.scene ||
                casillero.obstaculo == null || !casillero.obstaculo.enabled) continue;
            Bounds caja = casillero.obstaculo.bounds;
            if (centro.y + pies.extents.y <= caja.min.y || centro.y - pies.extents.y >= caja.max.y) continue;
            float x = destino.x + offset.x;
            if (x > centro.x && centro.x + pies.extents.x <= caja.min.x)
                destino.x = Mathf.Min(destino.x, caja.min.x - pies.extents.x - offset.x - 0.001f);
            else if (x < centro.x && centro.x - pies.extents.x >= caja.max.x)
                destino.x = Mathf.Max(destino.x, caja.max.x + pies.extents.x - offset.x + 0.001f);
        }
        centro.x = destino.x + offset.x;
        foreach (Casillero casillero in activos)
        {
            if (casillero == null || casillero.gameObject.scene != cuerpo.gameObject.scene ||
                casillero.obstaculo == null || !casillero.obstaculo.enabled) continue;
            Bounds caja = casillero.obstaculo.bounds;
            if (centro.x + pies.extents.x <= caja.min.x || centro.x - pies.extents.x >= caja.max.x) continue;
            float y = destino.y + offset.y;
            if (y > centro.y && centro.y + pies.extents.y <= caja.min.y)
                destino.y = Mathf.Min(destino.y, caja.min.y - pies.extents.y - offset.y - 0.001f);
            else if (y < centro.y && centro.y - pies.extents.y >= caja.max.y)
                destino.y = Mathf.Max(destino.y, caja.max.y + pies.extents.y - offset.y + 0.001f);
        }
        return destino;
    }
}
