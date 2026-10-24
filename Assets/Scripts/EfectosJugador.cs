using UnityEngine;

[DisallowMultipleComponent]
public class EfectosJugador : MonoBehaviour
{
    // Time.time respeta la pausa y conserva duraciones independientes por tipo.
    private CicloPartida partida;
    private readonly ModificadoresVelocidad velocidad = new ModificadoresVelocidad();

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

    public void Limpiar() => velocidad.Limpiar();
    private void OnDisable() => Limpiar();
}
