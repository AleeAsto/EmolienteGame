using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AudioJuego : MonoBehaviour
{
    public static AudioJuego Instancia { get; private set; }
    public ConfiguracionAudio Configuracion { get; private set; }

    private AudioSource musica;
    private AudioSource efectosMenu;
    private AudioSource efectosPartida;
    private AudioListener escucha;
    private Coroutine transicion;
    private int versionEscena;
    public float VolumenMusica { get; private set; } = 1f;
    public float VolumenEfectos { get; private set; } = 1f;
    private float VolumenMusicalFinal => Configuracion.volumenMusica * VolumenMusica;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() => Instancia = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Crear()
    {
        if (Instancia == null)
            new GameObject("AudioJuego").AddComponent<AudioJuego>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
        Configuracion = Resources.Load<ConfiguracionAudio>("AudioJuego");
        if (Configuracion == null)
        {
            Debug.LogError("Falta Resources/AudioJuego con la configuracion de audio.", this);
            Instancia = null;
            Destroy(gameObject);
            return;
        }
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenGeneral", 1f));
        VolumenMusica = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenMusica", 1f));
        VolumenEfectos = Mathf.Clamp01(PlayerPrefs.GetFloat("VolumenEfectos", 1f));
        escucha = gameObject.AddComponent<AudioListener>();
        musica = CrearFuente("Musica", true);
        musica.priority = 0;
        efectosMenu = CrearFuente("EfectosMenu", false);
        efectosPartida = CrearFuente("EfectosPartida", false);
        efectosMenu.volume = efectosPartida.volume = VolumenEfectos;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private AudioSource CrearFuente(string nombre, bool repetir)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = repetir;
        fuente.spatialBlend = 0f;
        return fuente;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        versionEscena++;
        // Un unico listener para las dos vistas y para el menu sin camara.
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
            if (listener != escucha) listener.enabled = false;
        efectosPartida.Stop();
        efectosPartida.UnPause();

        bool esPartida = false;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.GetComponentInChildren<CicloPartida>(true) != null) esPartida = true;
            foreach (var jugador in raiz.GetComponentsInChildren<MovimientoJugador2D>(true))
            {
                if (jugador.GetComponent<AudioPasosJugador>() == null)
                    jugador.gameObject.AddComponent<AudioPasosJugador>();
                if (jugador.GetComponent<AudioEstadoJugador>() == null)
                    jugador.gameObject.AddComponent<AudioEstadoJugador>();
            }
        }
        CambiarMusica(esPartida ? Configuracion.musicaPartida : Configuracion.musicaMenu);
    }

    private void CambiarMusica(AudioClip clip)
    {
        if (musica.clip == clip && musica.isPlaying) return;
        if (transicion != null) StopCoroutine(transicion);
        transicion = StartCoroutine(Transicionar(clip));
    }

    public Coroutine DesvanecerMusica()
    {
        if (transicion != null) StopCoroutine(transicion);
        transicion = StartCoroutine(DesvanecerHastaSilencio());
        return transicion;
    }

    private IEnumerator DesvanecerHastaSilencio()
    {
        float duracion = Mathf.Max(0f, Configuracion.transicionMusica);
        float inicial = musica.volume;
        float tiempo = 0f;
        while (musica.isPlaying && tiempo < duracion)
        {
            musica.volume = Mathf.Lerp(inicial, 0f, Mathf.SmoothStep(0f, 1f, tiempo / duracion));
            yield return null;
            tiempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }
        musica.volume = 0f;
        musica.Stop();
        // Al cargar la revancha, isPlaying sera falso aunque el clip sea el mismo.
        transicion = null;
    }

    private IEnumerator Transicionar(AudioClip clip)
    {
        float duracion = Mathf.Max(0f, Configuracion.transicionMusica);
        float inicial = musica.volume;
        for (float t = 0f; musica.isPlaying && t < duracion; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
        {
            musica.volume = Mathf.Lerp(inicial, 0f, t / duracion);
            yield return null;
        }
        musica.Stop();
        musica.clip = clip;
        musica.volume = 0f;
        if (clip != null)
        {
            musica.time = 0f;
            musica.Play();
        }
        for (float t = 0f; t < duracion; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
        {
            musica.volume = Mathf.Lerp(0f, VolumenMusicalFinal, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }
        musica.volume = VolumenMusicalFinal;
        transicion = null;
    }

    public void BotonMenu() => Reproducir(efectosMenu, Configuracion.botonMenu);
    public void CambiarCarta() => Reproducir(efectosPartida, Configuracion.cambiarCarta);
    public void UsarCarta() => Reproducir(efectosPartida, Configuracion.usarCarta);
    public void Versus() => Reproducir(efectosMenu, Configuracion.versus);
    public void Victoria() => Reproducir(efectosMenu, Configuracion.victoria);
    public void Golpe() => Reproducir(efectosPartida, Configuracion.golpe);
    public void EstudianteAgotado() => Reproducir(efectosPartida, Configuracion.estudianteAgotado);
    public void Casillero(bool abierto) => Reproducir(efectosPartida,
        abierto ? Configuracion.abrirCasillero : Configuracion.cerrarCasillero);

    public void CambiarVolumenMusica(float valor)
    {
        VolumenMusica = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenMusica", VolumenMusica);
        if (transicion == null && musica != null) musica.volume = VolumenMusicalFinal;
    }

    public void CambiarVolumenEfectos(float valor)
    {
        VolumenEfectos = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenEfectos", VolumenEfectos);
        if (efectosMenu != null) efectosMenu.volume = VolumenEfectos;
        if (efectosPartida != null) efectosPartida.volume = VolumenEfectos;
    }

    public void PausarEfectos(bool pausa)
    {
        if (efectosPartida == null) return;
        if (pausa) efectosPartida.Pause();
        else efectosPartida.UnPause();
    }

    public void UsarPuerta()
    {
        Reproducir(efectosPartida, Configuracion.abrirPuerta);
        StartCoroutine(CerrarPuerta(versionEscena));
    }

    private IEnumerator CerrarPuerta(int version)
    {
        float espera = Configuracion.abrirPuerta != null ? Configuracion.abrirPuerta.length : 0f;
        yield return new WaitForSeconds(espera);
        if (version == versionEscena) Reproducir(efectosPartida, Configuracion.cerrarPuerta);
    }

    private void Reproducir(AudioSource fuente, AudioClip clip)
    {
        if (fuente != null && clip != null)
            fuente.PlayOneShot(clip, Configuracion.volumenEfectos);
    }

    private void OnDestroy()
    {
        if (Instancia != this) return;
        SceneManager.sceneLoaded -= AlCargarEscena;
        Instancia = null;
    }
}
