using UnityEngine;

[CreateAssetMenu(menuName = "Emoliente/Configuracion de audio")]
public class ConfiguracionAudio : ScriptableObject
{
    [Header("Musica")]
    public AudioClip musicaMenu;
    public AudioClip musicaPartida;
    [Range(0f, 1f)] public float volumenMusica = 0.3f;
    [Min(0f)] public float transicionMusica = 0.4f;

    [Header("Efectos")]
    public AudioClip botonMenu;
    public AudioClip cambiarCarta;
    public AudioClip usarCarta;
    public AudioClip abrirPuerta;
    public AudioClip cerrarPuerta;
    [Range(0f, 1f)] public float volumenEfectos = 0.75f;

    [Header("Pasos")]
    public AudioClip pasos;
    [Range(0f, 1f)] public float volumenPasos = 1f;

}
