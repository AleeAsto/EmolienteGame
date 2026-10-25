using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RedPuertas : MonoBehaviour
{
    [SerializeField] private PuertaTeletransporte[] puertas;
    [SerializeField, Min(0f)] private float duracionBloqueoSalida = 10f;

    private readonly List<PuertaTeletransporte> candidatas = new List<PuertaTeletransporte>();

    private void Awake()
    {
        if (puertas == null || puertas.Length < 2 || System.Array.Exists(puertas, puerta => puerta == null))
        {
            Debug.LogError("Asigna al menos dos puertas a la red, sin referencias vacias.", this);
            enabled = false;
        }
    }

    public bool IntentarTransportar(UsarPuertaJugador jugador)
    {
        if (!isActiveAndEnabled || jugador == null || !jugador.PuedeUsar)
            return false;

        // MovimientoJugador2D mueve el Transform en Update. Actualizar las cajas
        // antes de consultar su solapamiento, sin esperar al siguiente paso fisico.
        Physics2D.SyncTransforms();
        PuertaTeletransporte entrada = null;
        float menorDistancia = float.PositiveInfinity;
        foreach (PuertaTeletransporte puerta in puertas)
        {
            if (!puerta.Disponible || !puerta.EstaEnAlcance(jugador.ColliderPies))
                continue;

            float distancia = (puerta.transform.position - jugador.PosicionPies).sqrMagnitude;
            if (distancia < menorDistancia)
            {
                entrada = puerta;
                menorDistancia = distancia;
            }
        }
        if (entrada == null)
            return false;

        candidatas.Clear();
        foreach (PuertaTeletransporte puerta in puertas)
            if (puerta != entrada && puerta.Disponible)
                candidatas.Add(puerta);
        if (candidatas.Count == 0)
            return false;

        PuertaTeletransporte salida = candidatas[Random.Range(0, candidatas.Count)];
        // Bloquear el destino antes de mover evita que dos viajes simultaneos
        // elijan la misma salida. La puerta de entrada queda disponible.
        salida.ActivarCooldown(duracionBloqueoSalida);
        jugador.TransportarA(salida.PosicionSalida);
        AudioJuego.Instancia?.UsarPuerta();
        return true;
    }
}
