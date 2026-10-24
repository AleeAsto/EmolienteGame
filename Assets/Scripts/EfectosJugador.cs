using UnityEngine;

[DisallowMultipleComponent]
public class EfectosJugador : MonoBehaviour
{
    // Time.time respeta la pausa y conserva duraciones independientes por tipo.
    private CicloPartida partida;
    private readonly ModificadoresVelocidad velocidad = new ModificadoresVelocidad();
    private float finConfusion;
    public bool EstaConfundido => isActiveAndEnabled && partida != null && partida.EnCurso &&
        !partida.Terminada && Time.time < finConfusion;

    public float MultiplicadorVelocidad => partida != null && partida.EnCurso && !partida.Terminada
        ? velocidad.Multiplicador(Time.time) : 1f;

    public void Inicializar(CicloPartida ciclo)
    {
        partida = ciclo;
        Limpiar();
    }

    public void AplicarVelocidad(float multiplicador, float duracion)
    {
        if (isActiveAndEnabled && partida != null && partida.EnCurso && !partida.Terminada)
            velocidad.Aplicar(multiplicador, duracion, Time.time);
    }

    public void AplicarConfusion(float duracion)
    {
        if (!isActiveAndEnabled || partida == null || !partida.EnCurso || partida.Terminada || duracion <= 0f)
            return;
        bool nueva = !EstaConfundido;
        finConfusion = Mathf.Max(finConfusion, Time.time + duracion);
        if (nueva) GetComponent<ManoCartasJugador>()?.MezclarPorConfusion();
    }

    public bool IntentarDisiparFantasma(Vector2 posicion, float distanciaMaximaCuadrada) =>
        EstaConfundido && GetComponent<IlusionesConfusionJugador>() != null &&
        GetComponent<IlusionesConfusionJugador>().IntentarDisipar(posicion, distanciaMaximaCuadrada);

    public void Limpiar()
    {
        velocidad.Limpiar();
        finConfusion = 0f;
        GetComponent<IlusionesConfusionJugador>()?.Limpiar();
    }
    private void OnDisable() => Limpiar();
}
