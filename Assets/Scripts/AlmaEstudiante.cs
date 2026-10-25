using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AlmaEstudiante : MonoBehaviour
{
    [SerializeField, Min(1)] private int valorAlmas = 10;
    [SerializeField] private bool esRaro;
    private bool agotado;
    private float tiempoAgotado;
    private bool asustado;
    private AlmasJugador ocupante;

    public int ValorAlmas => valorAlmas;
    public bool EsRaro => esRaro;
    public bool Asustado => asustado;
    public bool Agotado => agotado;
    public float SegundosAgotado => agotado ? Mathf.Max(0f, Time.time - tiempoAgotado) : 0f;
    public bool Disponible => isActiveAndEnabled && !agotado && !asustado;
    public bool EstaOcupado => ocupante != null;
    public AlmasJugador Ocupante => ocupante;
    public event Action EstadoCambiado;

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
        if (!agotado && !asustado && !esRaro)
        {
            asustado = true;
            GetComponent<EstudiantePasillo>()?.HuirDe(jugador.transform.position);
        }
        EstadoCambiado?.Invoke();
    }

    public bool IntentarExtraer(AlmasJugador jugador)
    {
        if (jugador == null || !jugador.isActiveAndEnabled || !Disponible || ocupante != jugador)
            return false;

        agotado = true;
        tiempoAgotado = Time.time;
        ocupante = null;
        jugador.Recibir(Mathf.Max(1, valorAlmas));
        if (esRaro)
            jugador.GetComponent<ManoCartasJugador>()?.RecibirRecompensaRaro();
        EstadoCambiado?.Invoke();
        return true;
    }

    private void OnDisable()
    {
        ocupante = null;
    }
}
