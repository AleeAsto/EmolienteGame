using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(MovimientoJugador2D))]
public class AnimacionJugador : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float esperaIdleAlternativo = 4f;
    private Animator animador;
    private MovimientoJugador2D movimiento;
    private SuccionJugador succion;
    private AtaqueJugador ataque;
    private EstadoCombateJugador estado;
    private float tiempoQuieto;
    private int estadoActual;
    private bool mostrandoEntrada;
    private bool estabaAtacando;
    private AnimatorUpdateMode modoAnterior;
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int IdleAlternativo = Animator.StringToHash("Base Layer.Idle2");
    private static readonly int Run = Animator.StringToHash("Base Layer.Run");
    private static readonly int Talk = Animator.StringToHash("Base Layer.Talk");
    private static readonly int Attack = Animator.StringToHash("Base Layer.attack");

    private void Awake()
    {
        animador = GetComponent<Animator>();
        movimiento = GetComponent<MovimientoJugador2D>();
        succion = GetComponent<SuccionJugador>();
        ataque = GetComponent<AtaqueJugador>();
        estado = GetComponent<EstadoCombateJugador>();
        animador.applyRootMotion = false;
    }

    private void LateUpdate()
    {
        if (mostrandoEntrada)
        {
            if (estadoActual == IdleAlternativo &&
                animador.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                estadoActual = Idle;
                animador.Play(Idle, 0, 0f);
            }
            return;
        }
        // Conservar la pose al pausar o durante la animacion de entrar a una puerta.
        if (Time.timeScale <= 0f || movimiento.EnPuerta) return;

        bool atacando = ataque != null && ataque.EstaAtacando;
        bool iniciaAtaque = atacando && !estabaAtacando;
        estabaAtacando = atacando;
        int siguiente;
        if (estado != null && estado.EstaAturdido)
        {
            tiempoQuieto = 0f;
            siguiente = IdleAlternativo;
        }
        else if (iniciaAtaque || (estadoActual == Attack &&
            animador.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f))
        {
            tiempoQuieto = 0f;
            siguiente = Attack;
        }
        else if (succion != null && succion.Objetivo != null)
        {
            tiempoQuieto = 0f;
            siguiente = Talk;
        }
        else if (movimiento.EstaCaminando || movimiento.EstaEnDash)
        {
            tiempoQuieto = 0f;
            siguiente = Run;
        }
        else
        {
            tiempoQuieto += Time.deltaTime;
            siguiente = tiempoQuieto >= esperaIdleAlternativo ? IdleAlternativo : Idle;
            if (estadoActual == IdleAlternativo && animador.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                tiempoQuieto = 0f;
                siguiente = Idle;
            }
        }

        // No reiniciar el clip cada frame: dejarlo avanzar y repetir normalmente.
        if (siguiente == estadoActual && !(iniciaAtaque && siguiente == Attack)) return;
        estadoActual = siguiente;
        animador.Play(siguiente, 0, 0f);
    }

    public void MostrarEntrada(bool mostrar)
    {
        if (animador == null || mostrandoEntrada == mostrar) return;
        mostrandoEntrada = mostrar;
        tiempoQuieto = 0f;
        if (mostrar)
        {
            modoAnterior = animador.updateMode;
            // La cuenta inicial se reproduce con el tiempo de juego detenido.
            animador.updateMode = AnimatorUpdateMode.UnscaledTime;
            estadoActual = IdleAlternativo;
            animador.Play(IdleAlternativo, 0, 0f);
        }
        else
        {
            animador.updateMode = modoAnterior;
            estadoActual = 0;
        }
    }

    private void OnDisable() => MostrarEntrada(false);
}
