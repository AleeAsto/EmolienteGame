using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class CombatePvP : MonoBehaviour
{
    [SerializeField] private AtaqueJugador jugador1;
    [SerializeField] private AtaqueJugador jugador2;
    [SerializeField] private SpriteRenderer destelloChoque;
    [SerializeField, Min(0.01f)] private float duracionDestello = 0.16f;

    private float destelloRestante;
    private Vector3 escalaDestello;

    private void Awake()
    {
        if (jugador1 == null || jugador2 == null || jugador1 == jugador2)
        {
            Debug.LogError("Asigna los ataques de P1 y P2 al coordinador PvP.", this);
            enabled = false;
        }
        if (destelloChoque != null)
        {
            escalaDestello = destelloChoque.transform.localScale;
            destelloChoque.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        ActualizarDestello();
        if (jugador1 == null || jugador2 == null ||
            !jugador1.isActiveAndEnabled || !jugador2.isActiveAndEnabled)
            return;

        // Ambos Update ya procesaron sus teclas. Los choques se resuelven antes
        // que los impactos para que el orden de componentes no favorezca a P1.
        if (ChocanLosGolpes())
        {
            ResolverChoque();
            return;
        }

        bool golpe1 = jugador1.PuedeImpactar && jugador1.EnAlcance(jugador2);
        bool golpe2 = jugador2.PuedeImpactar && jugador2.EnAlcance(jugador1);
        bool succionaba1 = jugador1.EstaSuccionando;
        bool succionaba2 = jugador2.EstaSuccionando;
        if (golpe1)
            AplicarGolpe(jugador1, jugador2, succionaba2);
        if (golpe2)
            AplicarGolpe(jugador2, jugador1, succionaba1);
    }

    private bool ChocanLosGolpes()
    {
        if (!jugador1.PuedeChocar || !jugador2.PuedeChocar)
            return false;

        Vector2 distancia = jugador2.transform.position - jugador1.transform.position;
        if (Mathf.Abs(distancia.y) > Mathf.Min(jugador1.ToleranciaY, jugador2.ToleranciaY) ||
            distancia.x * jugador1.Direccion < -0.05f || -distancia.x * jugador2.Direccion < -0.05f)
            return false;

        // Barrido relativo de los circulos para no saltar un choque entre frames.
        Vector2 inicio = jugador1.PosicionAnteriorGolpe - jugador2.PosicionAnteriorGolpe;
        Vector2 final = jugador1.PosicionGolpe - jugador2.PosicionGolpe;
        Vector2 recorrido = final - inicio;
        float t = recorrido.sqrMagnitude > 0.000001f
            ? Mathf.Clamp01(-Vector2.Dot(inicio, recorrido) / recorrido.sqrMagnitude)
            : 0f;
        float radio = jugador1.RadioGolpe + jugador2.RadioGolpe;
        return (inicio + recorrido * t).sqrMagnitude <= radio * radio;
    }

    private void ResolverChoque()
    {
        Vector2 punto = (jugador1.PosicionGolpe + jugador2.PosicionGolpe) * 0.5f;
        // El choque cancela ambas animaciones incluso si uno esta protegido.
        jugador1.CancelarAtaque();
        jugador2.CancelarAtaque();
        Vector2 hacia2 = DireccionEmpujon(jugador1, jugador2);
        jugador1.Estado.RecibirGolpe(jugador2.StunBloqueo, -hacia2 * jugador2.EmpujonBloqueo);
        jugador2.Estado.RecibirGolpe(jugador1.StunBloqueo, hacia2 * jugador1.EmpujonBloqueo);
        if (destelloChoque != null)
        {
            destelloChoque.transform.position = new Vector3(punto.x, punto.y, jugador1.transform.position.z);
            destelloRestante = Mathf.Max(0.01f, duracionDestello);
            ActualizarDestello();
        }
    }

    private static void AplicarGolpe(AtaqueJugador atacante, AtaqueJugador victima, bool succionaba)
    {
        atacante.ConsumirImpacto();
        float stun = succionaba ? atacante.StunSuccionando : atacante.StunNormal;
        victima.Estado.RecibirGolpe(stun, DireccionEmpujon(atacante, victima) * atacante.Empujon);
    }

    private static Vector2 DireccionEmpujon(AtaqueJugador atacante, AtaqueJugador victima)
    {
        float diferencia = victima.transform.position.x - atacante.transform.position.x;
        return Vector2.right * (Mathf.Abs(diferencia) > 0.01f ? Mathf.Sign(diferencia) : atacante.Direccion);
    }

    private void ActualizarDestello()
    {
        if (destelloChoque == null)
            return;

        destelloRestante = Mathf.Max(0f, destelloRestante - Time.deltaTime);
        destelloChoque.enabled = destelloRestante > 0f;
        if (destelloRestante <= 0f)
            return;

        float t = destelloRestante / Mathf.Max(0.01f, duracionDestello);
        destelloChoque.transform.localScale = escalaDestello * Mathf.Lerp(1.5f, 0.5f, t);
        destelloChoque.color = new Color(1f, 1f, 1f, t);
    }

    private void OnDisable()
    {
        if (destelloChoque != null)
            destelloChoque.enabled = false;
    }
}
