using UnityEngine;
using UnityEngine.InputSystem;

// Un solo punto de entrada: las acciones siguen pasando por las reglas del jugador.
[DisallowMultipleComponent]
public sealed class EntradaJugador : MonoBehaviour
{
    public bool EsCPU { get; private set; }
    public Vector2 MovimientoCPU { get; private set; }
    public bool SuccionCPU { get; private set; }
    public bool PuertaCPU { get; private set; }
    public bool TieneFoco => EsCPU || Application.isFocused;

    public static EntradaJugador Obtener(Component jugador)
    {
        var entrada = jugador.GetComponent<EntradaJugador>();
        return entrada != null ? entrada : jugador.gameObject.AddComponent<EntradaJugador>();
    }

    public Vector2 LeerMovimiento(InputAction accion) => EsCPU ? MovimientoCPU :
        Application.isFocused && accion != null ? accion.ReadValue<Vector2>() : Vector2.zero;

    public bool LeerSuccion(InputAction accion) => EsCPU ? SuccionCPU :
        Application.isFocused && accion != null && accion.IsPressed();

    public void ActivarCPU(bool activo)
    {
        EsCPU = activo;
        Limpiar();
    }

    public void Ordenar(Vector2 movimiento, bool succionar = false, bool puerta = false)
    {
        MovimientoCPU = Vector2.ClampMagnitude(movimiento, 1f);
        SuccionCPU = succionar;
        PuertaCPU = puerta;
    }

    public void Limpiar() => Ordenar(Vector2.zero);
    private void OnDisable() => Limpiar();
}
