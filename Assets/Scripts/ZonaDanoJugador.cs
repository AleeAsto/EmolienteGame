using UnityEngine;

// Caja independiente de los pies usados para movimiento y puertas.
[DisallowMultipleComponent]
public class ZonaDanoJugador : MonoBehaviour
{
    [SerializeField] private BoxCollider2D caja;
    private readonly Vector2[] esquinas = new Vector2[4];

    public Vector3 Centro => caja.transform.TransformPoint(caja.offset);
    public bool Disponible => isActiveAndEnabled && caja != null && caja.isActiveAndEnabled;
    public Bounds Limites
    {
        get
        {
            if (!Disponible) return new Bounds(transform.position, Vector3.zero);
            ObtenerEsquinas();
            Bounds limites = new Bounds(esquinas[0], Vector3.zero);
            for (int i = 1; i < esquinas.Length; i++) limites.Encapsulate(esquinas[i]);
            return limites;
        }
    }

    private void ObtenerEsquinas()
    {
        Vector2 mitad = caja.size * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 signo = new Vector2(i == 0 || i == 3 ? -1f : 1f, i < 2 ? -1f : 1f);
            esquinas[i] = caja.transform.TransformPoint(caja.offset + Vector2.Scale(mitad, signo));
        }
    }

    public bool IntersectaRectangulo(Vector2 centro, Vector2 tamano)
    {
        if (!Disponible) return false;
        ObtenerEsquinas();
        // SAT: respeta tambien una caja de dano rotada o escalada.
        return SolapaEje(Vector2.right, centro, tamano) && SolapaEje(Vector2.up, centro, tamano) &&
            SolapaEje(Perpendicular(esquinas[1] - esquinas[0]), centro, tamano) &&
            SolapaEje(Perpendicular(esquinas[3] - esquinas[0]), centro, tamano);
    }

    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);

    private bool SolapaEje(Vector2 eje, Vector2 centro, Vector2 tamano)
    {
        float minimo = float.PositiveInfinity, maximo = float.NegativeInfinity;
        foreach (Vector2 punto in esquinas)
        {
            float proyeccion = Vector2.Dot(punto, eje);
            minimo = Mathf.Min(minimo, proyeccion);
            maximo = Mathf.Max(maximo, proyeccion);
        }
        float medio = Vector2.Dot(centro, eje);
        float radio = (Mathf.Abs(eje.x) * tamano.x + Mathf.Abs(eje.y) * tamano.y) * 0.5f;
        return maximo >= medio - radio && minimo <= medio + radio;
    }

    public bool IntersectaElipse(Vector2 centro, Vector2 radios)
    {
        if (!Disponible) return false;
        ObtenerEsquinas();
        // En el espacio de la elipse, comprobar caja contra circulo unitario.
        Vector2 escala = new Vector2(1f / Mathf.Max(0.01f, radios.x), 1f / Mathf.Max(0.01f, radios.y));
        bool positivo = false, negativo = false;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = Vector2.Scale(esquinas[i] - centro, escala);
            Vector2 b = Vector2.Scale(esquinas[(i + 1) % 4] - centro, escala);
            Vector2 segmento = b - a;
            float t = segmento.sqrMagnitude > 0.000001f
                ? Mathf.Clamp01(-Vector2.Dot(a, segmento) / segmento.sqrMagnitude) : 0f;
            if ((a + segmento * t).sqrMagnitude <= 1f) return true;
            float cruz = a.x * b.y - a.y * b.x;
            positivo |= cruz > 0f; negativo |= cruz < 0f;
        }
        return !positivo || !negativo; // Centro de la elipse dentro de la caja.
    }

    private void OnDrawGizmosSelected()
    {
        if (caja == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0.2f);
        Matrix4x4 anterior = Gizmos.matrix;
        Gizmos.matrix = caja.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(caja.offset, caja.size);
        Gizmos.matrix = anterior;
    }
}
