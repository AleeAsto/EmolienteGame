using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class CicloPartida : MonoBehaviour
{
    [SerializeField, Min(1f)] private float duracionSegundos = 180f;
    [SerializeField] private AlmasJugador jugador1;
    [SerializeField] private AlmasJugador jugador2;
    [SerializeField] private Text textoTiempo;
    [SerializeField] private CuentaRegresivaInicial cuentaRegresiva;

    public float TiempoRestante { get; private set; }
    public bool Terminada { get; private set; }
    public bool EnCurso { get; private set; }
    private bool controlaTiempo;
    // 1 = jugador 1, 2 = jugador 2, 0 = empate.
    public event Action<int> PartidaTerminada;

    private void Awake()
    {
        if (jugador1 == null || jugador2 == null || textoTiempo == null || cuentaRegresiva == null)
        {
            Debug.LogError("Asigna ambos jugadores, el timer y el texto de cuenta regresiva.", this);
            enabled = false;
            return;
        }

        TiempoRestante = Mathf.Max(1f, duracionSegundos);
        ActualizarTexto();
        controlaTiempo = true;
        Time.timeScale = 0f;
        cuentaRegresiva.Ocultar();
    }

    private IEnumerator Start()
    {
        if (!controlaTiempo)
            yield break;

        for (int numero = 3; numero >= 1; numero--)
        {
            yield return cuentaRegresiva.Mostrar(numero.ToString(), 1f);
        }

        EnCurso = true;
        Time.timeScale = 1f;
        yield return cuentaRegresiva.Mostrar("GO!", 0.6f);
    }

    private void Update()
    {
        if (!EnCurso || Terminada || Time.timeScale <= 0f)
            return;

        TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
        // Atajo de prueba: adelantar el final sin aumentar el tiempo restante.
        if (Application.isFocused && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            TiempoRestante = Mathf.Min(TiempoRestante, 3f);

        ActualizarTexto();
        if (TiempoRestante > 0f)
            return;

        Terminada = true;
        EnCurso = false;
        Time.timeScale = 0f;
        int puntos1 = jugador1.Cantidad;
        int puntos2 = jugador2.Cantidad;
        int ganador = puntos1 == puntos2 ? 0 : (puntos1 > puntos2 ? 1 : 2);
        string resultado = ganador == 0 ? "Empate" : $"El jugador {ganador} gano";
        Debug.Log($"Fin de la partida. {resultado}. P1: {puntos1} | P2: {puntos2}", this);
        PartidaTerminada?.Invoke(ganador);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (cuentaRegresiva != null)
            cuentaRegresiva.Ocultar();
        EnCurso = false;
        // No dejar la siguiente escena (o partida) con el tiempo congelado.
        if (controlaTiempo)
            Time.timeScale = 1f;
    }

    private void ActualizarTexto()
    {
        int segundos = Mathf.CeilToInt(TiempoRestante);
        textoTiempo.text = $"{segundos / 60:00}:{segundos % 60:00}";
    }
}
