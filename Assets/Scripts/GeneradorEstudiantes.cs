using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class GeneradorEstudiantes : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BoxCollider2D zonaPasillo;
    [SerializeField] private Camera[] camarasJugadores;
    [SerializeField] private EstudiantePasillo[] prefabsEstudiantes;

    [Header("Poblacion")]
    [SerializeField, Min(0)] private int estudiantesIniciales = 10;
    [SerializeField, Min(1)] private int maximoEstudiantes = 16;
    [SerializeField] private Vector2 intervaloAparicion = new Vector2(2f, 4f);
    [SerializeField] private Vector2 rangoVelocidad = new Vector2(1.2f, 2.2f);

    [Header("Espacio")]
    [Tooltip("Distancia extra fuera de cada vista. Incluye el ancho del sprite.")]
    [SerializeField, Min(0f)] private float margenCamaras = 2f;
    [SerializeField, Min(0f)] private float margenSalida = 2f;
    [SerializeField, Min(0f)] private float margenPies = 0.5f;
    [SerializeField, Min(0f)] private float separacionMinima = 1.2f;
    [SerializeField, Min(1)] private int tramos = 6;

    private readonly List<EstudiantePasillo> estudiantes = new List<EstudiantePasillo>();
    private readonly List<Vector2> intervalosOcultos = new List<Vector2>();
    private readonly List<Vector3> candidatos = new List<Vector3>();
    private float proximaAparicion;

    public int CantidadActiva
    {
        get
        {
            estudiantes.RemoveAll(estudiante => estudiante == null);
            return estudiantes.Count;
        }
    }

    private void Start()
    {
        if (!ReferenciasValidas())
        {
            Debug.LogError("Asigna zona, ambas camaras ortograficas y prefabs de estudiantes.", this);
            enabled = false;
            return;
        }

        Bounds zona = zonaPasillo.bounds;
        int cantidad = Mathf.Min(estudiantesIniciales, maximoEstudiantes);
        for (int i = 0; i < cantidad; i++)
        {
            float desde = Mathf.Lerp(zona.min.x, zona.max.x, (float)i / cantidad);
            float hasta = Mathf.Lerp(zona.min.x, zona.max.x, (float)(i + 1) / cantidad);
            IntentarCrear(new Vector2(desde, hasta), zona);
        }

        ProgramarAparicion();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f)
            return;

        if (Time.time < proximaAparicion)
            return;

        ProgramarAparicion();
        if (CantidadActiva >= maximoEstudiantes)
            return;

        Bounds zona = zonaPasillo.bounds;
        CalcularIntervalosOcultos(zona);
        candidatos.Clear();
        int cantidadTramos = Mathf.Max(1, tramos);
        for (int tramo = 0; tramo < cantidadTramos; tramo++)
        {
            float desde = Mathf.Lerp(zona.min.x, zona.max.x, (float)tramo / cantidadTramos);
            float hasta = Mathf.Lerp(zona.min.x, zona.max.x, (float)(tramo + 1) / cantidadTramos);
            int poblacion = 0;
            foreach (EstudiantePasillo estudiante in estudiantes)
            {
                int indice = Mathf.Clamp(Mathf.FloorToInt(
                    Mathf.InverseLerp(zona.min.x, zona.max.x, estudiante.transform.position.x) * cantidadTramos),
                    0, cantidadTramos - 1);
                if (indice == tramo)
                    poblacion++;
            }

            foreach (Vector2 intervalo in intervalosOcultos)
            {
                float inicio = Mathf.Max(desde, intervalo.x);
                float fin = Mathf.Min(hasta, intervalo.y);
                if (fin > inicio)
                    candidatos.Add(new Vector3(inicio, fin, poblacion));
            }
        }

        while (candidatos.Count > 0)
        {
            float menorPoblacion = float.PositiveInfinity;
            foreach (Vector3 candidato in candidatos)
                menorPoblacion = Mathf.Min(menorPoblacion, candidato.z);

            float longitudTotal = 0f;
            foreach (Vector3 candidato in candidatos)
                if (candidato.z == menorPoblacion)
                    longitudTotal += candidato.y - candidato.x;

            float eleccion = Random.value * longitudTotal;
            int elegido = -1;
            for (int i = 0; i < candidatos.Count; i++)
            {
                Vector3 candidato = candidatos[i];
                if (candidato.z != menorPoblacion)
                    continue;

                elegido = i;
                eleccion -= candidato.y - candidato.x;
                if (eleccion <= 0f)
                    break;
            }

            Vector3 seleccionado = candidatos[elegido];
            candidatos.RemoveAt(elegido);
            if (IntentarCrear(new Vector2(seleccionado.x, seleccionado.y), zona))
                return;
        }
    }

    private void ProgramarAparicion()
    {
        float minima = Mathf.Max(0.1f, Mathf.Min(intervaloAparicion.x, intervaloAparicion.y));
        float maxima = Mathf.Max(minima, Mathf.Max(intervaloAparicion.x, intervaloAparicion.y));
        proximaAparicion = Time.time + Random.Range(minima, maxima);
    }

    private bool ReferenciasValidas()
    {
        if (zonaPasillo == null || camarasJugadores == null || camarasJugadores.Length == 0 ||
            prefabsEstudiantes == null || prefabsEstudiantes.Length == 0)
            return false;

        foreach (Camera camara in camarasJugadores)
            if (camara == null || !camara.orthographic)
                return false;

        foreach (EstudiantePasillo prefab in prefabsEstudiantes)
            if (prefab == null)
                return false;

        return true;
    }

    private void CalcularIntervalosOcultos(Bounds zona)
    {
        intervalosOcultos.Clear();
        intervalosOcultos.Add(new Vector2(zona.min.x, zona.max.x));

        foreach (Camera camara in camarasJugadores)
        {
            if (camara == null || !camara.isActiveAndEnabled)
                continue;

            float distancia = Mathf.Abs(zona.center.z - camara.transform.position.z);
            float x1 = camara.ViewportToWorldPoint(new Vector3(0f, 0.5f, distancia)).x;
            float x2 = camara.ViewportToWorldPoint(new Vector3(1f, 0.5f, distancia)).x;
            float izquierda = Mathf.Min(x1, x2) - margenCamaras;
            float derecha = Mathf.Max(x1, x2) + margenCamaras;

            for (int i = intervalosOcultos.Count - 1; i >= 0; i--)
            {
                Vector2 intervalo = intervalosOcultos[i];
                if (derecha <= intervalo.x || izquierda >= intervalo.y)
                    continue;

                intervalosOcultos.RemoveAt(i);
                if (izquierda > intervalo.x)
                    intervalosOcultos.Add(new Vector2(intervalo.x, izquierda));
                if (derecha < intervalo.y)
                    intervalosOcultos.Add(new Vector2(derecha, intervalo.y));
            }
        }
    }

    private bool IntentarCrear(Vector2 intervalo, Bounds zona)
    {
        estudiantes.RemoveAll(estudiante => estudiante == null);
        float margen = Mathf.Min(margenPies, zona.extents.y);
        for (int intento = 0; intento < 20; intento++)
        {
            Vector3 posicion = new Vector3(Random.Range(intervalo.x, intervalo.y),
                Random.Range(zona.min.y + margen, zona.max.y - margen), zona.center.z);
            bool libre = true;
            foreach (EstudiantePasillo estudiante in estudiantes)
            {
                if ((estudiante.transform.position - posicion).sqrMagnitude < separacionMinima * separacionMinima)
                {
                    libre = false;
                    break;
                }
            }
            if (!libre)
                continue;

            EstudiantePasillo prefab = prefabsEstudiantes[Random.Range(0, prefabsEstudiantes.Length)];
            EstudiantePasillo nuevo = Instantiate(prefab, posicion, Quaternion.identity, transform);
            float minima = Mathf.Max(0.1f, Mathf.Min(rangoVelocidad.x, rangoVelocidad.y));
            float maxima = Mathf.Max(minima, Mathf.Max(rangoVelocidad.x, rangoVelocidad.y));
            nuevo.ConfigurarRecorrido(Random.value < 0.5f ? -1f : 1f,
                Random.Range(minima, maxima), zona.min.x - margenSalida, zona.max.x + margenSalida);
            estudiantes.Add(nuevo);
            return true;
        }
        return false;
    }
}
