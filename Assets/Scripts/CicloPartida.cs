using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CicloPartida : MonoBehaviour
{
    [SerializeField, Min(1f)] private float duracionSegundos = 180f;
    [SerializeField] private AlmasJugador jugador1;
    [SerializeField] private AlmasJugador jugador2;
    [SerializeField] private Text textoTiempo;

    public float TiempoRestante { get; private set; }
    public bool Terminada { get; private set; }
    // 1 = jugador 1, 2 = jugador 2, 0 = empate.
    public event Action<int> PartidaTerminada;

    private void Start()
    {
        if (jugador1 == null || jugador2 == null || textoTiempo == null)
        {
            Debug.LogError("Asigna ambos jugadores y el texto del timer de la partida.", this);
            enabled = false;
            return;
        }

        TiempoRestante = Mathf.Max(1f, duracionSegundos);
        ActualizarTexto();
    }

    private void Update()
    {
        if (Terminada)
            return;

        TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
        // Atajo de prueba: adelantar el final sin aumentar el tiempo restante.
        if (Application.isFocused && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            TiempoRestante = Mathf.Min(TiempoRestante, 3f);

        ActualizarTexto();
        if (TiempoRestante > 0f)
            return;

        Terminada = true;
        int puntos1 = jugador1.Cantidad;
        int puntos2 = jugador2.Cantidad;
        int ganador = puntos1 == puntos2 ? 0 : (puntos1 > puntos2 ? 1 : 2);
        string resultado = ganador == 0 ? "Empate" : $"El jugador {ganador} gano";
        Debug.Log($"Fin de la partida. {resultado}. P1: {puntos1} | P2: {puntos2}", this);
        PartidaTerminada?.Invoke(ganador);
    }

    private void ActualizarTexto()
    {
        int segundos = Mathf.CeilToInt(TiempoRestante);
        textoTiempo.text = $"{segundos / 60:00}:{segundos % 60:00}";
    }
}
