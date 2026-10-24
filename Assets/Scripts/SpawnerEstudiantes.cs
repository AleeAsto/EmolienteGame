using System.Collections.Generic;
using UnityEngine;

public class SpawnerEstudiantes : MonoBehaviour
{
    [SerializeField] private EstudianteNPC prefabEstudiante;
    [SerializeField] private BoxCollider2D zonaPasillo;
    [SerializeField] private Camera[] camaras = new Camera[2];
    [SerializeField, Min(0)] private int cantidadInicial = 12;
    [SerializeField, Min(1)] private int maximoEstudiantes = 24;
    [SerializeField] private Vector2 intervaloSpawn = new Vector2(2f, 4f);
    [SerializeField] private Vector2 velocidadEstudiantes = new Vector2(1f, 2f);
    [SerializeField, Min(0f)] private float margenCamara = 2f;
    [SerializeField, Min(0f)] private float separacionMinima = 1f;
    [SerializeField, Min(1)] private int tramos = 6;

    private readonly List<EstudianteNPC> estudiantes = new List<EstudianteNPC>();
    private float tiempoRestante;
    private Bounds formaEstudiante;

    private void Start()
    {
        if (prefabEstudiante == null || zonaPasillo == null || camaras == null || camaras.Length == 0)
        {
            Debug.LogError("Asigna Prefab Estudiante, Zona Pasillo y ambas Camaras.", this);
            enabled = false;
            return;
        }

        foreach (Camera camara in camaras)
        {
            if (camara == null)
            {
                Debug.LogError("Asigna todas las Camaras del spawner.", this);
                enabled = false;
                return;
            }
        }

        SpriteRenderer sprite = prefabEstudiante.GetComponent<SpriteRenderer>();
        formaEstudiante = sprite.bounds;
        formaEstudiante.center -= prefabEstudiante.transform.position;

        for (int i = 0; i < Mathf.Min(cantidadInicial, maximoEstudiantes); i++)
            IntentarCrear(true);

        ProgramarSpawn();
    }

    private void LateUpdate()
    {
        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante > 0f)
            return;

        estudiantes.RemoveAll(estudiante => estudiante == null);
        if (estudiantes.Count < maximoEstudiantes)
            IntentarCrear(false);

        ProgramarSpawn();
    }

    private void ProgramarSpawn()
    {
        float minimo = Mathf.Max(0.1f, Mathf.Min(intervaloSpawn.x, intervaloSpawn.y));
        float maximo = Mathf.Max(minimo, Mathf.Max(intervaloSpawn.x, intervaloSpawn.y));
        tiempoRestante = Random.Range(minimo, maximo);
    }

    private void IntentarCrear(bool inicial)
    {
        Bounds zona = zonaPasillo.bounds;
        float minX = zona.min.x;
        float maxX = zona.max.x;
        float minY = zona.min.y;
        float maxY = zona.max.y;
        if (minX > maxX || minY > maxY)
            return;

        Vector3 elegida = Vector3.zero;
        int menorPoblacion = int.MaxValue;
        bool encontrada = false;

        for (int intento = 0; intento < 40; intento++)
        {
            Vector3 candidata = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY),
                zonaPasillo.transform.position.z);
            if ((!inicial && EsVisible(candidata)) || EstaOcupada(candidata))
                continue;

            int tramo = ObtenerTramo(candidata.x, zona);
            int poblacion = 0;
            foreach (EstudianteNPC estudiante in estudiantes)
            {
                if (estudiante != null && ObtenerTramo(estudiante.transform.position.x, zona) == tramo)
                    poblacion++;
            }

            if (poblacion < menorPoblacion)
            {
                menorPoblacion = poblacion;
                elegida = candidata;
                encontrada = true;
            }
        }

        if (!encontrada)
            return;

        int direccion = Random.value < 0.5f ? -1 : 1;
        float minima = Mathf.Max(0.01f, Mathf.Min(velocidadEstudiantes.x, velocidadEstudiantes.y));
        float maxima = Mathf.Max(minima, Mathf.Max(velocidadEstudiantes.x, velocidadEstudiantes.y));
        EstudianteNPC nuevo = Instantiate(prefabEstudiante, elegida, prefabEstudiante.transform.rotation);
        nuevo.Inicializar(direccion, Random.Range(minima, maxima), direccion > 0 ? zona.max.x : zona.min.x);
        estudiantes.Add(nuevo);
    }

    private int ObtenerTramo(float x, Bounds zona)
    {
        int cantidad = Mathf.Max(1, tramos);
        return Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(zona.min.x, zona.max.x, x) * cantidad),
            0, cantidad - 1);
    }

    private bool EstaOcupada(Vector3 posicion)
    {
        foreach (EstudianteNPC estudiante in estudiantes)
        {
            if (estudiante != null && Vector2.Distance(posicion, estudiante.transform.position) < separacionMinima)
                return true;
        }
        return false;
    }

    private bool EsVisible(Vector3 posicion)
    {
        Bounds limites = new Bounds(posicion + formaEstudiante.center, formaEstudiante.size);
        limites.Expand(Mathf.Max(0f, margenCamara) * 2f);
        foreach (Camera camara in camaras)
        {
            if (camara.isActiveAndEnabled &&
                GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camara), limites))
                return true;
        }
        return false;
    }
}
