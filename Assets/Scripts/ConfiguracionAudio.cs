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
    public AudioClip versus;
    public AudioClip victoria;
    public AudioClip golpe;
    [Tooltip("Un solo impacto por archivo. Se elige una variante por ataque.")]
    public AudioClip[] variantesGolpe;
    public AudioClip estudianteAgotado;
    public AudioClip abrirCasillero;
    public AudioClip cerrarCasillero;
    [Range(0f, 1f)] public float volumenEfectos = 0.75f;

    [Header("Estados de los jugadores")]
    public AudioClip succion;
    public AudioClip aturdimiento;
    [Range(0f, 1f)] public float volumenSuccion = 0.45f;
    [Range(0f, 1f)] public float volumenAturdimiento = 0.4f;

    [Header("Pasos")]
    public AudioClip pasos;
    [Range(0f, 1f)] public float volumenPasos = 1f;

}
