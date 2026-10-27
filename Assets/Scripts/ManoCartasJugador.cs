using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public class ManoCartasJugador : MonoBehaviour
{
    public const int Capacidad = 3;
    private readonly List<DefinicionCarta> cartas = new List<DefinicionCarta>(Capacidad);
    private readonly List<DefinicionCarta> entregasPendientes = new List<DefinicionCarta>();
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
    private EntradaJugador entradaJugador;

    public bool MenuAbierto { get; private set; }
    public float TiempoApertura => sistema != null ? sistema.TiempoMantener : 0.35f;
    public float TiempoHastaEfecto(DefinicionCarta carta) => TiempoApertura + (sistema != null ? sistema.DuracionUso(carta) : 0f);
    public bool TieneCartaEnActivacion(EfectoCarta efecto) => sistema != null && sistema.TieneUsoPendiente(this, efecto);
    public bool BloqueaSuccion => MenuAbierto || frameBloqueado == Time.frameCount;
    public int Seleccion { get; private set; }
    public int Cantidad => cartas.Count;
    public int EspaciosOcupados => Cantidad + entregasPendientes.Count;
    public bool TienePotente => cartas.Exists(c => c.EsPotente) || entregasPendientes.Exists(c => c.EsPotente);
    public bool EstaConfundido => GetComponent<EfectosJugador>() != null && GetComponent<EfectosJugador>().EstaConfundido;
    public IReadOnlyList<DefinicionCarta> Cartas => cartas;
    public AlmasJugador Almas => almas;
    public float AvisoHasta { get; private set; }
    public string Aviso { get; private set; }
    public int IndiceRecibida { get; private set; } = -1;
    public float RecibidaHasta { get; private set; }
    public event Action Cambiada;

    public void Inicializar(SistemaCartas controlador)
    {
        entradaJugador = EntradaJugador.Obtener(this);
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
        if (!PuedeRecibir(carta))
            return false;
        cartas.Add(carta);
        IndiceRecibida = Cantidad - 1;
        RecibidaHasta = Time.time + 2f;
        MostrarAviso("Nueva carta");
        Cambiada?.Invoke();
        return true;
    }

    public bool RecibirRecompensaRaro() => sistema != null && sistema.EntregarPorEstudianteRaro(this);

    private bool PuedeRecibir(DefinicionCarta carta) => carta != null && EspaciosOcupados < Capacidad &&
        sistema != null && sistema.EnCurso && (!carta.EsPotente || !TienePotente);

    public bool ReservarEntrega(DefinicionCarta carta)
    {
        if (!PuedeRecibir(carta)) return false;
        entregasPendientes.Add(carta);
        return true;
    }

    public bool CompletarEntrega(DefinicionCarta carta)
    {
        if (!entregasPendientes.Remove(carta)) return false;
        return Recibir(carta);
    }

    public void CancelarEntregas() => entregasPendientes.Clear();

    public void MezclarPorConfusion()
    {
        var anterior = cartas.ToArray();
        for (int i = cartas.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            DefinicionCarta temporal = cartas[i]; cartas[i] = cartas[j]; cartas[j] = temporal;
        }
        bool cambio = false;
        for (int i = 0; i < cartas.Count; i++) cambio |= cartas[i] != anterior[i];
        if (!cambio && cartas.Count > 1)
        {
            DefinicionCarta primera = cartas[0]; cartas.RemoveAt(0); cartas.Add(primera);
        }
        IndiceRecibida = -1;
        RecibidaHasta = 0f;
        Cambiada?.Invoke();
    }

    private void Update()
    {
        if (sistema == null)
            return;
        // La CPU solicita las mismas operaciones publicas, sin leer el teclado de P2.
        if (entradaJugador.EsCPU)
            return;
        if (!sistema.EnCurso || !Application.isFocused || estado.EstaAturdido ||
            (GetComponent<UsarPuertaJugador>() != null && GetComponent<UsarPuertaJugador>().EstaViajando))
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
            // Respuesta inmediata: no esperar a soltar para distinguir tap/hold.
            // Si sigue manteniendo, despues podra abrir el menu de cartas.
            ataque.IntentarAtacar();
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
                    AudioJuego.Instancia?.CambiarCarta();
                    Cambiada?.Invoke();
                }
                else if (Cantidad == 0)
                    MostrarAviso("Sin cartas");
            }
            else if (golpear.WasReleasedThisFrame())
            {
                pulsacionPendiente = false;
            }
        }
        if (!MenuAbierto)
            return;

        float x = movimiento.AccionMovimiento != null
            ? movimiento.AccionMovimiento.ReadValue<Vector2>().x : 0f;
        int direccion = Mathf.Abs(x) >= 0.5f ? (x > 0f ? 1 : -1) : 0;
        if (direccion != 0 && (direccion != direccionAnterior || Time.time >= siguienteSeleccion))
        {
            int anterior = Seleccion;
            Seleccion = (Seleccion + direccion + Cantidad) % Cantidad;
            if (Seleccion != anterior) AudioJuego.Instancia?.CambiarCarta();
            siguienteSeleccion = Time.time + (direccion != direccionAnterior ? 0.35f : 0.18f);
            Cambiada?.Invoke();
        }
        direccionAnterior = direccion;

        if (succion.AccionSuccionar != null && succion.AccionSuccionar.WasPressedThisFrame())
            IntentarUsar();
    }

    public bool IntentarUsar()
    {
        if (!MenuAbierto || sistema == null || !sistema.EnCurso || estado.EstaAturdido || Cantidad == 0 ||
            (movimiento != null && movimiento.EnPuerta))
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

    public bool AbrirParaCPU(int indice)
    {
        if (entradaJugador == null || !entradaJugador.EsCPU || sistema == null || !sistema.EnCurso ||
            estado.EstaAturdido || movimiento.EnPuerta || ataque.EstaAtacando || indice < 0 || indice >= Cantidad)
            return false;
        succion.CancelarSuccion();
        MenuAbierto = true;
        RecibidaHasta = 0f;
        Seleccion = indice;
        AudioJuego.Instancia?.CambiarCarta();
        Cambiada?.Invoke();
        return true;
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
