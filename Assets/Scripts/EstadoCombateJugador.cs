using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class EstadoCombateJugador : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicadorAturdimiento;
    [SerializeField] private ZonaDanoJugador zonaDano;
    public ZonaDanoJugador ZonaDano => zonaDano;
    [SerializeField, Min(0f)] private float invulnerabilidadTrasStun = 0.5f;
    [SerializeField] private Color colorAturdido = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color colorProtegido = new Color(0.4f, 0.9f, 1f, 1f);

    private MovimientoJugador2D movimiento;
    private float aturdimientoRestante;
    private float invulnerabilidadRestante;

    public bool EstaAturdido => aturdimientoRestante > 0f;
    public float AturdimientoRestante => aturdimientoRestante;
    public bool EsInvulnerable => invulnerabilidadRestante > 0f;
    public float InvulnerabilidadRestante => invulnerabilidadRestante;
    public event Action Interrumpido;

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        ActualizarIndicador();
    }

    private void Update()
    {
        aturdimientoRestante = Mathf.Max(0f, aturdimientoRestante - Time.deltaTime);
        invulnerabilidadRestante = Mathf.Max(0f, invulnerabilidadRestante - Time.deltaTime);
        ActualizarIndicador();
    }

    public bool RecibirGolpe(float segundos, Vector2 empujon)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || EsInvulnerable)
            return false;

        aturdimientoRestante = Mathf.Max(0f, segundos);
        invulnerabilidadRestante = aturdimientoRestante + Mathf.Max(0f, invulnerabilidadTrasStun);
        Interrumpido?.Invoke();
        if (movimiento != null)
            movimiento.Empujar(empujon);
        ActualizarIndicador();
        return true;
    }

    private void ActualizarIndicador()
    {
        if (indicadorAturdimiento != null)
        {
            indicadorAturdimiento.enabled = EstaAturdido || EsInvulnerable;
            indicadorAturdimiento.color = EstaAturdido ? colorAturdido : colorProtegido;
        }
    }

    private void OnDisable()
    {
        aturdimientoRestante = 0f;
        invulnerabilidadRestante = 0f;
        Interrumpido?.Invoke();
        ActualizarIndicador();
    }
}
