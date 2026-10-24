using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class EstadoCombateJugador : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicadorAturdimiento;

    private MovimientoJugador2D movimiento;
    private float aturdimientoRestante;

    public bool EstaAturdido => aturdimientoRestante > 0f;
    public float AturdimientoRestante => aturdimientoRestante;
    public event Action Interrumpido;

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        ActualizarIndicador();
    }

    private void Update()
    {
        aturdimientoRestante = Mathf.Max(0f, aturdimientoRestante - Time.deltaTime);
        ActualizarIndicador();
    }

    public void RecibirGolpe(float segundos, Vector2 empujon)
    {
        aturdimientoRestante += Mathf.Max(0f, segundos);
        Interrumpido?.Invoke();
        if (movimiento != null)
            movimiento.Empujar(empujon);
        ActualizarIndicador();
    }

    private void ActualizarIndicador()
    {
        if (indicadorAturdimiento != null)
            indicadorAturdimiento.enabled = EstaAturdido;
    }

    private void OnDisable()
    {
        aturdimientoRestante = 0f;
        Interrumpido?.Invoke();
        ActualizarIndicador();
    }
}
