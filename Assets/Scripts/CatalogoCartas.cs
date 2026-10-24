using UnityEngine;

[CreateAssetMenu(menuName = "Emoliente/Catalogo de cartas", fileName = "CatalogoCartas")]
public class CatalogoCartas : ScriptableObject
{
    [SerializeField] private DefinicionCarta[] cartas;
    public DefinicionCarta[] Cartas => cartas;
}
