using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuertaTeletransporte : MonoBehaviour
{
    [SerializeField] private int numero = 1;
    [SerializeField] private Transform salida;
    [SerializeField] private SpriteRenderer panel;
    [SerializeField] private Text textoEstado;
    [SerializeField, Min(0f)] private float alcanceX = 1.2f;
    [SerializeField, Min(0f)] private float alcanceY = 0.8f;
    [SerializeField] private Color colorDisponible = new Color(0.25f, 0.85f, 0.65f, 1f);
    [SerializeField] private Color colorCooldown = new Color(0.4f, 0.42f, 0.5f, 1f);

    private float disponibleDesde;
    private int ultimoSegundo = -1;

    public float CooldownRestante => Mathf.Max(0f, disponibleDesde - Time.time);
    public bool Disponible => isActiveAndEnabled && CooldownRestante <= 0f;
    public Vector3 PosicionSalida => salida != null ? salida.position : transform.position;

    private void OnEnable()
    {
        ultimoSegundo = -1;
        ActualizarIndicador();
    }

    private void Update() => ActualizarIndicador();

    public bool EstaEnAlcance(Vector3 posicion)
    {
        Vector3 distancia = posicion - transform.position;
        return Mathf.Abs(distancia.x) <= alcanceX && Mathf.Abs(distancia.y) <= alcanceY;
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
