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
        var olor = GetComponent<AuraOlorJugador>();
        if (olor == null) olor = gameObject.AddComponent<AuraOlorJugador>();
        olor.Inicializar(ciclo);
        var marca = GetComponent<MarcaLatigoJugador>();
        if (marca == null) marca = gameObject.AddComponent<MarcaLatigoJugador>();
        marca.Inicializar(ciclo);
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

    public bool IntentarDisiparFantasma(Vector2 posicion, float distanciaMaximaCuadrada, float escalaHorizontal = 1f) =>
        EstaConfundido && GetComponent<IlusionesConfusionJugador>() != null &&
        GetComponent<IlusionesConfusionJugador>().IntentarDisipar(posicion, distanciaMaximaCuadrada, escalaHorizontal);

    public void Limpiar()
    {
        velocidad.Limpiar();
        finConfusion = 0f;
        GetComponent<IlusionesConfusionJugador>()?.Limpiar();
        GetComponent<AuraOlorJugador>()?.Limpiar();
        GetComponent<MarcaLatigoJugador>()?.Limpiar();
    }
    public void AplicarOlor(bool repeler, float duracion, float radio, float retirada, float atraccion = 1.5f) =>
        GetComponent<AuraOlorJugador>()?.Aplicar(repeler, duracion, radio, retirada, atraccion);
    private void OnDisable() => Limpiar();
}
