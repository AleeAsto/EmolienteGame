using UnityEngine;

public enum DestinoCarta { Propio, Rival }

[CreateAssetMenu(menuName = "Emoliente/Carta", fileName = "Carta")]
public class DefinicionCarta : ScriptableObject
{
    [SerializeField] private string identificador = "prueba";
    [SerializeField] private string titulo = "Carta de prueba";
    [SerializeField] private Sprite frente;
    [SerializeField] private Sprite reverso;
    [SerializeField, Min(0f)] private float puntosNecesarios = 40f;
    [SerializeField] private DestinoCarta destino;

    public string Identificador => identificador;
    public string Titulo => titulo;
    public Sprite Frente => frente;
    public Sprite Reverso => reverso;
    public float PuntosNecesarios => Mathf.Max(0f, puntosNecesarios);
    public DestinoCarta Destino => destino;
    // Esta primera carta solo se presenta; no modifica el estado de la partida.
}
