using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AlmasJugador : MonoBehaviour
{
    [SerializeField, Min(0f)] private float cantidad;

    public float Cantidad => cantidad;
    public event Action<float> CantidadCambiada;

    public void Recibir(float almas)
    {
        if (almas <= 0)
            return;

        cantidad += almas;
        CantidadCambiada?.Invoke(cantidad);
    }

    public void Perder(float almas)
    {
        if (almas <= 0f || cantidad <= 0f)
            return;

        cantidad = Mathf.Max(0f, cantidad - almas);
        CantidadCambiada?.Invoke(cantidad);
    }

    public bool IntentarGastar(float almas)
    {
        if (float.IsNaN(almas) || float.IsInfinity(almas) || almas < 0f || cantidad < almas)
            return false;
        if (almas == 0f)
            return true;
        cantidad -= almas;
        CantidadCambiada?.Invoke(cantidad);
        return true;
    }
}
