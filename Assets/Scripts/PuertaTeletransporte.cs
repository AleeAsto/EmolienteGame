using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class PuertaTeletransporte : MonoBehaviour
{
    [SerializeField] private int numero = 1;
    [SerializeField] private Transform salida;
    [SerializeField] private SpriteRenderer panel;
    [SerializeField] private Text textoEstado;
    [SerializeField] private Color colorDisponible = new Color(0.25f, 0.85f, 0.65f, 1f);
    [SerializeField] private Color colorCooldown = new Color(0.4f, 0.42f, 0.5f, 1f);

    private float disponibleDesde;
    private int ultimoSegundo = -1;
    private BoxCollider2D zonaEntrada;

    private void Reset()
    {
        var zona = GetComponent<BoxCollider2D>();
        zona.isTrigger = true;
        zona.size = new Vector2(2.4f, 1.6f);
    }

    private void Awake() => zonaEntrada = GetComponent<BoxCollider2D>();

    public float CooldownRestante => Mathf.Max(0f, disponibleDesde - Time.time);
    public bool Disponible => isActiveAndEnabled && CooldownRestante <= 0f;
    public Vector3 PosicionSalida => salida != null ? salida.position : transform.position;

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
            textoEstado.text = "PUERTA " + numero + "\n" + (segundos > 0 ? segundos + " s" : "ARRIBA");
        if (panel != null)
            panel.color = segundos > 0 ? colorCooldown : colorDisponible;
    }
}
