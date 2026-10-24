using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AlmaEstudiante : MonoBehaviour
{
    [SerializeField, Min(1)] private int valorAlmas = 10;
    [SerializeField, Min(0.1f)] private float tiempoRecuperacion = 15f;

    private float recuperacionRestante;
    private AlmasJugador ocupante;

    public int ValorAlmas => valorAlmas;
    public float RecuperacionRestante => recuperacionRestante;
    public bool Disponible => isActiveAndEnabled && recuperacionRestante <= 0f;
    public bool EstaOcupado => ocupante != null;
    public AlmasJugador Ocupante => ocupante;
    public event Action EstadoCambiado;

    private void Update()
    {
        if (recuperacionRestante <= 0f)
            return;

        recuperacionRestante = Mathf.Max(0f, recuperacionRestante - Time.deltaTime);
        if (recuperacionRestante == 0f)
            EstadoCambiado?.Invoke();
    }

    public bool IntentarOcupar(AlmasJugador jugador)
    {
        if (jugador == null || !jugador.isActiveAndEnabled || !Disponible)
            return false;

        if (EstaOcupado)
            return ocupante == jugador;

        ocupante = jugador;
        EstadoCambiado?.Invoke();
        return true;
    }

    public void Liberar(AlmasJugador jugador)
    {
        if (jugador == null || ocupante != jugador)
            return;

        ocupante = null;
        EstadoCambiado?.Invoke();
    }

    // La succion llamara a este metodo al completar su progreso.
    public bool IntentarExtraer(AlmasJugador jugador)
    {
        if (jugador == null || !jugador.isActiveAndEnabled || !Disponible || ocupante != jugador)
            return false;

        recuperacionRestante = Mathf.Max(0.1f, tiempoRecuperacion);
        ocupante = null;
        jugador.Recibir(Mathf.Max(1, valorAlmas));
        EstadoCambiado?.Invoke();
        return true;
    }

    private void OnDisable()
    {
        ocupante = null;
    }
}
