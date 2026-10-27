using System.Collections.Generic;
using UnityEngine;

// Malla pequena del piso para rodear casilleros. No mueve al personaje ni salta colisiones.
public sealed class NavegacionCPU
{
    private readonly List<Bounds> obstaculos = new List<Bounds>();
    private readonly List<int> abiertos = new List<int>();
    private readonly List<Vector3> inversa = new List<Vector3>();
    private readonly Bounds limites;
    private readonly int columnas, filas;
    private readonly float pasoX, pasoY, altura;
    private readonly bool[] libres;
    private readonly float[] costos;
    private readonly int[] padres;
    private readonly bool[] cerrados;

    public NavegacionCPU(MovimientoJugador2D jugador)
    {
        Physics2D.SyncTransforms();
        Bounds pies = jugador.GetComponent<BoxCollider2D>().bounds;
        Vector3 offset = pies.center - jugador.transform.position;
        limites = jugador.ZonaMovimiento.bounds;
        limites.center -= offset;
        limites.size = new Vector3(Mathf.Max(0.1f, limites.size.x - pies.size.x - 0.06f),
            Mathf.Max(0.1f, limites.size.y - pies.size.y - 0.06f), 1f);
        altura = jugador.transform.position.z;
        foreach (Casillero casillero in Object.FindObjectsByType<Casillero>(FindObjectsSortMode.None))
        {
            if (casillero.gameObject.scene != jugador.gameObject.scene) continue;
            var collider = casillero.GetComponent<BoxCollider2D>();
            if (collider == null || !collider.enabled) continue;
            Bounds caja = collider.bounds;
            caja.center -= offset;
            caja.Expand(new Vector3(pies.size.x + 0.08f, pies.size.y + 0.08f, 0f));
            obstaculos.Add(caja);
        }
        columnas = Mathf.Clamp(Mathf.CeilToInt(limites.size.x / 0.65f) + 1, 2, 240);
        filas = Mathf.Clamp(Mathf.CeilToInt(limites.size.y / 0.65f) + 1, 2, 80);
        pasoX = limites.size.x / (columnas - 1);
        pasoY = limites.size.y / (filas - 1);
        int cantidad = columnas * filas;
        libres = new bool[cantidad]; costos = new float[cantidad];
        padres = new int[cantidad]; cerrados = new bool[cantidad];
        for (int i = 0; i < cantidad; i++) libres[i] = !Ocupado(Punto(i));
    }

    public Vector3 Limitar(Vector3 punto) => new Vector3(Mathf.Clamp(punto.x, limites.min.x, limites.max.x),
        Mathf.Clamp(punto.y, limites.min.y, limites.max.y), altura);

    public bool CalcularRuta(Vector3 origen, Vector3 destino, List<Vector3> ruta)
    {
        ruta.Clear();
        destino = Limitar(destino);
        if (LineaLibre(origen, destino)) { ruta.Add(destino); return true; }
        int inicio = Cercano(origen), fin = Cercano(destino);
        if (inicio < 0 || fin < 0) return false;
        abiertos.Clear();
        for (int i = 0; i < costos.Length; i++)
        { costos[i] = float.PositiveInfinity; padres[i] = -1; cerrados[i] = false; }
        costos[inicio] = 0f; abiertos.Add(inicio);
        while (abiertos.Count > 0)
        {
            int mejor = 0;
            float mejorCosto = float.PositiveInfinity;
            for (int i = 0; i < abiertos.Count; i++)
            {
                int indice = abiertos[i];
                float costo = costos[indice] + Vector2.Distance(Punto(indice), Punto(fin));
                if (costo < mejorCosto) { mejor = i; mejorCosto = costo; }
            }
            int actual = abiertos[mejor]; abiertos.RemoveAt(mejor);
            if (actual == fin)
            {
                inversa.Clear();
                for (int nodo = fin; nodo >= 0; nodo = padres[nodo]) inversa.Add(Punto(nodo));
                Vector3 desde = origen;
                for (int i = inversa.Count - 1; i >= 0;)
                {
                    int siguiente = i;
                    while (siguiente > 0 && LineaLibre(desde, inversa[siguiente - 1])) siguiente--;
                    ruta.Add(inversa[siguiente]); desde = inversa[siguiente]; i = siguiente - 1;
                }
                if (LineaLibre(desde, destino)) ruta.Add(destino);
                return ruta.Count > 0;
            }
            cerrados[actual] = true;
            int x = actual % columnas, y = actual / columnas;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if ((dx == 0 && dy == 0) || nx < 0 || nx >= columnas || ny < 0 || ny >= filas) continue;
                int vecino = ny * columnas + nx;
                if (!libres[vecino] || cerrados[vecino] || !LineaLibre(Punto(actual), Punto(vecino))) continue;
                float costo = costos[actual] + Vector2.Distance(Punto(actual), Punto(vecino));
                if (costo >= costos[vecino]) continue;
                padres[vecino] = actual; costos[vecino] = costo;
                if (!abiertos.Contains(vecino)) abiertos.Add(vecino);
            }
        }
        return false;
    }

    public bool LineaLibre(Vector3 desde, Vector3 hasta)
    {
        foreach (Bounds caja in obstaculos)
        {
            // Interseccion segmento/AABB en el piso, incluyendo ambos extremos.
            float entrada = 0f, salida = 1f;
            Vector3 delta = hasta - desde;
            if (SolapaEje(desde.x, delta.x, caja.min.x, caja.max.x, ref entrada, ref salida) &&
                SolapaEje(desde.y, delta.y, caja.min.y, caja.max.y, ref entrada, ref salida)) return false;
        }
        return true;
    }

    public static float Longitud(Vector3 origen, List<Vector3> ruta)
    {
        float longitud = 0f;
        foreach (Vector3 punto in ruta) { longitud += Vector2.Distance(origen, punto); origen = punto; }
        return longitud;
    }

    private int Cercano(Vector3 punto)
    {
        int mejor = -1;
        float distancia = float.PositiveInfinity;
        for (int i = 0; i < libres.Length; i++)
        {
            if (!libres[i]) continue;
            float d = ((Vector2)(Punto(i) - punto)).sqrMagnitude;
            if (d < distancia && LineaLibre(punto, Punto(i))) { distancia = d; mejor = i; }
        }
        // Un destino dentro del mueble se aproxima por su nodo accesible mas cercano.
        if (mejor < 0)
            for (int i = 0; i < libres.Length; i++)
            {
                float d = ((Vector2)(Punto(i) - punto)).sqrMagnitude;
                if (libres[i] && d < distancia) { distancia = d; mejor = i; }
            }
        return mejor;
    }

    private Vector3 Punto(int i) => new Vector3(limites.min.x + i % columnas * pasoX,
        limites.min.y + i / columnas * pasoY, altura);
    private bool Ocupado(Vector3 punto)
    {
        foreach (Bounds caja in obstaculos)
            if (punto.x >= caja.min.x && punto.x <= caja.max.x && punto.y >= caja.min.y && punto.y <= caja.max.y) return true;
        return false;
    }

    private static bool SolapaEje(float origen, float delta, float minimo, float maximo, ref float entrada, ref float salida)
    {
        if (Mathf.Abs(delta) < 0.00001f) return origen >= minimo && origen <= maximo;
        float a = (minimo - origen) / delta, b = (maximo - origen) / delta;
        entrada = Mathf.Max(entrada, Mathf.Min(a, b)); salida = Mathf.Min(salida, Mathf.Max(a, b));
        return entrada <= salida;
    }
}
