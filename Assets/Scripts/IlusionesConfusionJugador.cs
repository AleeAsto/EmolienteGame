using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Copias visuales sin AlmaEstudiante, collider ni participacion en el generador real.
[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
public class IlusionesConfusionJugador : MonoBehaviour
{
    [SerializeField, Range(4, 48)] private int cantidadFantasmas = 24;
    [SerializeField, Min(0.05f)] private float duracionFadeIn = 0.35f;
    [SerializeField, Min(0.05f)] private float duracionFadeOut = 0.35f;
    private readonly List<Fantasma> fantasmas = new List<Fantasma>();
    private EfectosJugador efectos;
    private Camera camara;
    private BoxCollider2D zona;
    private GeneradorEstudiantes generador;
    private int capa;
    private float proximaReposicion;

    private class Fantasma
    {
        public GameObject objeto;
        public SpriteRenderer[] sprites;
        public Color[] colores;
        public float velocidad;
        public float opacidad;
        public float vida;
        public float duracionVida;
        public bool retirando;
        public bool consumido;
    }

    public int Cantidad => fantasmas.Count;

    public void Inicializar(EfectosJugador estado, Camera vista, BoxCollider2D pasillo,
        GeneradorEstudiantes estudiantes, int capaPrivada)
    {
        efectos = estado; camara = vista; zona = pasillo; generador = estudiantes; capa = capaPrivada;
    }

    private void LateUpdate()
    {
        bool activo = efectos != null && efectos.EstaConfundido;
        if (!activo && fantasmas.Count == 0) return;
        if (Time.timeScale <= 0f || camara == null || zona == null || generador == null) return;
        float mitad = camara.orthographicSize * camara.aspect;
        float izquierda = Mathf.Max(zona.bounds.min.x + 0.4f, camara.transform.position.x - mitad + 0.4f);
        float derecha = Mathf.Min(zona.bounds.max.x - 0.4f, camara.transform.position.x + mitad - 0.4f);
        if (izquierda > derecha) return;
        ActualizarFantasmas(Time.deltaTime, activo, izquierda, derecha);
        if (!activo || Time.time < proximaReposicion) return;
        proximaReposicion = Time.time + 0.6f;
        while (fantasmas.Count < cantidadFantasmas)
        {
            EstudiantePasillo modelo = generador.ModeloVisual;
            if (modelo == null) break;
            Crear(modelo, PosicionAleatoria(izquierda, derecha));
        }
    }

    private void ActualizarFantasmas(float delta, bool activo, float izquierda, float derecha)
    {
        for (int i = fantasmas.Count - 1; i >= 0; i--)
        {
            Fantasma fantasma = fantasmas[i];
            if (fantasma.objeto == null) { fantasmas.RemoveAt(i); continue; }
            Vector3 posicion = fantasma.objeto.transform.position;
            if (!fantasma.consumido && AuraOlorJugador.CalcularPaso(posicion, Mathf.Abs(fantasma.velocidad), delta,
                zona.bounds, gameObject.scene, fantasma.objeto.GetEntityId().GetHashCode(), out Vector3 pasoOlor, out bool repelido))
            {
                if (repelido && Mathf.Abs(pasoOlor.x - posicion.x) > 0.0001f)
                    fantasma.velocidad = Mathf.Abs(fantasma.velocidad) * Mathf.Sign(pasoOlor.x - posicion.x);
                fantasma.objeto.transform.position = pasoOlor;
            }
            else fantasma.objeto.transform.position += Vector3.right * fantasma.velocidad * delta;
            fantasma.vida += delta;
            bool fuera = fantasma.objeto.transform.position.x < izquierda - 2f ||
                fantasma.objeto.transform.position.x > derecha + 2f;
            if (!activo || fantasma.consumido || fuera || fantasma.vida >= fantasma.duracionVida)
                fantasma.retirando = true;
            fantasma.opacidad = Mathf.MoveTowards(fantasma.opacidad, fantasma.retirando ? 0f : 1f,
                delta / Mathf.Max(0.05f, fantasma.retirando ? duracionFadeOut : duracionFadeIn));
            for (int j = 0; j < fantasma.sprites.Length; j++)
            {
                Color color = fantasma.colores[j];
                color.a *= fantasma.opacidad;
                fantasma.sprites[j].color = color;
            }
            if (fantasma.retirando && fantasma.opacidad <= 0f)
            {
                if (activo && !fantasma.consumido)
                {
                    // Reubicar solo cuando es invisible y reaparecer con fade.
                    fantasma.objeto.transform.position = PosicionAleatoria(izquierda, derecha);
                    fantasma.retirando = false;
                    fantasma.vida = 0f;
                    fantasma.duracionVida = Random.Range(4f, 7f);
                }
                else { Destruir(fantasma.objeto); fantasmas.RemoveAt(i); }
            }
        }
    }

    private Vector3 PosicionAleatoria(float izquierda, float derecha)
    {
        float margen = Mathf.Min(0.5f, zona.bounds.extents.y);
        return new Vector3(Random.Range(izquierda, derecha),
            Random.Range(zona.bounds.min.y + margen, zona.bounds.max.y - margen), zona.bounds.center.z);
    }

    private void Crear(EstudiantePasillo modelo, Vector3 posicion)
    {
        var objeto = new GameObject("EstudianteFantasma_" + name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(objeto, gameObject.scene);
        objeto.layer = capa;
        objeto.transform.position = posicion;
        objeto.transform.localScale = modelo.transform.localScale;
        SortingGroup grupo = objeto.AddComponent<SortingGroup>();
        SortingGroup original = modelo.GetComponent<SortingGroup>();
        grupo.sortingLayerID = original != null ? original.sortingLayerID : 0;
        grupo.sortingOrder = original != null ? original.sortingOrder : 10;
        var copias = new List<SpriteRenderer>();
        var colores = new List<Color>();
        foreach (SpriteRenderer fuente in modelo.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (fuente.sprite == null || !fuente.enabled) continue;
            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.layer = capa;
            visual.transform.SetParent(objeto.transform, false);
            visual.transform.localPosition = modelo.transform.InverseTransformPoint(fuente.transform.position);
            visual.transform.localRotation = Quaternion.Inverse(modelo.transform.rotation) * fuente.transform.rotation;
            Vector3 escala = modelo.transform.lossyScale;
            visual.transform.localScale = new Vector3(fuente.transform.lossyScale.x / escala.x,
                fuente.transform.lossyScale.y / escala.y, 1f);
            SpriteRenderer copia = visual.GetComponent<SpriteRenderer>();
            copia.sprite = fuente.sprite;
            copia.sharedMaterial = fuente.sharedMaterial;
            copia.color = fuente.color;
            copia.flipX = fuente.flipX; copia.flipY = fuente.flipY;
            copia.sortingLayerID = fuente.sortingLayerID;
            copia.sortingOrder = fuente.sortingOrder;
            copia.spriteSortPoint = fuente.spriteSortPoint;
            copia.drawMode = fuente.drawMode; copia.size = fuente.size;
            copias.Add(copia); colores.Add(copia.color);
            Color invisible = copia.color; invisible.a = 0f; copia.color = invisible;
        }
        Vector2 rango = generador.RangoVelocidad;
        fantasmas.Add(new Fantasma { objeto = objeto, sprites = copias.ToArray(), colores = colores.ToArray(),
            duracionVida = Random.Range(4f, 7f),
            velocidad = Random.Range(Mathf.Min(rango.x, rango.y), Mathf.Max(rango.x, rango.y)) *
                (Random.value < 0.5f ? -1f : 1f) });
    }

    public bool IntentarDisipar(Vector2 posicion, float distanciaMaximaCuadrada)
    {
        if (efectos == null || !efectos.EstaConfundido) return false;
        Fantasma cercano = null;
        float mejor = distanciaMaximaCuadrada;
        foreach (Fantasma fantasma in fantasmas)
        {
            if (fantasma.objeto == null || fantasma.consumido || fantasma.retirando || fantasma.opacidad < 0.15f) continue;
            float distancia = ((Vector2)fantasma.objeto.transform.position - posicion).sqrMagnitude;
            if (distancia < mejor) { mejor = distancia; cercano = fantasma; }
        }
        if (cercano == null) return false;
        cercano.consumido = true;
        return true;
    }

    public void Limpiar()
    {
        foreach (Fantasma fantasma in fantasmas)
            if (fantasma.objeto != null) Destruir(fantasma.objeto);
        fantasmas.Clear(); proximaReposicion = 0f;
    }

    private static void Destruir(GameObject objeto)
    {
        objeto.SetActive(false);
        if (Application.isPlaying) Destroy(objeto); else DestroyImmediate(objeto);
    }

    private void OnDisable() => Limpiar();
    private void OnDestroy() => Limpiar();
}
