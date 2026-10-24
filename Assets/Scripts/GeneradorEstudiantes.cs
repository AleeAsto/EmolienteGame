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
    [SerializeField, Min(0.1f)] private float intervaloAparicion = 2.5f;
    [SerializeField] private Vector2 rangoVelocidad = new Vector2(1.2f, 2.2f);

    [Header("Espacio")]
    [Tooltip("Distancia extra fuera de cada vista. Incluye el ancho del sprite.")]
    [SerializeField, Min(0f)] private float margenCamaras = 2f;
    [SerializeField, Min(0f)] private float margenSalida = 2f;
    [SerializeField, Min(0f)] private float margenPies = 0.5f;
    [SerializeField, Min(0f)] private float separacionMinima = 1.2f;

    private readonly List<EstudiantePasillo> estudiantes = new List<EstudiantePasillo>();
    private readonly List<Vector2> intervalosOcultos = new List<Vector2>();
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
        // Una franja por estudiante reparte la poblacion inicial por todo el pasillo.
        for (int i = 0; i < cantidad; i++)
        {
            float desde = Mathf.Lerp(zona.min.x, zona.max.x, (float)i / cantidad);
            float hasta = Mathf.Lerp(zona.min.x, zona.max.x, (float)(i + 1) / cantidad);
            IntentarCrear(new Vector2(desde, hasta), zona);
        }

        proximaAparicion = Time.time + Mathf.Max(0.1f, intervaloAparicion);
    }

    // Se ejecuta despues de que las camaras hayan seguido a los jugadores.
    private void LateUpdate()
    {
        if (Time.time < proximaAparicion)
            return;

        proximaAparicion = Time.time + Mathf.Max(0.1f, intervaloAparicion);
        if (CantidadActiva >= maximoEstudiantes)
            return;

        Bounds zona = zonaPasillo.bounds;
        CalcularIntervalosOcultos(zona);
        float longitudTotal = 0f;
        foreach (Vector2 intervalo in intervalosOcultos)
            longitudTotal += intervalo.y - intervalo.x;

        if (longitudTotal <= 0f)
            return;

        float eleccion = Random.value * longitudTotal;
        foreach (Vector2 intervalo in intervalosOcultos)
        {
            float longitud = intervalo.y - intervalo.x;
            if (eleccion <= longitud)
            {
                IntentarCrear(intervalo, zona);
                return;
            }
            eleccion -= longitud;
        }
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
