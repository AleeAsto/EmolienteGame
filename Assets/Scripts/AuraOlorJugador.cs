using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AuraOlorJugador : MonoBehaviour
{
    private static readonly List<AuraOlorJugador> auras = new List<AuraOlorJugador>();
    private CicloPartida partida;
    private float finPerfume;
    private float finBomba;
    private float radioPerfume = 8f;
    private float radioBomba = 8f;
    private float retirada = 1.25f;
    private readonly List<Halo> halos = new List<Halo>();
    private class Halo
    {
        public SpriteRenderer fuente;
        public SpriteRenderer copia;
        public Vector2 desplazamiento;
        public float opacidad;
    }

    private bool EnCurso => isActiveAndEnabled && partida != null && partida.EnCurso && !partida.Terminada;
    public bool TienePerfume => EnCurso && Time.time < finPerfume;
    public bool TieneMalOlor => EnCurso && Time.time < finBomba;

    public void Inicializar(CicloPartida ciclo)
    {
        partida = ciclo;
        if (!auras.Contains(this)) auras.Add(this);
        Limpiar();
    }

    public void Aplicar(bool repeler, float duracion, float radio, float multiplicadorRetirada)
    {
        if (!EnCurso || duracion <= 0f || radio <= 0f) return;
        if (repeler)
        {
            finBomba = Mathf.Max(finBomba, Time.time + duracion);
            radioBomba = radio;
            retirada = Mathf.Max(1f, multiplicadorRetirada);
        }
        else
        {
            finPerfume = Mathf.Max(finPerfume, Time.time + duracion);
            radioPerfume = radio;
        }
    }

    public static bool Resolver(Vector2 posicion, Scene escena, out Vector2 centro,
        out bool repeler, out float multiplicador)
    {
        centro = Vector2.zero; repeler = false; multiplicador = 1f;
        float mejor = float.PositiveInfinity;
        int desempate = int.MaxValue;
        bool encontrado = false;
        for (int i = auras.Count - 1; i >= 0; i--)
        {
            AuraOlorJugador aura = auras[i];
            if (aura == null) { auras.RemoveAt(i); continue; }
            if (aura.gameObject.scene != escena || !aura.EnCurso) continue;
            float distancia = ((Vector2)aura.transform.position - posicion).sqrMagnitude;
            for (int tipo = 0; tipo < 2; tipo++)
            {
                bool bomba = tipo == 1;
                bool activa = bomba ? aura.TieneMalOlor : aura.TienePerfume;
                float radio = bomba ? aura.radioBomba : aura.radioPerfume;
                if (!activa || distancia > radio * radio) continue;
                int id = aura.GetEntityId().GetHashCode();
                bool empate = Mathf.Approximately(distancia, mejor);
                if (encontrado && !(distancia < mejor && !empate) &&
                    !(empate && (bomba && !repeler || bomba == repeler && id < desempate))) continue;
                encontrado = true;
                mejor = distancia; desempate = id;
                centro = aura.transform.position;
                repeler = bomba;
                multiplicador = bomba ? aura.retirada : 1f;
            }
        }
        return encontrado;
    }

    public static bool CalcularPaso(Vector3 posicion, float velocidad, float delta,
        Bounds zona, Scene escena, int semilla, out Vector3 destino)
        => CalcularPaso(posicion, velocidad, delta, zona, escena, semilla, out destino, out _);

    public static bool CalcularPaso(Vector3 posicion, float velocidad, float delta,
        Bounds zona, Scene escena, int semilla, out Vector3 destino, out bool repeler)
    {
        destino = posicion;
        if (!Resolver(posicion, escena, out Vector2 centro, out repeler, out float multiplicador))
            return false;
        Vector2 objetivo;
        if (repeler)
        {
            // Retirarse por el pasillo, sin empujarse contra los bordes verticales.
            float sentido = Mathf.Abs(posicion.x - centro.x) > 0.05f
                ? Mathf.Sign(posicion.x - centro.x) : semilla % 2 == 0 ? 1f : -1f;
            destino.x += sentido * Mathf.Max(0f, velocidad * multiplicador * delta);
            return true; // Permite alcanzar las salidas, sin quedar clavado en el borde de ZoneMov.
        }
        else
        {
            // Distintos puntos de espera dentro del alcance de absorcion, sin apilarse en el centro.
            float angulo = Mathf.Repeat((semilla % 997) * 137.5f, 360f) * Mathf.Deg2Rad;
            objetivo = centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 0.95f;
        }
        Vector2 siguiente = Vector2.MoveTowards(posicion, objetivo, Mathf.Max(0f, velocidad * multiplicador * delta));
        float margenX = Mathf.Min(0.4f, zona.extents.x);
        float margenY = Mathf.Min(0.4f, zona.extents.y);
        destino = new Vector3(Mathf.Clamp(siguiente.x, zona.min.x + margenX, zona.max.x - margenX),
            Mathf.Clamp(siguiente.y, zona.min.y + margenY, zona.max.y - margenY), posicion.z);
        return true;
    }

    private void LateUpdate()
    {
        bool bomba = TieneMalOlor;
        if (!bomba && !TienePerfume) { Ocultar(); return; }
        if (halos.Count == 0) CrearVisual();
        Color color = bomba ? new Color(0.5f, 0.95f, 0.25f) : new Color(1f, 0.45f, 0.75f);
        float pulso = 1f + Mathf.Sin(Time.time * 2f) * 0.06f;
        foreach (Halo halo in halos)
        {
            if (halo.fuente == null || halo.copia == null) continue;
            halo.copia.enabled = halo.fuente.enabled && halo.fuente.gameObject.activeInHierarchy;
            halo.copia.sprite = halo.fuente.sprite;
            halo.copia.flipX = halo.fuente.flipX; halo.copia.flipY = halo.fuente.flipY;
            halo.copia.drawMode = halo.fuente.drawMode; halo.copia.size = halo.fuente.size;
            halo.copia.sortingLayerID = halo.fuente.sortingLayerID;
            halo.copia.sortingOrder = halo.fuente.sortingOrder - 1;
            halo.copia.transform.localPosition = halo.fuente.transform.InverseTransformVector(
                (Vector3)(halo.desplazamiento * pulso));
            Color tinte = color; tinte.a = halo.opacidad * halo.fuente.color.a;
            halo.copia.color = tinte;
        }
    }

    private void CrearVisual()
    {
        Transform modelo = transform.Find("Sprite");
        SpriteRenderer[] fuentes = modelo != null ? modelo.GetComponentsInChildren<SpriteRenderer>(true) :
            GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer fuente in fuentes)
        {
            if (fuente.sprite == null) continue;
            // Varias muestras suaves de la propia silueta forman el halo sin marcar el alcance.
            for (int anillo = 0; anillo < 3; anillo++)
            for (int muestra = 0; muestra < 8; muestra++)
            {
                float angulo = (muestra + anillo * 0.3f) / 8f * Mathf.PI * 2f;
                float radio = anillo == 0 ? 0.06f : anillo == 1 ? 0.15f : 0.27f;
                var visual = new GameObject("HaloSuave", typeof(SpriteRenderer));
                visual.transform.SetParent(fuente.transform, false);
                visual.layer = fuente.gameObject.layer;
                var copia = visual.GetComponent<SpriteRenderer>();
                copia.sharedMaterial = fuente.sharedMaterial;
                halos.Add(new Halo { fuente = fuente, copia = copia,
                    desplazamiento = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio,
                    opacidad = anillo == 0 ? 0.045f : anillo == 1 ? 0.025f : 0.012f });
            }
        }
    }

    private void Ocultar()
    {
        foreach (Halo halo in halos) if (halo.copia != null) halo.copia.enabled = false;
    }

    public void Limpiar() { finPerfume = finBomba = 0f; Ocultar(); }
    private void OnEnable() { if (!auras.Contains(this)) auras.Add(this); }
    private void OnDisable() { auras.Remove(this); Limpiar(); }
    private void OnDestroy()
    {
        auras.Remove(this);
        foreach (Halo halo in halos)
        {
            if (halo.copia == null) continue;
            if (Application.isPlaying) Destroy(halo.copia.gameObject); else DestroyImmediate(halo.copia.gameObject);
        }
        halos.Clear();
    }
}
