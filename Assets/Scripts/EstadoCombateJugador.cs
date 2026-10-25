using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class EstadoCombateJugador : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicadorAturdimiento;
    [SerializeField] private ZonaDanoJugador zonaDano;
    public ZonaDanoJugador ZonaDano => zonaDano;
    [SerializeField, Min(0f)] private float invulnerabilidadTrasStun = 0.5f;
    [SerializeField] private Color colorAturdido = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color colorProtegido = new Color(0.4f, 0.9f, 1f, 1f);

    private MovimientoJugador2D movimiento;
    private float aturdimientoRestante;
    private float invulnerabilidadRestante;
    private SpriteRenderer visualPersonaje;
    private LineRenderer[] estrellas;
    private float giroEstrellas;
    private readonly Vector3[] puntosEstrella = new Vector3[10];

    public bool EstaAturdido => aturdimientoRestante > 0f;
    public float AturdimientoRestante => aturdimientoRestante;
    public bool EsInvulnerable => invulnerabilidadRestante > 0f;
    public float InvulnerabilidadRestante => invulnerabilidadRestante;
    public event Action Interrumpido;

    private void Awake()
    {
        movimiento = GetComponent<MovimientoJugador2D>();
        ActualizarIndicador();
    }

    private void Update()
    {
        aturdimientoRestante = Mathf.Max(0f, aturdimientoRestante - Time.deltaTime);
        invulnerabilidadRestante = Mathf.Max(0f, invulnerabilidadRestante - Time.deltaTime);
        ActualizarIndicador();
    }

    public bool RecibirGolpe(float segundos, Vector2 empujon)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || EsInvulnerable)
            return false;

        aturdimientoRestante = Mathf.Max(0f, segundos);
        invulnerabilidadRestante = aturdimientoRestante + Mathf.Max(0f, invulnerabilidadTrasStun);
        Interrumpido?.Invoke();
        if (movimiento != null)
            movimiento.Empujar(empujon);
        ActualizarIndicador();
        return true;
    }

    private void ActualizarIndicador()
    {
        if (indicadorAturdimiento != null)
            indicadorAturdimiento.enabled = false;
        if (estrellas == null) return;
        foreach (LineRenderer estrella in estrellas)
            estrella.enabled = EstaAturdido || EsInvulnerable;
    }

    private void Start()
    {
        Transform visual = transform.Find("Sprite");
        visualPersonaje = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (visualPersonaje == null) return;
        Material material = Resources.Load<Material>("MaterialLatigo");
        if (material == null) return;
        estrellas = new LineRenderer[3];
        for (int i = 0; i < estrellas.Length; i++)
        {
            GameObject objeto = new GameObject("EstrellaStun" + (i + 1), typeof(LineRenderer));
            objeto.layer = gameObject.layer;
            objeto.transform.SetParent(transform, false);
            LineRenderer estrella = objeto.GetComponent<LineRenderer>();
            estrella.sharedMaterial = material;
            estrella.useWorldSpace = true;
            estrella.loop = true;
            estrella.positionCount = puntosEstrella.Length;
            estrella.startWidth = estrella.endWidth = 0.035f;
            estrella.numCornerVertices = 2;
            estrella.sortingLayerID = visualPersonaje.sortingLayerID;
            estrella.sortingOrder = visualPersonaje.sortingOrder + 30;
            estrellas[i] = estrella;
        }
        ActualizarIndicador();
    }

    private void LateUpdate()
    {
        if (estrellas == null || visualPersonaje == null || (!EstaAturdido && !EsInvulnerable)) return;
        // Usar tiempo de juego para que tambien se congelen al pausar.
        giroEstrellas += Time.deltaTime * 4f;
        Bounds cuerpo = visualPersonaje.bounds;
        Vector3 cabeza = new Vector3(cuerpo.center.x, cuerpo.max.y + 0.15f, transform.position.z);
        Color color = EstaAturdido ? colorAturdido : colorProtegido;
        for (int i = 0; i < estrellas.Length; i++)
        {
            float angulo = giroEstrellas + i * Mathf.PI * 2f / estrellas.Length;
            float profundidad = Mathf.Sin(angulo);
            Vector3 centro = cabeza + new Vector3(Mathf.Cos(angulo) * 0.65f, profundidad * 0.18f, 0f);
            float radio = Mathf.Lerp(0.13f, 0.19f, (profundidad + 1f) * 0.5f);
            for (int punto = 0; punto < puntosEstrella.Length; punto++)
            {
                float fase = Mathf.PI * 0.5f + punto * Mathf.PI / 5f + giroEstrellas * 0.4f;
                float distancia = punto % 2 == 0 ? radio : radio * 0.43f;
                puntosEstrella[punto] = centro + new Vector3(Mathf.Cos(fase), Mathf.Sin(fase), 0f) * distancia;
            }
            estrellas[i].SetPositions(puntosEstrella);
            estrellas[i].startColor = estrellas[i].endColor = color;
        }
    }

    private void OnDisable()
    {
        aturdimientoRestante = 0f;
        invulnerabilidadRestante = 0f;
        Interrumpido?.Invoke();
        ActualizarIndicador();
    }
}
