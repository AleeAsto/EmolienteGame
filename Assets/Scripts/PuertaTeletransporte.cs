using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class PuertaTeletransporte : MonoBehaviour
{
    [SerializeField] private int numero = 1;
    [SerializeField] private Transform salida;
    [SerializeField] private SpriteRenderer panel;
    [SerializeField] private Sprite spriteAbierta;
    [SerializeField] private Sprite spriteCerrada;
    [SerializeField] private bool ajustarAccesosAlSprite = true;
    [SerializeField] private Vector2 offsetAcceso = new Vector2(0f, -0.25f);
    [SerializeField] private Text textoEstado;
    [SerializeField] private Color colorDisponible = new Color(0.25f, 0.85f, 0.65f, 1f);
    [SerializeField] private Color colorCooldown = new Color(0.4f, 0.42f, 0.5f, 1f);

    private float disponibleDesde;
    private int ultimoSegundo = -1;
    private BoxCollider2D zonaEntrada;
    private BrilloPuerta brillo;
    private Vector3 posicionPanel;
    private Vector3 escalaPanel;
    private Bounds limitesPanel;
    private bool visualInicializado;

    private void Reset()
    {
        var zona = GetComponent<BoxCollider2D>();
        zona.isTrigger = true;
        zona.size = new Vector2(2.4f, 1.6f);
    }

    private void Awake()
    {
        zonaEntrada = GetComponent<BoxCollider2D>();
        if (panel == null || panel.sprite == null) return;
        posicionPanel = panel.transform.localPosition;
        escalaPanel = panel.transform.localScale;
        limitesPanel = panel.sprite.bounds;
        visualInicializado = true;
        // El acceso debe quedar listo aunque falle un efecto visual en la build.
        if (ajustarAccesosAlSprite) AjustarAccesos();
        brillo = gameObject.AddComponent<BrilloPuerta>();
        brillo.Inicializar(panel);
    }

    public float CooldownRestante => Mathf.Max(0f, disponibleDesde - Time.time);
    public bool Disponible => isActiveAndEnabled && CooldownRestante <= 0f;
    public Vector3 PosicionSalida => salida != null ? salida.position : transform.position;

    [ContextMenu("Alinear entrada y salida con la puerta")]
    public void AjustarAccesos()
    {
        if (panel == null || panel.sprite == null) return;
        if (zonaEntrada == null) zonaEntrada = GetComponent<BoxCollider2D>();
        Bounds puerta = panel.bounds;
        Vector3 acceso = new Vector3(puerta.center.x + offsetAcceso.x,
            puerta.min.y + offsetAcceso.y, transform.position.z);
        if (salida != null) salida.position = acceso;
        Vector3 local = transform.InverseTransformPoint(acceso);
        zonaEntrada.offset = new Vector2(local.x, local.y);
        Vector3 escala = transform.lossyScale;
        zonaEntrada.size = new Vector2(Mathf.Max(1.2f, puerta.size.x * 0.7f) /
            Mathf.Max(0.001f, Mathf.Abs(escala.x)), 1.2f / Mathf.Max(0.001f, Mathf.Abs(escala.y)));
        zonaEntrada.isTrigger = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (salida == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(salida.position, 0.2f);
    }

    private void OnEnable()
    {
        ultimoSegundo = -1;
        ActualizarIndicador();
    }

    private void Update() => ActualizarIndicador();

    public bool EstaEnAlcance(BoxCollider2D pies)
    {
        if (zonaEntrada == null || !zonaEntrada.isActiveAndEnabled ||
            pies == null || !pies.isActiveAndEnabled)
            return false;

        // Usa la forma real de ambas cajas, incluidos offsets, escala y rotacion.
        ColliderDistance2D distancia = zonaEntrada.Distance(pies);
        return distancia.isValid && distancia.isOverlapped;
    }

    public void ActivarCooldown(float segundos)
    {
        disponibleDesde = Time.time + Mathf.Max(0f, segundos);
        ActualizarIndicador();
    }

    private void ActualizarIndicador()
    {
        int segundos = Mathf.CeilToInt(CooldownRestante);
        if (segundos == ultimoSegundo)
            return;

        ultimoSegundo = segundos;
        if (textoEstado != null)
            textoEstado.text = string.Empty;
        if (panel != null)
        {
            Sprite sprite = segundos > 0 ? spriteCerrada : spriteAbierta;
            if (sprite != null && visualInicializado)
            {
                Bounds limites = sprite.bounds;
                Vector3 escala = new Vector3(
                    escalaPanel.x * limitesPanel.size.x / Mathf.Max(0.001f, limites.size.x),
                    escalaPanel.y * limitesPanel.size.y / Mathf.Max(0.001f, limites.size.y), escalaPanel.z);
                panel.sprite = sprite;
                panel.transform.localScale = escala;
                panel.transform.localPosition = posicionPanel + Vector3.Scale(limitesPanel.center, escalaPanel) -
                    Vector3.Scale(limites.center, escala);
                panel.color = Color.white;
            }
            else panel.color = segundos > 0 ? colorCooldown : colorDisponible;
        }
        if (brillo != null) brillo.Mostrar(segundos == 0);
    }

    private void OnDisable()
    {
        if (brillo != null) brillo.Mostrar(false);
    }
}
