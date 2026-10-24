using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public class ManoCartasJugador : MonoBehaviour
{
    public const int Capacidad = 3;
    private readonly List<DefinicionCarta> cartas = new List<DefinicionCarta>(Capacidad);
    private SistemaCartas sistema;
    private AlmasJugador almas;
    private MovimientoJugador2D movimiento;
    private AtaqueJugador ataque;
    private SuccionJugador succion;
    private EstadoCombateJugador estado;
    private bool pulsacionPendiente;
    private float tiempoPulsacion;
    private int direccionAnterior;
    private float siguienteSeleccion;
    private int frameBloqueado = -1;

    public bool MenuAbierto { get; private set; }
    public bool BloqueaSuccion => MenuAbierto || frameBloqueado == Time.frameCount;
    public int Seleccion { get; private set; }
    public int Cantidad => cartas.Count;
    public IReadOnlyList<DefinicionCarta> Cartas => cartas;
    public AlmasJugador Almas => almas;
    public float AvisoHasta { get; private set; }
    public string Aviso { get; private set; }
    public int IndiceRecibida { get; private set; } = -1;
    public float RecibidaHasta { get; private set; }
    public event Action Cambiada;

    public void Inicializar(SistemaCartas controlador)
    {
        sistema = controlador;
        almas = GetComponent<AlmasJugador>();
        movimiento = GetComponent<MovimientoJugador2D>();
        ataque = GetComponent<AtaqueJugador>();
        succion = GetComponent<SuccionJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        estado.Interrumpido += Cerrar;
    }

    public bool Recibir(DefinicionCarta carta)
    {
        if (carta == null || Cantidad >= Capacidad || sistema == null || !sistema.EnCurso)
            return false;
        cartas.Add(carta);
        IndiceRecibida = Cantidad - 1;
        RecibidaHasta = Time.time + 2f;
        MostrarAviso("Nueva carta");
        Cambiada?.Invoke();
        return true;
    }

    private void Update()
    {
        if (sistema == null)
            return;
        if (!sistema.EnCurso || !Application.isFocused || estado.EstaAturdido)
        {
            Cerrar();
            return;
        }
        var golpear = ataque.AccionGolpear;
        if (golpear == null)
            return;

        if (golpear.WasPressedThisFrame())
        {
            if (MenuAbierto)
            {
                Cerrar();
                frameBloqueado = Time.frameCount;
                return;
            }
            pulsacionPendiente = true;
            tiempoPulsacion = 0f;
        }
        if (pulsacionPendiente)
        {
            tiempoPulsacion += Time.deltaTime;
            if (golpear.IsPressed() && tiempoPulsacion >= sistema.TiempoMantener)
            {
                pulsacionPendiente = false;
                if (Cantidad > 0 && !ataque.EstaAtacando)
                {
                    MenuAbierto = true;
                    RecibidaHasta = 0f;
                    Seleccion = Mathf.Clamp(Seleccion, 0, Cantidad - 1);
                    direccionAnterior = 0;
                    siguienteSeleccion = 0f;
                    succion.CancelarSuccion();
                    Cambiada?.Invoke();
                }
                else if (Cantidad == 0)
                    MostrarAviso("Sin cartas");
            }
            else if (golpear.WasReleasedThisFrame())
            {
                pulsacionPendiente = false;
                ataque.IntentarAtacar();
            }
        }
        if (!MenuAbierto)
            return;

        float x = movimiento.AccionMovimiento != null
            ? movimiento.AccionMovimiento.ReadValue<Vector2>().x : 0f;
        int direccion = Mathf.Abs(x) >= 0.5f ? (x > 0f ? 1 : -1) : 0;
        if (direccion != 0 && (direccion != direccionAnterior || Time.time >= siguienteSeleccion))
        {
            Seleccion = (Seleccion + direccion + Cantidad) % Cantidad;
            siguienteSeleccion = Time.time + (direccion != direccionAnterior ? 0.35f : 0.18f);
            Cambiada?.Invoke();
        }
        direccionAnterior = direccion;

        if (succion.AccionSuccionar != null && succion.AccionSuccionar.WasPressedThisFrame())
            IntentarUsar();
    }

    public bool IntentarUsar()
    {
        if (!MenuAbierto || sistema == null || !sistema.EnCurso || estado.EstaAturdido || Cantidad == 0)
            return false;
        DefinicionCarta carta = cartas[Seleccion];
        if (!almas.IntentarGastar(carta.PuntosNecesarios))
        {
            MostrarAviso($"Necesitas {carta.PuntosNecesarios:0.#} puntos");
            return false;
        }
        sistema.Presentar(this, carta);
        if (Seleccion == IndiceRecibida)
        {
            IndiceRecibida = -1;
            RecibidaHasta = 0f;
        }
        else if (Seleccion < IndiceRecibida)
            IndiceRecibida--;
        cartas.RemoveAt(Seleccion);
        Seleccion = Mathf.Clamp(Seleccion, 0, Mathf.Max(0, Cantidad - 1));
        frameBloqueado = Time.frameCount;
        Cerrar();
        return true;
    }

    public void Cerrar()
    {
        pulsacionPendiente = false;
        if (!MenuAbierto)
            return;
        MenuAbierto = false;
        frameBloqueado = Time.frameCount;
        Cambiada?.Invoke();
    }

    private void MostrarAviso(string mensaje)
    {
        Aviso = mensaje;
        AvisoHasta = Time.time + 2f;
    }

    private void OnDisable() => Cerrar();
    private void OnDestroy()
    {
        if (estado != null)
            estado.Interrumpido -= Cerrar;
    }
}
