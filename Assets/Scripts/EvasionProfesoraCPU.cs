using System.Collections.Generic;
using UnityEngine;

// Compara trayectorias en el piso con el contacto real de la profesora.
// Solo propone movimiento; el jugador conserva sus limites y colisiones.
public sealed class EvasionProfesoraCPU
{
    private readonly MovimientoJugador2D movimiento;
    private readonly EstadoCombateJugador estado;
    private readonly NavegacionCPU navegacion;
    private readonly List<Vector3> ruta = new List<Vector3>();
    private float horizonteActual;
    private const float Margen = 0.18f;

    public EvasionProfesoraCPU(MovimientoJugador2D jugador, EstadoCombateJugador combate, NavegacionCPU mapa)
    { movimiento = jugador; estado = combate; navegacion = mapa; }

    public bool Intentar(ProfesoraPasillo[] profesoras, Vector2 intencion, float anticipacion, out Vector3 seguro)
    {
        seguro = movimiento.transform.position;
        horizonteActual = Mathf.Max(0.5f, anticipacion);
        Bounds propia = estado.ZonaDano.Limites;
        Vector2 velocidad = Vector2.ClampMagnitude(intencion, 1f) * movimiento.VelocidadActual;
        ProfesoraPasillo amenaza = null;
        float primerContacto = float.PositiveInfinity;
        foreach (var profesora in profesoras)
        {
            if (!Activa(profesora)) continue;
            Bounds area = profesora.AreaContacto;
            area.Expand(new Vector3(Margen * 2f, Margen * 2f, 0f));
            if (Contacto(propia, velocidad, area, profesora.VelocidadRecorrido, anticipacion, out float tiempo) &&
                tiempo < primerContacto)
            { amenaza = profesora; primerContacto = tiempo; }
        }
        if (amenaza == null) return false;

        Bounds peligro = amenaza.AreaContacto;
        Vector3 origen = movimiento.transform.position;
        float offsetY = propia.center.y - origen.y;
        float mejor = float.PositiveInfinity;
        // Si ya esta fuera del carril, puede esperar a que el cruce quede libre.
        bool fuera = Mathf.Abs(propia.center.y - peligro.center.y) > propia.extents.y + peligro.extents.y + Margen;
        if (fuera) Evaluar(origen, profesoras, propia, ref mejor, ref seguro);
        Evaluar(new Vector3(origen.x, peligro.min.y - propia.extents.y - Margen - offsetY, origen.z),
            profesoras, propia, ref mejor, ref seguro);
        Evaluar(new Vector3(origen.x, peligro.max.y + propia.extents.y + Margen - offsetY, origen.z),
            profesoras, propia, ref mejor, ref seguro);
        // Si un mueble o un extremo impide salir verticalmente, valorar retroceder.
        if (mejor >= 100f)
        {
            float separacion = Mathf.Max(3f, movimiento.VelocidadActual * 0.6f);
            Evaluar(origen + Vector3.left * separacion, profesoras, propia, ref mejor, ref seguro);
            Evaluar(origen + Vector3.right * separacion, profesoras, propia, ref mejor, ref seguro);
        }
        return true;
    }

    public bool TrayectoPeligroso(ProfesoraPasillo[] profesoras, Vector3 destino, float duracion)
    {
        Bounds propia = estado.ZonaDano.Limites;
        Vector2 velocidad = (destino - movimiento.transform.position) / Mathf.Max(0.01f, duracion);
        foreach (var profesora in profesoras)
        {
            if (!Activa(profesora)) continue;
            Bounds area = profesora.AreaContacto;
            area.Expand(new Vector3(Margen * 2f, Margen * 2f, 0f));
            if (Contacto(propia, velocidad, area, profesora.VelocidadRecorrido, duracion, out _)) return true;
        }
        return false;
    }

    public bool DestinoPeligroso(ProfesoraPasillo[] profesoras, Vector3 destino, float dentroDe)
    {
        Bounds propia = estado.ZonaDano.Limites;
        propia.center += destino - movimiento.transform.position;
        foreach (var profesora in profesoras)
        {
            if (!Activa(profesora)) continue;
            Bounds area = profesora.AreaContacto;
            area.center += (Vector3)profesora.VelocidadRecorrido * Mathf.Max(0f, dentroDe);
            area.Expand(new Vector3(Margen * 2f, Margen * 2f, 0f));
            if (Contacto(propia, Vector2.zero, area, profesora.VelocidadRecorrido, 0.4f, out _)) return true;
        }
        return false;
    }

    private void Evaluar(Vector3 candidato, ProfesoraPasillo[] profesoras, Bounds propia, ref float mejor, ref Vector3 seguro)
    {
        if (!navegacion.CalcularRuta(movimiento.transform.position, candidato, ruta) || ruta.Count == 0) return;
        Vector3 fin = ruta[ruta.Count - 1];
        float duracion = NavegacionCPU.Longitud(movimiento.transform.position, ruta) / Mathf.Max(0.1f, movimiento.VelocidadActual);
        bool peligro = RutaPeligrosa(profesoras, propia);
        float valor = duracion + (peligro ? 100f : 0f);
        if (valor < mejor) { mejor = valor; seguro = fin; }
    }

    private bool RutaPeligrosa(ProfesoraPasillo[] profesoras, Bounds propia)
    {
        Vector3 desde = movimiento.transform.position;
        float transcurrido = 0f;
        float rapidez = Mathf.Max(0.1f, movimiento.VelocidadActual);
        foreach (Vector3 punto in ruta)
        {
            float duracion = Vector2.Distance(desde, punto) / rapidez;
            Vector2 velocidad = duracion > 0.001f ? (Vector2)(punto - desde) / duracion : Vector2.zero;
            foreach (var profesora in profesoras)
            {
                if (!Activa(profesora)) continue;
                Bounds area = profesora.AreaContacto;
                area.center += (Vector3)profesora.VelocidadRecorrido * transcurrido;
                area.Expand(new Vector3(Margen, Margen, 0f));
                if (Contacto(propia, velocidad, area, profesora.VelocidadRecorrido, duracion, out _)) return true;
            }
            propia.center += punto - desde;
            desde = punto; transcurrido += duracion;
        }
        foreach (var profesora in profesoras)
        {
            if (!Activa(profesora)) continue;
            Bounds area = profesora.AreaContacto;
            area.center += (Vector3)profesora.VelocidadRecorrido * transcurrido;
            area.Expand(new Vector3(Margen, Margen, 0f));
            if (Contacto(propia, Vector2.zero, area, profesora.VelocidadRecorrido, horizonteActual, out _)) return true;
        }
        return false;
    }

    private bool Activa(ProfesoraPasillo profesora) => profesora != null && profesora.EnRecorrido &&
        profesora.gameObject.scene == movimiento.gameObject.scene;

    // Barrido de dos rectangulos en movimiento, con velocidad relativa.
    public static bool Contacto(Bounds a, Vector2 velocidadA, Bounds b, Vector2 velocidadB, float horizonte, out float tiempo)
    {
        Vector2 separacion = a.center - b.center;
        Vector2 margen = a.extents + b.extents;
        Vector2 velocidad = velocidadA - velocidadB;
        float entrada = 0f, salida = Mathf.Max(0f, horizonte);
        bool coincide = Eje(separacion.x, velocidad.x, margen.x, ref entrada, ref salida) &&
            Eje(separacion.y, velocidad.y, margen.y, ref entrada, ref salida);
        tiempo = entrada;
        return coincide && entrada <= salida;
    }

    private static bool Eje(float distancia, float velocidad, float margen, ref float entrada, ref float salida)
    {
        if (Mathf.Abs(velocidad) < 0.00001f) return Mathf.Abs(distancia) <= margen;
        float a = (-margen - distancia) / velocidad;
        float b = (margen - distancia) / velocidad;
        entrada = Mathf.Max(entrada, Mathf.Min(a, b)); salida = Mathf.Min(salida, Mathf.Max(a, b));
        return entrada <= salida;
    }
}
