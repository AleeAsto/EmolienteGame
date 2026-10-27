using System.Collections.Generic;
using UnityEngine;

// Decide intenciones. Movimiento, impactos, drenaje, cartas y puertas conservan sus reglas.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-175)]
public sealed class ControladorCPU : MonoBehaviour
{
    private enum Tarea { Esperar, Buscar, Absorber, Interrumpir, Combatir, Escapar, Viajar, Explorar }
    private CicloPartida partida;
    private AlmasJugador almas, rival;
    private MovimientoJugador2D movimiento, movimientoRival;
    private AtaqueJugador ataque, ataqueRival;
    private SuccionJugador succion, succionRival;
    private EstadoCombateJugador estado, estadoRival;
    private ManoCartasJugador mano;
    private EfectosJugador efectos;
    private UsarPuertaJugador viajero;
    private EntradaJugador entrada;
    private ProfesoraPasillo[] profesoras;
    private PuertaTeletransporte[] puertas;
    private Camera[] camaras;
    private NavegacionCPU navegacion;
    private EvasionProfesoraCPU evasionProfesora;
    private readonly List<Vector3> ruta = new List<Vector3>();
    private readonly List<Vector3> rutaTemporal = new List<Vector3>();
    private readonly List<Transform> senuelos = new List<Transform>();
    private Tarea tarea;
    private Transform objetivo;
    private Vector3 destino;
    private float siguienteDecision, preparadoParaGolpear, siguienteCarta, confirmarCarta, preparadaSuccion;
    private bool reaccionandoGolpe, preparandoSuccion;
    private float direccionExploracion = -1f;
    private float desdeSinAvance;
    private Vector3 ultimaPosicion;
    private int nodoRuta;
    private Vector3 posicionRivalAnterior;
    private Vector2 velocidadRivalObservada;
    private float pasoMovimientoRivalAnterior;
    private float compromisoCombateHasta, evadirHasta;

    public DificultadCPU Dificultad { get; private set; }
    public string DecisionActual => tarea.ToString();
    public Transform ObjetivoActual => objetivo;
    public float UltimoTiempoIntercepcion { get; private set; }
    public int GolpesIntentados { get; private set; }
    public int CartasUsadas { get; private set; }
    public Vector2 VelocidadRivalObservada => velocidadRivalObservada;
    private bool Experta => Dificultad == DificultadCPU.Dificil;
    private float Intervalo => Experta ? 0.09f : Dificultad == DificultadCPU.Normal ? 0.24f : 0.65f;
    private float Reaccion => Experta ? 0.08f : Dificultad == DificultadCPU.Normal ? 0.25f : 0.7f;
    private float Rapidez => Mathf.Max(0.1f, movimiento.VelocidadActual);

    public void Inicializar(CicloPartida ciclo, AlmasJugador oponente, DificultadCPU dificultad)
    {
        partida = ciclo; rival = oponente; Dificultad = dificultad;
        entrada = EntradaJugador.Obtener(this);
        entrada.ActivarCPU(true);
    }

    private void Start()
    {
        almas = GetComponent<AlmasJugador>(); movimiento = GetComponent<MovimientoJugador2D>();
        ataque = GetComponent<AtaqueJugador>(); succion = GetComponent<SuccionJugador>();
        estado = GetComponent<EstadoCombateJugador>(); mano = GetComponent<ManoCartasJugador>();
        efectos = GetComponent<EfectosJugador>(); viajero = GetComponent<UsarPuertaJugador>();
        if (partida == null || rival == null || movimiento == null || movimiento.ZonaMovimiento == null)
        { enabled = false; return; }
        movimientoRival = rival.GetComponent<MovimientoJugador2D>(); ataqueRival = rival.GetComponent<AtaqueJugador>();
        succionRival = rival.GetComponent<SuccionJugador>(); estadoRival = rival.GetComponent<EstadoCombateJugador>();
        profesoras = FindObjectsByType<ProfesoraPasillo>(FindObjectsSortMode.None);
        puertas = FindObjectsByType<PuertaTeletransporte>(FindObjectsSortMode.None);
        camaras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        navegacion = new NavegacionCPU(movimiento);
        evasionProfesora = new EvasionProfesoraCPU(movimiento, estado, navegacion);
        ultimaPosicion = transform.position;
        posicionRivalAnterior = rival.transform.position;
        pasoMovimientoRivalAnterior = Time.deltaTime;
    }

    private void Update()
    {
        if (entrada == null) return;
        Vector2 intencionAnterior = entrada.MovimientoCPU;
        ObservarMovimientoRival();
        entrada.Limpiar();
        if (navegacion == null || partida == null || !partida.EnCurso || partida.Terminada || Time.timeScale <= 0f ||
            estado.EstaAturdido || movimiento.EnPuerta)
        {
            reaccionandoGolpe = false;
            if (mano != null) mano.Cerrar();
            return;
        }
        if (!movimiento.EstaEnDash && EsquivarProfesora(intencionAnterior)) return;
        if (mano.MenuAbierto)
        {
            if (efectos.EstaConfundido) { mano.Cerrar(); return; }
            if (Time.time >= confirmarCarta)
            {
                if (mano.IntentarUsar()) CartasUsadas++;
                else mano.Cerrar();
                siguienteCarta = Time.time + 2f;
                siguienteDecision = 0f;
            }
            return;
        }
        // El componente de succion decide cuando debe soltarse el boton, incluso
        // si el NPC fue destruido o el intento consumio un fantasma.
        if (succion.RequiereSoltar)
        {
            preparandoSuccion = false; siguienteDecision = 0f;
            return;
        }
        if (MantenerAbsorcion()) return;
        if (Time.time >= siguienteDecision)
        {
            siguienteDecision = Time.time + Intervalo;
            Decidir();
        }
        Ejecutar();
        if (!movimiento.EstaEnDash) EsquivarProfesora(entrada.MovimientoCPU);
    }

    private void Decidir()
    {
        Vector3 posicion = transform.position;
        float separacion = Vector2.Distance(movimiento.PosicionPies, movimientoRival.PosicionPies);
        // Un ataque en curso es una amenaza, no una invitacion a entrar en su alcance.
        if (Experta && !estado.EsInvulnerable && ataqueRival.EstaAtacando && ataqueRival.EnAlcance(ataque) &&
            ataque.RecargaRestante > Reaccion)
        {
            evadirHasta = Time.time + 0.18f;
            Establecer(Tarea.Escapar, null, PuntoDeEscape());
            return;
        }
        if (Experta && tarea == Tarea.Escapar && Time.time < evadirHasta) return;

        // El intercambio se resuelve al terminar su presentacion. No iniciar
        // una persecucion o un drenaje que el propio intercambio cancelara.
        if (Experta && mano.TieneCartaEnActivacion(EfectoCarta.IntercambioPosicion))
        { Establecer(Tarea.Esperar, null, posicion); return; }

        // Mantener la iniciativa aunque el rival tambien tenga su golpe preparado.
        // El ataque sigue resolviendo los choques y el stun normales del juego.
        bool combate = tarea == Tarea.Combatir || tarea == Tarea.Interrumpir;
        bool golpeCercano = separacion < ataque.AlcanceNormal + 0.6f &&
            Mathf.Abs(estadoRival.ZonaDano.Centro.y - movimiento.PosicionPies.y) < ataque.ToleranciaY + 0.3f;
        if (Experta && !movimientoRival.EnPuerta &&
            (!estadoRival.EsInvulnerable || estadoRival.InvulnerabilidadRestante < 0.65f) &&
            ((golpeCercano && ataque.RecargaRestante <= Reaccion) ||
             (combate && objetivo == rival.transform && Time.time < compromisoCombateHasta && PuedePerseguir())))
        {
            Establecer(succionRival.Objetivo != null ? Tarea.Interrumpir : Tarea.Combatir,
                rival.transform, PuntoParaGolpear());
            return;
        }

        // No perder una interrupcion corta por detenerse a abrir una carta.
        bool interrupcionUrgente = Experta && succionRival.Objetivo != null &&
            !estadoRival.EsInvulnerable && separacion < ataque.AlcanceNormal + Rapidez * 1.2f &&
            Mathf.Max(0f, separacion - ataque.AlcanceNormal * 0.7f) / Rapidez + Reaccion + 0.15f <
                succionRival.TiempoRestanteDrenaje && succionRival.TiempoRestanteDrenaje < 1.6f;
        if (interrupcionUrgente)
        { Establecer(Tarea.Interrumpir, rival.transform, PuntoParaGolpear()); return; }

        if (Time.time >= siguienteCarta && IntentarCarta(separacion)) return;

        if (ataque.TieneLatigo && !estadoRival.EsInvulnerable && !movimientoRival.EnPuerta)
        {
            var marca = rival.GetComponent<MarcaLatigoJugador>();
            float llegada = Mathf.Max(0f, separacion - ataque.AlcanceNormal) / Rapidez;
            if (marca != null && llegada + Reaccion + 0.3f < marca.SegundosRestantes)
            { Establecer(Tarea.Combatir, rival.transform, PuntoParaGolpear()); return; }
        }

        Transform mejor = null;
        float mejorValor = 0f;
        Vector3 mejorDestino = posicion;
        bool confundida = efectos.EstaConfundido;
        foreach (AlmaEstudiante estudiante in FindObjectsByType<AlmaEstudiante>(FindObjectsSortMode.None))
        {
            if (!estudiante.Disponible || estudiante.EstaOcupado || estudiante.gameObject.scene != gameObject.scene ||
                !Visible(estudiante.transform.position)) continue;
            Vector3 punto = DestinoEstudiante(estudiante.transform);
            float viaje = Vector2.Distance(posicion, punto) / Rapidez;
            float duracion = confundida ? 3f : succion.DuracionPara(estudiante);
            if (viaje + duracion > partida.TiempoRestante + 0.3f) continue;
            float premio = confundida ? 10f : estudiante.ValorAlmas;
            if (!confundida && estudiante.EsRaro && mano.EspaciosOcupados < ManoCartasJugador.Capacidad)
                premio += Experta ? 16f : 9f;
            float valor = Dificultad == DificultadCPU.Facil ? 8f / (viaje + 1f) :
                (premio + duracion * 2f) / (viaje + duracion + 0.3f);
            if (objetivo == estudiante.transform) valor *= 1.2f;
            if (Dificultad == DificultadCPU.Facil) valor *= Random.Range(0.8f, 1.2f);
            if (Dificultad != DificultadCPU.Facil && !estadoRival.EstaAturdido &&
                Vector2.Distance(punto, rival.transform.position) < ataqueRival.AlcanceNormal + 1f && !estado.EsInvulnerable)
                valor *= Experta ? 0.55f : 0.8f;
            if (valor > mejorValor) { mejor = estudiante.transform; mejorValor = valor; mejorDestino = punto; }
        }
        if (confundida)
        {
            senuelos.Clear(); GetComponent<IlusionesConfusionJugador>()?.CopiarObjetivosVisibles(senuelos);
            foreach (Transform falso in senuelos)
            {
                float viaje = Vector2.Distance(posicion, falso.position) / Rapidez;
                float valor = 16f / (viaje + 3.3f);
                if (objetivo == falso) valor *= 1.2f;
                if (valor > mejorValor) { mejor = falso; mejorValor = valor; mejorDestino = DestinoEstudiante(falso); }
            }
        }

        if ((!estadoRival.EsInvulnerable || Experta && estadoRival.InvulnerabilidadRestante < 1.2f) && !movimientoRival.EnPuerta)
        {
            Vector3 intercepcion = PuntoParaGolpear();
            bool rutaPosible = navegacion.CalcularRuta(posicion, intercepcion, rutaTemporal);
            float viaje = rutaPosible ? NavegacionCPU.Longitud(posicion, rutaTemporal) / Rapidez : float.PositiveInfinity;
            float recargaReal = ataque.RecargaRestante / Mathf.Max(0.1f, efectos.MultiplicadorVelocidad);
            UltimoTiempoIntercepcion = Mathf.Max(viaje, Reaccion, recargaReal,
                estadoRival.InvulnerabilidadRestante) + ataque.PreparacionSegundos;
            if (succionRival.Objetivo != null)
            {
                float margen = Experta ? 0.15f : 0.4f;
                float valorInterrupcion = (succionRival.Objetivo.EsRaro ? 20f : 13f) / (viaje + 1.2f);
                if (Experta && almas.Cantidad < rival.Cantidad) valorInterrupcion *= 1.35f;
                if ((Dificultad != DificultadCPU.Facil || separacion < 4f) &&
                    UltimoTiempoIntercepcion + margen < succionRival.TiempoRestanteDrenaje && valorInterrupcion > mejorValor)
                { Establecer(Tarea.Interrumpir, rival.transform, intercepcion); return; }
            }
            // Castigar oportunidades cercanas; no perseguir indefinidamente a un rival sin premio.
            bool vulnerable = ataqueRival.RecargaRestante > Reaccion || estado.EsInvulnerable;
            bool presionar = Experta && PuedePerseguir() && (separacion < 7f ||
                almas.Cantidad < rival.Cantidad && partida.TiempoRestante < 25f && separacion < 14f);
            if (Dificultad != DificultadCPU.Facil &&
                (separacion < ataque.AlcanceNormal + 0.65f && vulnerable || presionar) &&
                ataque.RecargaRestante <= Reaccion && (mejorValor < 6f || Experta && rival.Cantidad > almas.Cantidad))
            { Establecer(Tarea.Combatir, rival.transform, intercepcion); return; }
        }
        if (mejor != null)
        {
            if (Experta && IntentarAtajo(mejorDestino)) return;
            Establecer(Tarea.Buscar, mejor, mejorDestino); return;
        }
        if (tarea == Tarea.Explorar && Vector2.Distance(posicion, destino) > 1f) return;
        Bounds piso = movimiento.ZonaMovimiento.bounds;
        if (posicion.x <= piso.min.x + 3f) direccionExploracion = 1f;
        else if (posicion.x >= piso.max.x - 3f) direccionExploracion = -1f;
        Vector3 explorar = posicion + Vector3.right * direccionExploracion * 12f;
        explorar.y = piso.center.y - 0.5f;
        Establecer(Tarea.Explorar, null, explorar);
    }

    private void Ejecutar()
    {
        if (tarea == Tarea.Esperar || mano.MenuAbierto || ataque.EstaAtacando || movimiento.EstaEnDash) return;
        if (tarea == Tarea.Absorber && succion.Objetivo != null)
        { entrada.Ordenar(Vector2.zero, true); return; }
        if (tarea == Tarea.Buscar)
        {
            if (objetivo == null) { siguienteDecision = 0f; return; }
            if (succion.EnAlcance(objetivo.position))
            {
                if (!preparandoSuccion) { preparandoSuccion = true; preparadaSuccion = Time.time + Reaccion; }
                // Reaccionar mientras se acerca y capturar con margen. Detenerse
                // en el borde reiniciaba el intento cada vez que el NPC avanzaba.
                if (Time.time >= preparadaSuccion && succion.EnAlcance(objetivo.position, 0.3f))
                {
                    var alma = objetivo.GetComponent<AlmaEstudiante>();
                    if (EsquivarProfesora(Vector2.zero, succion.DuracionPara(alma) + 0.1f)) return;
                    movimiento.MirarHacia(objetivo.position.x - movimiento.PosicionPies.x);
                    entrada.Ordenar(Vector2.zero, true);
                    return;
                }
            }
        }
        if (tarea == Tarea.Interrumpir || tarea == Tarea.Combatir)
        {
            if (movimientoRival.EnPuerta)
            { reaccionandoGolpe = false; siguienteDecision = 0f; return; }
            movimiento.MirarHacia(estadoRival.ZonaDano.Centro.x - movimiento.PosicionPies.x);
            bool enAlcance = ataque.TieneLatigo ? ataque.ObjetivoEnApuntadoLatigo : ataque.EnAlcance(ataqueRival);
            if (enAlcance && !estadoRival.EsInvulnerable)
            {
                if (!reaccionandoGolpe) { preparadoParaGolpear = Time.time + Reaccion; reaccionandoGolpe = true; }
                bool dashPeligroso = ataque.TieneLatigo && evasionProfesora.TrayectoPeligroso(profesoras,
                    ataque.PuntoLatigo - (movimiento.PosicionPies - transform.position), ataque.DuracionDashLatigo);
                if (Time.time >= preparadoParaGolpear && !dashPeligroso &&
                    !mano.TieneCartaEnActivacion(EfectoCarta.Latigo) && ataque.IntentarAtacar())
                { GolpesIntentados++; reaccionandoGolpe = false; siguienteDecision = 0f; return; }
                // Acompanar al rival durante la preparacion; pararse en el borde
                // dejaba escapar a alguien que caminase a la misma velocidad.
                if (Experta)
                {
                    Vector2 diferencia = estadoRival.ZonaDano.Centro - movimiento.PosicionPies;
                    float margen = ataque.AlcanceNormal * 0.7f;
                    Vector2 ajuste = new Vector2(diferencia.x - Mathf.Sign(diferencia.x) * margen, diferencia.y);
                    Vector2 velocidadDeseada = velocidadRivalObservada + ajuste / Mathf.Max(0.04f, Time.deltaTime);
                    entrada.Ordenar(Vector2.ClampMagnitude(velocidadDeseada / Rapidez, 1f));
                }
                return;
            }
        }
        else reaccionandoGolpe = false;
        if (tarea == Tarea.Viajar)
        {
            var puerta = objetivo != null ? objetivo.GetComponent<PuertaTeletransporte>() : null;
            if (puerta == null || !puerta.Disponible || !viajero.PuedeUsar)
            { siguienteDecision = 0f; return; }
            Physics2D.SyncTransforms();
            if (puerta.EstaEnAlcance(viajero.ColliderPies))
            { entrada.Ordenar(Vector2.zero, false, true); siguienteDecision = 0f; return; }
        }
        AvanzarPorRuta();
    }

    private void AvanzarPorRuta(float tolerancia = 0.18f)
    {
        while (nodoRuta < ruta.Count && Vector2.Distance(transform.position, ruta[nodoRuta]) < tolerancia) nodoRuta++;
        if (nodoRuta >= ruta.Count) { siguienteDecision = 0f; return; }
        Vector2 delta = ruta[nodoRuta] - transform.position;
        Vector2 paso = Vector2.ClampMagnitude(delta / Mathf.Max(0.001f, Rapidez * Time.deltaTime), 1f);
        entrada.Ordenar(paso);
        if (Vector2.Distance(transform.position, ultimaPosicion) < 0.002f) desdeSinAvance += Time.deltaTime;
        else desdeSinAvance = 0f;
        ultimaPosicion = transform.position;
        if (desdeSinAvance > 0.8f)
        { desdeSinAvance = 0f; direccionExploracion *= -1f; tarea = Tarea.Esperar; objetivo = null; siguienteDecision = 0f; }
    }

    private void Establecer(Tarea nueva, Transform blanco, Vector3 punto)
    {
        bool entrabaEnCombate = tarea == Tarea.Combatir || tarea == Tarea.Interrumpir;
        bool entraEnCombate = nueva == Tarea.Combatir || nueva == Tarea.Interrumpir;
        if (nueva != tarea || blanco != objetivo) preparandoSuccion = false;
        if (blanco != objetivo || !entrabaEnCombate || !entraEnCombate)
        {
            reaccionandoGolpe = false;
            if (entraEnCombate && Experta)
            {
                // La reaccion transcurre mientras se acerca, no despues de llegar.
                preparadoParaGolpear = Time.time + Reaccion;
                reaccionandoGolpe = true;
                compromisoCombateHasta = Time.time + 0.9f;
            }
        }
        tarea = nueva; objetivo = blanco; destino = navegacion.Limitar(punto); nodoRuta = 0;
        navegacion.CalcularRuta(transform.position, destino, ruta);
    }

    private Vector3 DestinoEstudiante(Transform estudiante)
    {
        Vector3 punto = estudiante.position;
        var caminante = estudiante.GetComponent<EstudiantePasillo>();
        if (caminante != null)
            punto += (Vector3)caminante.VelocidadRecorrido * Mathf.Min(0.8f, Vector2.Distance(transform.position, punto) / Rapidez);
        return punto - (succion.OrigenSuccion - transform.position);
    }

    private Vector3 PuntoParaGolpear()
    {
        Vector3 centro = estadoRival.ZonaDano.Centro;
        if (Experta)
        {
            float viaje = Mathf.Max(0f, Vector2.Distance(movimiento.PosicionPies, centro) - ataque.AlcanceNormal * 0.7f) / Rapidez;
            centro += (Vector3)velocidadRivalObservada * Mathf.Min(0.65f, viaje);
        }
        float lado = movimiento.PosicionPies.x < centro.x ? -1f : 1f;
        Vector3 punto = centro + Vector3.right * lado * ataque.AlcanceNormal * 0.7f;
        punto -= movimiento.PosicionPies - transform.position;
        if (Vector2.Distance(transform.position, punto) < 0.2f) return transform.position;
        return punto;
    }

    private void ObservarMovimientoRival()
    {
        if (rival == null) return;
        Vector3 actual = rival.transform.position;
        if (movimientoRival != null && Time.deltaTime > 0f)
        {
            Vector2 desplazamiento = actual - posicionRivalAnterior;
            // La CPU corre antes que MovimientoJugador2D: este desplazamiento
            // pertenece al paso anterior, no al deltaTime del frame actual.
            float paso = pasoMovimientoRivalAnterior > 0f ? pasoMovimientoRivalAnterior : Time.deltaTime;
            // Un teletransporte no representa una velocidad de carrera.
            float maximo = Mathf.Max(0.1f, movimientoRival.VelocidadActual);
            if (desplazamiento.magnitude <= maximo * paso * 2f + 0.05f)
                velocidadRivalObservada = Vector2.Lerp(velocidadRivalObservada,
                    desplazamiento / paso, 1f - Mathf.Exp(-20f * paso));
            else velocidadRivalObservada = Vector2.zero;
        }
        else velocidadRivalObservada = Vector2.zero;
        posicionRivalAnterior = actual;
        pasoMovimientoRivalAnterior = Time.deltaTime;
    }

    private bool PuedePerseguir()
    {
        Vector2 separacion = estadoRival.ZonaDano.Centro - movimiento.PosicionPies;
        if (separacion.magnitude <= ataque.AlcanceNormal + 1.2f || ataque.TieneLatigo || succionRival.Objetivo != null)
            return true;
        // Con la misma velocidad no se gana una carrera recta desde atras.
        // Volver a recolectar o usar movilidad en lugar de perseguir eternamente.
        return Vector2.Dot(velocidadRivalObservada, separacion.normalized) < Rapidez * 0.96f;
    }

    private Vector3 PuntoDeEscape()
    {
        Vector3 posicion = transform.position;
        Bounds piso = movimiento.ZonaMovimiento.bounds;
        float arriba = piso.max.y - posicion.y, abajo = posicion.y - piso.min.y;
        Vector3 punto = posicion + Vector3.up * (arriba > abajo ? 3f : -3f);
        punto.x += Mathf.Sign(posicion.x - rival.transform.position.x) * 1.5f;
        return punto;
    }

    private bool MantenerAbsorcion()
    {
        if (!succion.EstaAbsorbiendo) return false;
        Vector2 diferencia = movimiento.PosicionPies - movimientoRival.PosicionPies;
        bool seAcerca = Vector2.Dot(velocidadRivalObservada, diferencia.normalized) > 0.5f;
        bool mismoCarril = Mathf.Abs(diferencia.y) < ataqueRival.ToleranciaY + 0.4f;
        float aviso = ataqueRival.AlcanceNormal + movimientoRival.VelocidadActual * Reaccion + 0.5f;
        float llegada = Mathf.Max(0f, diferencia.magnitude - ataqueRival.AlcanceNormal) /
            Mathf.Max(0.1f, movimientoRival.VelocidadActual);
        bool peligro = Dificultad != DificultadCPU.Facil && !estado.EsInvulnerable && !estadoRival.EstaAturdido &&
            mismoCarril && (seAcerca || ataqueRival.EstaAtacando) && diferencia.magnitude < aviso &&
            Mathf.Max(llegada, ataqueRival.RecargaRestante) + 0.15f < succion.TiempoRestanteDrenaje;
        if (peligro)
        {
            succion.CancelarSuccion();
            preparandoSuccion = false;
            siguienteDecision = 0f;
            return true; // Soltar un frame antes de pelear o escapar.
        }
        tarea = Tarea.Absorber;
        objetivo = succion.Objetivo.transform;
        entrada.Ordenar(Vector2.zero, true);
        return true;
    }

    private bool EsquivarProfesora(Vector2 intencion, float previsiones = -1f)
    {
        float anticipacion = previsiones > 0f ? previsiones : Experta ? 1.4f : Dificultad == DificultadCPU.Normal ? 0.95f : 0.45f;
        if (!evasionProfesora.Intentar(profesoras, intencion, anticipacion, out Vector3 seguro)) return false;
        mano.Cerrar();
        succion.CancelarSuccion();
        Establecer(Tarea.Escapar, null, seguro);
        desdeSinAvance = 0f;
        AvanzarPorRuta(0.025f);
        return true;
    }

    private bool IntentarAtajo(Vector3 meta)
    {
        if (viajero == null || !viajero.PuedeUsar || Vector2.Distance(transform.position, meta) < 12f) return false;
        PuertaTeletransporte mejor = null;
        float mejorAhorro = 3f;
        foreach (PuertaTeletransporte puerta in puertas)
        {
            if (!puerta.Disponible || puerta.gameObject.scene != gameObject.scene) continue;
            float suma = 0f; int cantidad = 0;
            foreach (PuertaTeletransporte salida in puertas)
                if (salida != puerta && salida.Disponible)
                { suma += Vector2.Distance(salida.PosicionSalida, meta); cantidad++; }
            if (cantidad == 0) continue;
            float ahorro = Vector2.Distance(transform.position, meta) -
                Vector2.Distance(movimiento.PosicionPies, puerta.PosicionSalida) - suma / cantidad - Rapidez * 0.8f;
            if (ahorro > mejorAhorro) { mejorAhorro = ahorro; mejor = puerta; }
        }
        if (mejor == null) return false;
        Establecer(Tarea.Viajar, mejor.transform, mejor.PosicionSalida - (movimiento.PosicionPies - transform.position));
        return true;
    }

    private bool IntentarCarta(float separacion)
    {
        if (mano.Cantidad == 0 || mano.EstaConfundido || ataque.EstaAtacando ||
            partida.TiempoRestante < 2f) return false;
        // No gastar la ventaja al cerrar la partida si la carta no puede recuperarla.
        if (partida.TiempoRestante < 8f && almas.Cantidad >= rival.Cantidad) return false;
        int mejor = -1;
        float mejorValor = Experta ? 3f : Dificultad == DificultadCPU.Normal ? 5f : 10f;
        for (int i = 0; i < mano.Cantidad; i++)
        {
            DefinicionCarta carta = mano.Cartas[i];
            if (carta.PuntosNecesarios > almas.Cantidad) continue;
            float valor = ValorarCarta(carta, separacion) - carta.PuntosNecesarios;
            if (valor > mejorValor) { mejor = i; mejorValor = valor; }
        }
        if (mejor < 0 || !mano.AbrirParaCPU(mejor)) return false;
        confirmarCarta = Time.time + Mathf.Max(mano.TiempoApertura, Reaccion);
        entrada.Limpiar();
        return true;
    }

    private float ValorarCarta(DefinicionCarta carta, float separacion)
    {
        float duracion = Mathf.Min(carta.DuracionEfecto, Mathf.Max(0f, partida.TiempoRestante - 1f));
        bool propia = carta.Destino == DestinoCarta.Propio;
        switch (carta.Efecto)
        {
            case EfectoCarta.Velocidad:
                if (propia)
                    return efectos.MultiplicadorVelocidad >= carta.MultiplicadorVelocidad ? 0f :
                        (carta.MultiplicadorVelocidad - 1f) * duracion * 5f;
                var efectoRival = rival.GetComponent<EfectosJugador>();
                if (efectoRival != null && efectoRival.MultiplicadorVelocidad <= carta.MultiplicadorVelocidad) return 0f;
                // Frenar un drenaje abre una ventana para interrumpirlo; no es solo caminar mas lento.
                return (1f - carta.MultiplicadorVelocidad) * duracion * (succionRival.Objetivo != null ? 8f : 5.5f);
            case EfectoCarta.Latigo:
                float llegada = Mathf.Max(0f, separacion - ataque.AlcanceNormal) / Rapidez;
                if (ataque.TieneLatigo || estadoRival.EsInvulnerable || llegada + 1f >= duracion) return 0f;
                return Mathf.Min(rival.Cantidad, carta.AlmasExtraLatigo) * (llegada < 1f ? 0.95f : 0.65f) +
                    (succionRival.Objetivo != null ? 20f : 7f) + (carta.EmpujaHastaExtremo ? 12f : 0f);
            case EfectoCarta.IntercambioPosicion:
                float demoraSwap = mano.TiempoHastaEfecto(carta) + Mathf.Max(0f, Reaccion - mano.TiempoApertura);
                Vector3 llegadaSwap = rival.transform.position + (Vector3)velocidadRivalObservada * demoraSwap;
                if (evasionProfesora.DestinoPeligroso(profesoras, llegadaSwap, demoraSwap)) return 0f;
                float ventaja = 0f;
                foreach (AlmaEstudiante estudiante in FindObjectsByType<AlmaEstudiante>(FindObjectsSortMode.None))
                {
                    if (!estudiante.Disponible || !Visible(estudiante.transform.position)) continue;
                    if (estudiante.Ocupante == rival && succionRival.Objetivo == estudiante &&
                        succionRival.TiempoRestanteDrenaje <= demoraSwap + 0.1f) continue;
                    float ahorro = Vector2.Distance(transform.position, estudiante.transform.position) -
                        Vector2.Distance(rival.transform.position, estudiante.transform.position);
                    ventaja = Mathf.Max(ventaja, ahorro / Rapidez * 4f +
                        (estudiante.Ocupante == rival ? estudiante.EsRaro ? 28f : 15f : 0f));
                }
                return ventaja;
            case EfectoCarta.Confusion:
                return propia || estadoRival.EstaAturdido ? 0f : duracion * 3.5f;
            case EfectoCarta.Perfume:
            case EfectoCarta.BombaApestosa:
                bool repeler = carta.Efecto == EfectoCarta.BombaApestosa;
                if (propia == repeler) return 0f;
                int cerca = 0;
                Vector3 centro = propia ? transform.position : rival.transform.position;
                foreach (AlmaEstudiante estudiante in FindObjectsByType<AlmaEstudiante>(FindObjectsSortMode.None))
                    if (estudiante.Disponible && !estudiante.EstaOcupado && Visible(estudiante.transform.position) &&
                        Vector2.Distance(centro, estudiante.transform.position) < carta.RadioOlor) cerca++;
                return cerca == 0 ? 0f : Mathf.Min(cerca, 6) * (repeler ? 8f : 6f) + duracion;
            default: return 0f;
        }
    }

    private bool Visible(Vector3 punto)
    {
        foreach (Camera camara in camaras)
        {
            if (camara == null || !camara.isActiveAndEnabled || camara.gameObject.scene != gameObject.scene ||
                camara.GetComponent<CamaraPasillo>() == null) continue;
            Vector3 vista = camara.WorldToViewportPoint(punto);
            if (vista.z > 0f && vista.x >= 0f && vista.x <= 1f && vista.y >= 0f && vista.y <= 1f) return true;
        }
        return false;
    }

    private void OnDisable()
    {
        if (entrada != null) entrada.Limpiar();
        if (succion != null) succion.CancelarSuccion();
        if (mano != null) mano.Cerrar();
    }

    private void OnDestroy()
    {
        if (entrada != null) entrada.ActivarCPU(false);
    }
}
