using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public class AudioEstadoJugador : MonoBehaviour
{
    private SuccionJugador succion;
    private EstadoCombateJugador estado;
    private AudioSource voz, pajaros;

    private void Awake()
    {
        succion = GetComponent<SuccionJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        voz = CrearFuente("AudioSuccion");
        pajaros = CrearFuente("AudioAturdimiento");
    }

    private AudioSource CrearFuente(string nombre)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.priority = 150;
        return fuente;
    }

    private void LateUpdate()
    {
        var audio = AudioJuego.Instancia;
        if (audio == null || audio.Configuracion == null) return;
        var config = audio.Configuracion;
        Actualizar(voz, config.succion, succion != null && succion.isActiveAndEnabled &&
            succion.Objetivo != null, config.volumenSuccion * audio.VolumenEfectos);
        Actualizar(pajaros, config.aturdimiento, estado != null && estado.isActiveAndEnabled &&
            estado.EstaAturdido, config.volumenAturdimiento * audio.VolumenEfectos);
    }

    private static void Actualizar(AudioSource fuente, AudioClip clip, bool activo, float volumen)
    {
        fuente.volume = volumen;
        if (!activo || clip == null) { fuente.Stop(); return; }
        if (fuente.clip != clip) { fuente.Stop(); fuente.clip = clip; }
        if (Time.timeScale <= 0f || !Application.isFocused) { fuente.Pause(); return; }
        fuente.UnPause();
        if (!fuente.isPlaying) fuente.Play();
    }

    private void OnDisable()
    {
        if (voz != null) voz.Stop();
        if (pajaros != null) pajaros.Stop();
    }
}
