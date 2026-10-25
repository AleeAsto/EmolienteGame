using UnityEngine;
using UnityEngine.UI;

public class TextoControlesJugador : MonoBehaviour
{
    [SerializeField] private int jugador = 1;
    [SerializeField] private bool detallado;
    [SerializeField] private Text movimiento;
    [SerializeField] private Text acciones;
    private void OnEnable() { ConfiguracionControles.Cambiados += Actualizar; Actualizar(); }
    private void OnDisable() => ConfiguracionControles.Cambiados -= Actualizar;
    private void Actualizar()
    {
        int inicio = jugador == 1 ? 0 : 6;
        string mover = ConfiguracionControles.Movimiento(jugador);
        string absorber = ConfiguracionControles.Nombre(inicio + 4), golpear = ConfiguracionControles.Nombre(inicio + 5);
        if (detallado) acciones.text = $"MOVERSE       {mover}\nABSORBER      Manten {absorber}\nGOLPEAR       Pulsa {golpear}\nCARTAS        Manten {golpear}";
        else { movimiento.text = mover; acciones.text = $"{absorber} absorber  /  {golpear} golpear"; }
    }
}
