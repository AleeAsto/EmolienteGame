using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovimientoJugador2D))]
public class AudioPasosJugador : MonoBehaviour
{
    private MovimientoJugador2D movimiento;
    private EfectosJugador efectos;
    private EstadoCombateJugador estado;
    private AudioSource fuente;

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        efectos = GetComponent<EfectosJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.priority = 160;
    }

    private void LateUpdate()
    {
        var audio = AudioJuego.Instancia;
        if (audio == null || audio.Configuracion == null) return;
        var config = audio.Configuracion;
        bool caminar = Application.isFocused && Time.timeScale > 0f && movimiento.isActiveAndEnabled &&
            movimiento.EstaCaminando && !movimiento.EstaEnDash && (estado == null || !estado.EstaAturdido);
        if (!caminar || config.pasos == null)
        {
            fuente.Stop();
            return;
        }
        if (fuente.clip != config.pasos) fuente.clip = config.pasos;
        fuente.volume = config.volumenPasos * audio.VolumenEfectos;
        fuente.pitch = Mathf.Clamp(efectos != null ? efectos.MultiplicadorVelocidad : 1f, 0.7f, 1.6f);
        if (!fuente.isPlaying) fuente.Play();
    }

    private void OnDisable()
    {
        if (fuente != null) fuente.Stop();
    }
}
