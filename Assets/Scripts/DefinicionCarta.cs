using UnityEngine;

public enum DestinoCarta { Propio, Rival }
public enum EfectoCarta { Ninguno, Velocidad, Confusion, Perfume, BombaApestosa, Latigo, IntercambioPosicion }

[CreateAssetMenu(menuName = "Emoliente/Carta", fileName = "Carta")]
public class DefinicionCarta : ScriptableObject
{
    [SerializeField] private string identificador = "prueba";
    [SerializeField] private string titulo = "Carta de prueba";
    [SerializeField] private Sprite frente;
    [SerializeField] private Sprite reverso;
    [Tooltip("Costo en puntos: se descuenta al confirmar el uso.")]
    [SerializeField, Min(0f)] private float puntosNecesarios = 20f;
    [SerializeField] private DestinoCarta destino;
    [SerializeField] private EfectoCarta efecto;
    [SerializeField, Min(0.1f)] private float multiplicadorVelocidad = 1f;
    [SerializeField, Min(0.1f)] private float duracionEfecto = 8f;
    [SerializeField] private bool potente;
    [SerializeField, Min(0.5f)] private float radioOlor = 8f;
    [SerializeField, Min(1f)] private float multiplicadorRetirada = 1.25f;
    [SerializeField, Min(0f)] private float almasExtraLatigo = 40f;
    [SerializeField, Min(0f)] private float empujonLatigo = 2f;

    public string Identificador => identificador;
    public string Titulo => titulo;
    public bool EsPotente => potente;
    public string TituloVisible => potente ? titulo + " ★" : titulo;
    public Sprite Frente => frente;
    public Sprite Reverso => reverso;
    public float PuntosNecesarios => Mathf.Max(0f, puntosNecesarios);
    public DestinoCarta Destino => destino;
    public EfectoCarta Efecto => efecto;
    public float MultiplicadorVelocidad => Mathf.Max(0.1f, multiplicadorVelocidad);
    public bool EsInstantanea => efecto == EfectoCarta.IntercambioPosicion;
    public float DuracionEfecto => EsInstantanea ? 0f : Mathf.Max(0.1f, duracionEfecto);
    public string DuracionVisible => EsInstantanea ? "Instantanea" : $"{DuracionEfecto:0.#} s";
    public float AlmasExtraLatigo => Mathf.Max(0f, almasExtraLatigo);
    public float EmpujonLatigo => Mathf.Max(0f, empujonLatigo);
    public float RadioOlor => Mathf.Max(0.5f, radioOlor);
    public float MultiplicadorRetirada => Mathf.Max(1f, multiplicadorRetirada);
    public Color ColorCarta => destino == DestinoCarta.Propio
        ? new Color(0.52f, 1f, 0.62f) : new Color(1f, 0.48f, 0.48f);
    public string Descripcion => efecto == EfectoCarta.Velocidad
        ? $"Movimiento, absorcion\ny golpes\n{(MultiplicadorVelocidad - 1f) * 100f:+0;-0;0}% por {DuracionEfecto:0.#} s"
        : efecto == EfectoCarta.Confusion
            ? $"Estudiantes falsos\ny mano oculta\n{DuracionEfecto:0.#} segundos"
            : efecto == EfectoCarta.Perfume ? $"Atrae estudiantes\nRadio {RadioOlor:0.#}\n{DuracionEfecto:0.#} segundos"
            : efecto == EfectoCarta.BombaApestosa ? $"Aleja estudiantes\nRadio {RadioOlor:0.#}\n{DuracionEfecto:0.#} segundos"
            : efecto == EfectoCarta.Latigo ? $"Dash y latigazo\nMarca al rival {DuracionEfecto:0.#} s\nGolpe: -{AlmasExtraLatigo:0.#} extra"
            : efecto == EfectoCarta.IntercambioPosicion ? "Intercambia posiciones\ncon el rival\nSin limite de distancia" : "Sin efecto";
}
