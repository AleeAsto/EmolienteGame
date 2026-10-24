using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AlmasJugador : MonoBehaviour
{
    [SerializeField, Min(0)] private int cantidad;

    public int Cantidad => cantidad;
    public event Action<int> CantidadCambiada;

    public void Recibir(int almas)
    {
        if (almas <= 0)
            return;

        cantidad += almas;
        CantidadCambiada?.Invoke(cantidad);
    }
}
