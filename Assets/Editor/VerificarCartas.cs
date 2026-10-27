using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Verifica assets y reglas en una escena de preview, sin alterar la escena abierta.
public static class VerificarCartas
{
    private const string Solicitud = "Temp/VerificarCartas.request";
    private const string Resultado = "Temp/VerificarCartas.resultado.txt";

    [InitializeOnLoadMethod]
    private static void AlRecargar()
    {
        if (File.Exists(Solicitud))
            EditorApplication.delayCall += EjecutarSolicitud;
    }

    private static void EjecutarSolicitud()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= EsperarEdicion;
            EditorApplication.playModeStateChanged += EsperarEdicion;
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EjecutarSolicitud;
            return;
        }
        File.Delete(Solicitud);
        Validar();
    }

    private static void EsperarEdicion(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.EnteredEditMode)
            return;
        EditorApplication.playModeStateChanged -= EsperarEdicion;
        EditorApplication.delayCall += EjecutarSolicitud;
    }

    [MenuItem("Emoliente/Verificar cartas (sin Play)")]
    public static void Validar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Ejecuta esta verificacion fuera de Play.");
        var escena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ConfusionCard.unity");
        try
        {
            AssetDatabase.ImportAsset("Assets/Cartas/CartaAceleracion.asset", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Cartas/CartaPesadez.asset", ImportAssetOptions.ForceUpdate);
            DefinicionCarta carta = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaAceleracion.asset");
            DefinicionCarta pesadez = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaPesadez.asset");
            AssetDatabase.ImportAsset("Assets/Cartas/CartaAceleracionPotente.asset", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Cartas/CartaPesadezPotente.asset", ImportAssetOptions.ForceUpdate);
            DefinicionCarta aceleracionPotente = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaAceleracionPotente.asset");
            DefinicionCarta pesadezPotente = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaPesadezPotente.asset");
            AssetDatabase.ImportAsset("Assets/Cartas/CartaConfusion.asset", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Cartas/CartaConfusionPotente.asset", ImportAssetOptions.ForceUpdate);
            DefinicionCarta confusion = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaConfusion.asset");
            DefinicionCarta confusionPotente = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaConfusionPotente.asset");
            string[] nombresOlor = { "CartaPerfume", "CartaPerfumePotente", "CartaBombaApestosa", "CartaBombaApestosaPotente" };
            var cartasOlor = new DefinicionCarta[4];
            for (int i = 0; i < nombresOlor.Length; i++)
            {
                string ruta = "Assets/Cartas/" + nombresOlor[i] + ".asset";
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                cartasOlor[i] = AssetDatabase.LoadAssetAtPath<DefinicionCarta>(ruta);
                Exigir(cartasOlor[i] != null && cartasOlor[i].DuracionEfecto == (i % 2 == 0 ? 8f : 20f) &&
                    cartasOlor[i].PuntosNecesarios == (i % 2 == 0 ? 10f : 20f) && cartasOlor[i].RadioOlor == 12f &&
                    cartasOlor[i].MultiplicadorAtraccion == 1.5f && cartasOlor[i].MultiplicadorRetirada == 1.6f,
                    "Assets de olor: radio 12, atraccion 1.5x y retirada 1.6x");
            }
            Exigir(confusion != null && confusion.Efecto == EfectoCarta.Confusion && confusion.DuracionEfecto == 10f &&
                confusionPotente != null && confusionPotente.EsPotente && confusionPotente.DuracionEfecto == 16f,
                "Confusion normal 10 s y potente 16 s");
            Exigir(aceleracionPotente != null && pesadezPotente != null &&
                aceleracionPotente.EsPotente && pesadezPotente.EsPotente &&
                aceleracionPotente.DuracionEfecto == 20f && pesadezPotente.DuracionEfecto == 20f &&
                aceleracionPotente.PuntosNecesarios == 25f && pesadezPotente.PuntosNecesarios == 20f &&
                aceleracionPotente.MultiplicadorVelocidad == carta.MultiplicadorVelocidad &&
                pesadezPotente.MultiplicadorVelocidad == pesadez.MultiplicadorVelocidad,
                $"Potentes: Aceleracion 25, Pesadez 20 puntos, 20 segundos e intensidad normal. A: {aceleracionPotente?.EsPotente}, {aceleracionPotente?.PuntosNecesarios}, {aceleracionPotente?.DuracionEfecto}, {aceleracionPotente?.MultiplicadorVelocidad}; P: {pesadezPotente?.EsPotente}, {pesadezPotente?.PuntosNecesarios}, {pesadezPotente?.DuracionEfecto}, {pesadezPotente?.MultiplicadorVelocidad}");
            Exigir(carta != null && carta.Frente != null && carta.Reverso != null, "Definicion y ambos sprites validos");
            Exigir(pesadez != null && pesadez.Frente != null && pesadez.Reverso != null, "Sprites de Pesadez validos");
            Exigir(carta.PuntosNecesarios == 10f && pesadez.PuntosNecesarios == 10f &&
                carta.DuracionEfecto == 8f && pesadez.DuracionEfecto == 8f,
                "Cartas normales: costo 10 y duracion 8 segundos");
            Exigir(carta.Destino == DestinoCarta.Propio && carta.MultiplicadorVelocidad > 1f &&
                pesadez.Destino == DestinoCarta.Rival && pesadez.MultiplicadorVelocidad < 1f,
                $"Buff propio y debuff rival configurados: {carta.Destino} x{carta.MultiplicadorVelocidad}, {pesadez.Destino} x{pesadez.MultiplicadorVelocidad}");
            SistemaCartas sistema = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform hijo in raiz.GetComponentsInChildren<Transform>(true))
                    Exigir(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(hijo.gameObject) == 0,
                        "Sin scripts faltantes: " + hijo.name);
                if (sistema == null) sistema = raiz.GetComponentInChildren<SistemaCartas>(true);
            }
            Exigir(sistema != null, "Sistema presente en ConfusionCard");
            AssetDatabase.ImportAsset("Assets/Resources/CatalogoCartas.asset", ImportAssetOptions.ForceUpdate);
            // Reproducir la escena cargada con una lista antigua: debe migrar al catalogo comun.
            Campo(sistema, "catalogo", new[] { carta, pesadez });
            typeof(SistemaCartas).GetMethod("CargarCatalogo", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sistema, null);
            var serializado = new SerializedObject(sistema);
            Exigir(serializado.FindProperty("catalogoCompartido").objectReferenceValue != null,
                "Catalogo compartido cargado sin depender de reabrir la escena");
            foreach (string campo in new[] { "partida", "jugador1", "jugador2", "camara1", "camara2" })
                Exigir(serializado.FindProperty(campo).objectReferenceValue != null, "Referencia " + campo);
            var catalogo = serializado.FindProperty("catalogo");
            var latigo = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaLatigo.asset");
            var golpePotente = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaGolpeFuertePotente.asset");
            Exigir(golpePotente != null && golpePotente.EsPotente && golpePotente.EmpujaHastaExtremo && golpePotente.PuntosNecesarios == 20f && golpePotente.DuracionEfecto == 10f && golpePotente.AlmasExtraLatigo == 60f, "Golpe fuerte potente: costo 20, marca 10 s, extra 60 y empuje al extremo");
            var swap = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaSwapPosicion.asset");
            Exigir(latigo != null && latigo.Efecto == EfectoCarta.Latigo && !latigo.EsPotente &&
                latigo.PuntosNecesarios == 10f && latigo.DuracionEfecto == 8f &&
                latigo.AlmasExtraLatigo == 40f && latigo.EmpujonLatigo == 7f,
                "Latigo normal: costo 10, marca 8 s, extra 40 y empujon 7");
            Exigir(swap != null && swap.Efecto == EfectoCarta.IntercambioPosicion && !swap.EsPotente &&
                swap.PuntosNecesarios == 10f && swap.EsInstantanea && swap.DuracionEfecto == 0f,
                "Swap normal: costo 10 y efecto instantaneo");
            Exigir(System.Array.IndexOf(((CatalogoCartas)serializado.FindProperty("catalogoCompartido").objectReferenceValue).Cartas, latigo) >= 0 &&
                System.Array.IndexOf(((CatalogoCartas)serializado.FindProperty("catalogoCompartido").objectReferenceValue).Cartas, swap) >= 0,
                "Latigo y swap disponibles en catalogo compartido");
            Exigir(catalogo.arraySize == 13 && catalogo.GetArrayElementAtIndex(0).objectReferenceValue == carta &&
                catalogo.GetArrayElementAtIndex(1).objectReferenceValue == pesadez &&
                catalogo.GetArrayElementAtIndex(2).objectReferenceValue == aceleracionPotente &&
                catalogo.GetArrayElementAtIndex(3).objectReferenceValue == pesadezPotente &&
                catalogo.GetArrayElementAtIndex(4).objectReferenceValue == confusion &&
                catalogo.GetArrayElementAtIndex(5).objectReferenceValue == confusionPotente,
                $"Catalogo conectado a normales y potentes: {catalogo.arraySize} entradas cargadas; compartido {((CatalogoCartas)serializado.FindProperty("catalogoCompartido").objectReferenceValue).Cartas.Length}");

            var partida = (CicloPartida)serializado.FindProperty("partida").objectReferenceValue;
            var p1 = (ManoCartasJugador)serializado.FindProperty("jugador1").objectReferenceValue;
            var p2 = (ManoCartasJugador)serializado.FindProperty("jugador2").objectReferenceValue;
            p1.Inicializar(sistema);
            p2.Inicializar(sistema);
            Exigir(!p1.Recibir(carta), "No entregar antes del inicio");
            Propiedad(partida, "EnCurso", true);
            Propiedad(partida, "TiempoRestante", 180f);
            Exigir(sistema.EnCurso, "Ronda simulada habilitada en preview");
            var catalogoCompleto = (DefinicionCarta[])LeerCampo(sistema, "catalogo");
            Campo(sistema, "catalogo", new[] { aceleracionPotente, pesadezPotente });
            Exigir(!sistema.EntregarCarta(p2), "No entregar potentes antes de mitad de partida");
            Exigir(sistema.EntregarCarta(p2, true) && p2.Cantidad == 0 && p2.EspaciosOcupados == 1,
                "Debug entrega potente temprano reservando cupo antes de llegar a mano");
            var colaEntrega = (System.Collections.IList)LeerCampo(sistema, "cola");
            Exigir((bool)colaEntrega[0].GetType().GetField("esEntrega").GetValue(colaEntrega[0]),
                "Debug conserva animacion de entrega potente");
            Exigir(!sistema.EntregarCarta(p2, true), "Maximo una potente contando pendientes");
            Exigir(p2.ReservarEntrega(carta) && p2.ReservarEntrega(carta) &&
                p2.EspaciosOcupados == 3 && !p2.Recibir(carta), "Reservas respetan capacidad total de tres");
            typeof(SistemaCartas).GetMethod("CancelarPresentaciones", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sistema, null);
            Exigir(p2.EspaciosOcupados == 0, "Cancelar presentaciones libera cupos pendientes");
            Campo(sistema, "catalogo", catalogoCompleto);
            Exigir(sistema.EntregarCarta(p2, true, true) && p2.TienePotente && p2.Cantidad == 0,
                "Debug forzado elige potente de catalogo mixto antes de mitad");
            typeof(SistemaCartas).GetMethod("CancelarPresentaciones", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sistema, null);
            Campo(sistema, "catalogo", new[] { aceleracionPotente, pesadezPotente });
            Propiedad(partida, "TiempoRestante", 90.01f);
            Exigir(!sistema.PuedeEntregar(p2, aceleracionPotente), "Bloqueada antes del limite exacto");
            Propiedad(partida, "TiempoRestante", 90f);
            Exigir(sistema.EntregarCarta(p2), "Potentes habilitadas desde mitad exacta de partida");
            var pendientes = (System.Collections.Generic.List<DefinicionCarta>)LeerCampo(p2, "entregasPendientes");
            DefinicionCarta entregada = pendientes[0];
            Exigir(p2.CompletarEntrega(entregada) && p2.Cantidad == 1 && p2.EspaciosOcupados == 1,
                "Entrega termina en mano sin duplicar reserva");
            Exigir(!p2.CompletarEntrega(entregada) && !sistema.PuedeEntregar(p2, aceleracionPotente, true),
                "No duplicar entrega ni otra potente en mano");
            typeof(SistemaCartas).GetMethod("CancelarPresentaciones", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sistema, null);
            Campo(sistema, "catalogo", catalogoCompleto);
            Propiedad(partida, "TiempoRestante", 180f);
            Exigir(p1.Recibir(carta) && p1.Recibir(carta) && p1.Recibir(carta), "Recibir tres cartas");
            Exigir(!p1.Recibir(carta) && p1.Cantidad == 3, "Limite de tres sin reemplazar cartas");
            Propiedad(p1, "MenuAbierto", true);
            Exigir(!p1.IntentarUsar() && p1.Cantidad == 3 && p1.MenuAbierto && p1.Almas.Cantidad == 0f,
                "Rechazar sin gastar puntos ni consumir carta");
            p1.Almas.Recibir(carta.PuntosNecesarios);
            float puntos = p1.Almas.Cantidad;
            int cobros = 0;
            p1.Almas.CantidadCambiada += _ => cobros++;
            Exigir(p1.IntentarUsar() && p1.Cantidad == 2 && !p1.MenuAbierto, "Consumir una carta y cerrar menu");
            Exigir(p1.Almas.Cantidad == puntos - carta.PuntosNecesarios && cobros == 1, "Descontar costo una sola vez y notificar marcador");
            Exigir(!p1.IntentarUsar() && p1.Cantidad == 2 && cobros == 1, "No usar ni cobrar fuera del menu");
            Propiedad(p1, "MenuAbierto", true);
            p1.Cerrar();
            Exigir(p1.Cantidad == 2 && !p1.MenuAbierto, "Cerrar sin consumir");

            var preparar = typeof(SistemaCartas).GetMethod("PrepararEfectos", BindingFlags.NonPublic | BindingFlags.Instance);
            var efectos1 = (EfectosJugador)preparar.Invoke(sistema, new object[] { p1 });
            var efectos2 = (EfectosJugador)preparar.Invoke(sistema, new object[] { p2 });
            Campo(sistema, "efectos1", efectos1);
            Campo(sistema, "efectos2", efectos2);
            var aplicar = typeof(SistemaCartas).GetMethod("AplicarCarta", BindingFlags.NonPublic | BindingFlags.Instance);
            aplicar.Invoke(sistema, new object[] { p1, carta });
            Exigir(Mathf.Approximately(efectos1.MultiplicadorVelocidad, 3f) &&
                Mathf.Approximately(efectos2.MultiplicadorVelocidad, 1f), "Buff afecta solo al lanzador");
            aplicar.Invoke(sistema, new object[] { p1, pesadez });
            aplicar.Invoke(sistema, new object[] { p1, pesadez });
            Exigir(Mathf.Approximately(efectos2.MultiplicadorVelocidad, 0.5f), "Debuff afecta rival y no acumula copias");
            aplicar.Invoke(sistema, new object[] { p2, carta });
            Exigir(Mathf.Approximately(efectos2.MultiplicadorVelocidad, 1.5f), "Buff y debuff combinados");
            efectos1.Limpiar();
            efectos2.Limpiar();
            Exigir(efectos1.MultiplicadorVelocidad == 1f && efectos2.MultiplicadorVelocidad == 1f, "Limpieza de efectos");
            VerificarConfusion(sistema, p1, p2, carta, pesadez, confusion, confusionPotente);
            VerificarOlores(sistema, p1, p2, cartasOlor);

            // Acelera unicamente el preview para comprobar entrega, espera y empate.
            Campo(sistema, "inicioFraccionPartida", 0f);
            Campo(sistema, "persistencia", 0f);
            p2.Almas.Recibir(100f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 3, "Entrega al jugador en desventaja");
            Propiedad(p1, "MenuAbierto", true);
            p1.Almas.Recibir(carta.PuntosNecesarios);
            Exigir(p1.IntentarUsar(), "Liberar espacio durante espera");
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "Espera impide entrega inmediata");
            ((float[])LeerCampo(sistema, "espera"))[0] = 0f;
            p1.Almas.Recibir(100f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "Empate no entrega cartas");
            p1.Almas.Perder(100f);
            Propiedad(partida, "TiempoRestante", 10f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "No entregar en ultimos diez segundos");
            VerificarSwapLatigo(sistema, p1, p2, latigo, swap);
            Propiedad(partida, "Terminada", true);
            Exigir(!p1.Recibir(carta), "No entregar despues del final");

            VerificarRepartos(sistema, partida, p1, p2, carta, pesadez, aceleracionPotente, pesadezPotente);
            VerificarSeguimiento(escena);
            const string mensaje = "OK: swap, cancelacion y puertas; latigo, dano extra unico e invulnerabilidad; radio 12, atraccion 1.5x, retirada 1.6x; catalogo de trece, Confusion, repartos y camaras.";
            Directory.CreateDirectory("Temp");
            File.WriteAllText(Resultado, mensaje);
            Debug.Log(mensaje);
        }
        catch (Exception error)
        {
            File.WriteAllText(Resultado, "ERROR: " + error);
            Debug.LogException(error);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }

    private static void VerificarSwapLatigo(SistemaCartas sistema, ManoCartasJugador p1, ManoCartasJugador p2,
        DefinicionCarta latigo, DefinicionCarta swap)
    {
        var movimiento1 = p1.GetComponent<MovimientoJugador2D>();
        var movimiento2 = p2.GetComponent<MovimientoJugador2D>();
        var ataque1 = p1.GetComponent<AtaqueJugador>();
        var ataque2 = p2.GetComponent<AtaqueJugador>();
        var estado2 = p2.GetComponent<EstadoCombateJugador>();
        var marca = p2.GetComponent<MarcaLatigoJugador>();
        foreach (MonoBehaviour componente in new MonoBehaviour[] { movimiento1, movimiento2,
            p1.GetComponent<EstadoCombateJugador>(), estado2, ataque1, ataque2 })
            componente.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(componente, null);
        Vector3 original1 = p1.transform.position;
        Vector3 original2 = p2.transform.position;
        float puntosOriginales = p2.Almas.Cantidad;
        var aplicar = typeof(SistemaCartas).GetMethod("AplicarCarta", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            p2.Almas.Perder(p2.Almas.Cantidad);
            p2.Almas.Recibir(100f);
            aplicar.Invoke(sistema, new object[] { p1, latigo });
            Exigir(marca != null && marca.PerteneceA(ataque1) && !marca.Consumir(ataque2), "Latigo pertenece al lanzador");
            CombatePvP combate = null;
            foreach (GameObject raiz in p1.gameObject.scene.GetRootGameObjects())
                if (combate == null) combate = raiz.GetComponentInChildren<CombatePvP>();
            Exigir(combate != null, "Coordinador de combate presente");
            float perdidaNormal = (float)LeerCampo(combate, "almasPerdidasPorGolpe");
            var golpear = typeof(CombatePvP).GetMethod("AplicarGolpe", BindingFlags.NonPublic | BindingFlags.Instance);
            golpear.Invoke(combate, new object[] { ataque1, ataque2, ataque1.StunNormal });
            Exigir(Mathf.Approximately(p2.Almas.Cantidad, 100f - perdidaNormal - latigo.AlmasExtraLatigo) &&
                !marca.EstaMarcado, "Golpe aceptado aplica extra y consume marca una sola vez");
            Exigir(Mathf.Approximately(((Vector2)LeerCampo(movimiento2, "empujonPendiente")).magnitude, 7f) &&
                (float)LeerCampo(movimiento2, "rapidezLanzamiento") >= 7f / 0.65f,
                "Golpe normal empuja 7 unidades con impulso de 0.65 s");
            aplicar.Invoke(sistema, new object[] { p1, latigo });
            float antes = p2.Almas.Cantidad;
            golpear.Invoke(combate, new object[] { ataque1, ataque2, ataque1.StunNormal });
            Exigir(p2.Almas.Cantidad == antes && marca.EstaMarcado,
                "Invulnerabilidad impide dano extra sin consumir la marca");
            Campo(estado2, "invulnerabilidadRestante", 0f);
            Campo(estado2, "aturdimientoRestante", 0f);
            Campo(marca, "finMarca", Time.time - 1f);
            golpear.Invoke(combate, new object[] { ataque1, ataque2, ataque1.StunNormal });
            Exigir(Mathf.Approximately(p2.Almas.Cantidad, antes - perdidaNormal), "Marca expirada no agrega dano");

            Vector3 centro = movimiento1.ZonaMovimiento.bounds.center;
            movimiento1.Teletransportar(centro + Vector3.left * 3f);
            movimiento2.Teletransportar(centro + Vector3.right * 3f);
            var potente = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaGolpeFuertePotente.asset");
            Campo(estado2, "invulnerabilidadRestante", 0f);
            aplicar.Invoke(sistema, new object[] { p1, potente });
            float antesPotente = p2.Almas.Cantidad;
            golpear.Invoke(combate, new object[] { ataque1, ataque2, ataque1.StunNormal });
            Exigir(Mathf.Approximately(p2.Almas.Cantidad, Mathf.Max(0f, antesPotente - perdidaNormal - potente.AlmasExtraLatigo)),
                "Golpe potente descuenta dano base mas 60 puntos extra");
            Vector3 borde = p2.transform.position;
            borde.x = movimiento2.ZonaMovimiento.bounds.max.x;
            Vector2 recorrido = (Vector2)(movimiento2.LimitarDestino(borde) - p2.transform.position);
            Exigir(Vector2.Distance((Vector2)LeerCampo(movimiento2, "empujonPendiente"), recorrido) < 0.001f &&
                Mathf.Approximately((float)LeerCampo(movimiento2, "rapidezLanzamiento"), recorrido.magnitude / 0.65f) &&
                !marca.EstaMarcado, "Golpe potente consume marca y lanza hasta el extremo respetando el cuerpo");
            movimiento2.Teletransportar(centro + Vector3.right * 3f);
            Vector3 origen1 = p1.transform.position;
            Vector3 origen2 = p2.transform.position;
            movimiento1.Empujar(Vector2.right);
            movimiento2.Empujar(Vector2.left);
            Campo(ataque1, "impactoConsumido", false);
            Propiedad(ataque1, "EstaAtacando", true);
            Propiedad(p1, "MenuAbierto", true);
            Propiedad(p2, "MenuAbierto", true);
            aplicar.Invoke(sistema, new object[] { p1, swap });
            Exigir(Vector3.Distance(p1.transform.position, origen2) < 0.001f &&
                Vector3.Distance(p2.transform.position, origen1) < 0.001f, "Swap intercambia ambas posiciones");
            Exigir(!ataque1.EstaAtacando && !p1.MenuAbierto && !p2.MenuAbierto &&
                (Vector2)LeerCampo(movimiento1, "empujonPendiente") == Vector2.zero &&
                (Vector2)LeerCampo(movimiento2, "empujonPendiente") == Vector2.zero,
                "Swap cancela golpe, menus y empujones pendientes");
            foreach (ManoCartasJugador jugador in new[] { p1, p2 })
            {
                var puerta = jugador.GetComponent<UsarPuertaJugador>();
                if (puerta != null) Exigir((bool)LeerCampo(puerta, "requiereSoltar"), "Swap evita teletransporte encadenado por puerta");
            }
        }
        finally
        {
            marca?.Limpiar();
            Campo(estado2, "invulnerabilidadRestante", 0f);
            Campo(estado2, "aturdimientoRestante", 0f);
            movimiento1.Teletransportar(original1); movimiento2.Teletransportar(original2);
            p2.Almas.Perder(p2.Almas.Cantidad); p2.Almas.Recibir(puntosOriginales);
        }
    }

    private static void VerificarOlores(SistemaCartas sistema, ManoCartasJugador p1, ManoCartasJugador p2,
        DefinicionCarta[] cartas)
    {
        Color buff = cartas[0].ColorCarta;
        Color debuff = cartas[2].ColorCarta;
        Exigir(buff.g > buff.r && buff.g > buff.b && debuff.r > debuff.g && debuff.r > debuff.b,
            "Carta Perfume verde y Bomba roja; las auras conservan rosa/verde");
        AuraOlorJugador a1 = p1.GetComponent<AuraOlorJugador>();
        AuraOlorJugador a2 = p2.GetComponent<AuraOlorJugador>();
        BoxCollider2D zona = p1.GetComponent<MovimientoJugador2D>().ZonaMovimiento;
        Vector3 original1 = p1.transform.position;
        Vector3 original2 = p2.transform.position;
        GameObject npc = null;
        try
        {
            Vector3 centro = zona.bounds.center;
            p1.transform.position = centro;
            p2.transform.position = centro + Vector3.right * 4f;
            var aplicar = typeof(SistemaCartas).GetMethod("AplicarCarta", BindingFlags.NonPublic | BindingFlags.Instance);
            aplicar.Invoke(sistema, new object[] { p1, cartas[0] });
            Exigir(a1.TienePerfume && !a2.TienePerfume, "Perfume propio");
            typeof(AuraOlorJugador).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(a1, null);
            var halos = (System.Collections.IList)LeerCampo(a1, "halos");
            Exigir(halos.Count > 0 && p1.transform.Find("RadioOlor") == null && p1.transform.Find("OndaOlor") == null,
                "Halo de silueta sin anillo de alcance ni lineas de olor");
            foreach (object halo in halos)
            {
                var copia = (SpriteRenderer)halo.GetType().GetField("copia").GetValue(halo);
                var fuente = (SpriteRenderer)halo.GetType().GetField("fuente").GetValue(halo);
                Exigir(copia.sprite == fuente.sprite && copia.color.a > 0f && copia.color.a < 0.1f &&
                    copia.sortingOrder < fuente.sortingOrder, "Halo difuminado detras del sprite");
            }
            Vector3 inicio = centro + Vector3.right * 3f;
            // Tolerancia absoluta: las coordenadas grandes del mapa pierden precision submilimetrica.
            Exigir(AuraOlorJugador.CalcularPaso(inicio, 2f, 0.1f, zona.bounds, p1.gameObject.scene, 1, out Vector3 atraido) &&
                Vector2.Distance(atraido, centro) < Vector2.Distance(inicio, centro) &&
                Mathf.Abs(Vector3.Distance(atraido, inicio) - 0.3f) < 0.0001f,
                $"Perfume acerca al estudiante a 1.5x (paso {Vector3.Distance(atraido, inicio)})");
            aplicar.Invoke(sistema, new object[] { p1, cartas[2] });
            Exigir(a2.TieneMalOlor && !a1.TieneMalOlor, "Bomba aplicada al rival");
            Vector3 cercaBomba = p2.transform.position + Vector3.right * 2f;
            Exigir(AuraOlorJugador.CalcularPaso(cercaBomba, 2f, 0.1f, zona.bounds, p1.gameObject.scene, 2, out Vector3 alejado) &&
                alejado.x > cercaBomba.x && Mathf.Abs(Vector3.Distance(alejado, cercaBomba) - 0.32f) < 0.0001f,
                "Retirada a 1.6x, sin huida permanente");
            AuraOlorJugador.Resolver(centro + Vector3.right * 2f, p1.gameObject.scene, out _, out bool repeler, out _);
            Exigir(repeler, "En empate gana bomba");
            AuraOlorJugador.Resolver(centro + Vector3.right * 0.1f, p1.gameObject.scene, out _, out repeler, out _);
            Exigir(!repeler, "Gana el olor mas cercano");
            aplicar.Invoke(sistema, new object[] { p1, cartas[3] });
            float fin = (float)LeerCampo(a2, "finBomba");
            aplicar.Invoke(sistema, new object[] { p1, cartas[2] });
            Exigir((float)LeerCampo(a2, "finBomba") == fin, "Bomba normal no acorta potente");
            a2.Limpiar();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Estudiante.prefab");
            npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p1.gameObject.scene);
            var estudiante = npc.GetComponent<EstudiantePasillo>();
            typeof(EstudiantePasillo).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(estudiante, null);
            estudiante.ConfigurarRecorrido(1f, 2f, zona.bounds.min.x - 10f, zona.bounds.max.x + 10f, zona);
            npc.transform.position = inicio;
            var avanzar = typeof(EstudiantePasillo).GetMethod("AvanzarMovimiento", BindingFlags.NonPublic | BindingFlags.Instance);
            avanzar.Invoke(estudiante, new object[] { 0.1f });
            Exigir(npc.transform.position.x < inicio.x, "Estudiante real responde a perfume");
            a1.Limpiar();
            Vector3 antesNormal = npc.transform.position;
            avanzar.Invoke(estudiante, new object[] { 0.1f });
            Exigir(Mathf.Approximately(npc.transform.position.x, antesNormal.x + 0.2f), "Recupera recorrido al terminar olor");
            aplicar.Invoke(sistema, new object[] { p1, cartas[2] });
            npc.transform.position = cercaBomba;
            estudiante.ConfigurarRecorrido(-1f, 2f, zona.bounds.min.x - 10f, zona.bounds.max.x + 10f, zona);
            avanzar.Invoke(estudiante, new object[] { 0.1f });
            Exigir(estudiante.Direccion == 1f && npc.transform.position.x > cercaBomba.x,
                "Bomba cambia el sentido del estudiante que venia hacia ella");
            a2.Limpiar();
            Vector3 despuesGiro = npc.transform.position;
            avanzar.Invoke(estudiante, new object[] { 0.1f });
            Exigir(Mathf.Approximately(npc.transform.position.x, despuesGiro.x + 0.2f),
                "Mantiene el nuevo sentido al terminar el mal olor");
            p2.transform.position = new Vector3(zona.bounds.max.x - 2f, centro.y, centro.z);
            aplicar.Invoke(sistema, new object[] { p1, cartas[2] });
            npc.transform.position = new Vector3(zona.bounds.max.x - 0.1f, centro.y, centro.z);
            avanzar.Invoke(estudiante, new object[] { 0.1f });
            Exigir(npc.transform.position.x > zona.bounds.max.x, "No se queda inmovil en el borde del aura/pasillo");
            p2.transform.position = centro + Vector3.right * 4f;
            npc.transform.position = cercaBomba;
            var alma = npc.GetComponent<AlmaEstudiante>();
            Exigir(alma.IntentarOcupar(p1.Almas), "Reservar absorcion en curso");
            aplicar.Invoke(sistema, new object[] { p1, cartas[2] });
            Vector3 ocupado = npc.transform.position;
            var succion = p1.GetComponent<SuccionJugador>();
            _ = succion.OrigenSuccion;
            Vector3 alineacion = succion.PosicionAlineacion;
            var piesNPC = npc.GetComponent<BoxCollider2D>();
            if (piesNPC != null) alineacion -= piesNPC.transform.TransformPoint(piesNPC.offset) - ocupado;
            alineacion = succion.LimitarAlineacion(alineacion);
            alineacion.z = ocupado.z;
            float distanciaAntes = Vector3.Distance(ocupado, alineacion);
            avanzar.Invoke(estudiante, new object[] { 0.5f });
            Exigir(alma.Ocupante == p1.Almas && !alma.Asustado &&
                (distanciaAntes < 0.01f || Vector3.Distance(npc.transform.position, alineacion) < distanciaAntes - 0.001f),
                "Bomba conserva la absorcion y permite alinear el estudiante sin asustarlo");
            Campo(a2, "finBomba", Time.time - 1f);
            Exigir(!AuraOlorJugador.Resolver(inicio, p1.gameObject.scene, out _, out _, out _), "Olor expira");
        }
        finally
        {
            a1.Limpiar(); a2.Limpiar();
            p1.transform.position = original1; p2.transform.position = original2;
            if (npc != null) UnityEngine.Object.DestroyImmediate(npc);
        }
    }

    private static void VerificarConfusion(SistemaCartas sistema, ManoCartasJugador p1, ManoCartasJugador p2,
        DefinicionCarta normal1, DefinicionCarta normal2, DefinicionCarta confusion, DefinicionCarta potente)
    {
        var config = new SerializedObject(sistema);
        var tags = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var serializadoTags = new SerializedObject(tags);
        var capas = serializadoTags.FindProperty("layers");
        for (int i = 8; i <= 9; i++)
        {
            string nombre = i == 8 ? "FantasmasP1" : "FantasmasP2";
            string anteriorNombre = capas.GetArrayElementAtIndex(i).stringValue;
            Exigir(string.IsNullOrEmpty(anteriorNombre) || anteriorNombre == nombre, "Capa privada libre: " + i);
            capas.GetArrayElementAtIndex(i).stringValue = nombre;
        }
        serializadoTags.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(tags);
        Camera cam1 = (Camera)config.FindProperty("camara1").objectReferenceValue;
        Camera cam2 = (Camera)config.FindProperty("camara2").objectReferenceValue;
        EfectosJugador e1 = p1.GetComponent<EfectosJugador>();
        EfectosJugador e2 = p2.GetComponent<EfectosJugador>();
        var preparar = typeof(SistemaCartas).GetMethod("PrepararIlusiones", BindingFlags.NonPublic | BindingFlags.Instance);
        preparar.Invoke(sistema, new object[] { p1, e1, cam1, cam2, 8 });
        preparar.Invoke(sistema, new object[] { p2, e2, cam2, cam1, 9 });
        Exigir(LayerMask.NameToLayer("FantasmasP1") == 8 && LayerMask.NameToLayer("FantasmasP2") == 9,
            "Capas privadas reservadas");
        Exigir((cam1.cullingMask & (1 << 8)) != 0 && (cam1.cullingMask & (1 << 9)) == 0 &&
            (cam2.cullingMask & (1 << 9)) != 0 && (cam2.cullingMask & (1 << 8)) == 0,
            "Cada camara ve solo sus propios fantasmas");
        p2.Recibir(normal1); p2.Recibir(normal2);
        var anterior = new System.Collections.Generic.List<DefinicionCarta>(p2.Cartas);
        float puntos = p2.Almas.Cantidad;
        typeof(SistemaCartas).GetMethod("AplicarCarta", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(sistema, new object[] { p1, confusion });
        Exigir(e2.EstaConfundido && !e1.EstaConfundido && p2.EstaConfundido, "Confusion afecta rival y mano");
        bool cambiada = false;
        for (int i = 0; i < anterior.Count; i++) cambiada |= anterior[i] != p2.Cartas[i];
        Exigir(cambiada && anterior.Count == p2.Cantidad, "Mezcla cambia orden sin perder cartas");
        var orden = new System.Collections.Generic.List<DefinicionCarta>(p2.Cartas);
        e2.AplicarConfusion(potente.DuracionEfecto);
        float finPotente = (float)LeerCampo(e2, "finConfusion");
        e2.AplicarConfusion(confusion.DuracionEfecto);
        Exigir((float)LeerCampo(e2, "finConfusion") == finPotente, "Normal no acorta potente");
        for (int i = 0; i < orden.Count; i++) Exigir(orden[i] == p2.Cartas[i], "Renovar no vuelve a mezclar");
        var ilusiones2 = p2.GetComponent<IlusionesConfusionJugador>();
        var generador = (GeneradorEstudiantes)LeerCampo(ilusiones2, "generador");
        int estudiantesReales = generador.CantidadActiva;
        var tick = typeof(IlusionesConfusionJugador).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        tick.Invoke(ilusiones2, null);
        Exigir(ilusiones2.Cantidad == 24, "Pantalla llena de 24 estudiantes falsos");
        Exigir(generador.CantidadActiva == estudiantesReales, "Fantasmas no ocupan cupos del spawn real");
        var fantasmas = (System.Collections.IList)LeerCampo(ilusiones2, "fantasmas");
        object fantasma = fantasmas[0];
        GameObject objeto = (GameObject)fantasma.GetType().GetField("objeto").GetValue(fantasma);
        SpriteRenderer visual = objeto.GetComponentInChildren<SpriteRenderer>();
        Exigir(visual.color.a == 0f && !e2.IntentarDisiparFantasma(objeto.transform.position, 0.01f),
            "Fantasma nace invisible y no se absorbe antes de aparecer");
        var avanzar = typeof(IlusionesConfusionJugador).GetMethod("ActualizarFantasmas", BindingFlags.NonPublic | BindingFlags.Instance);
        BoxCollider2D zona = (BoxCollider2D)LeerCampo(ilusiones2, "zona");
        avanzar.Invoke(ilusiones2, new object[] { 0.175f, true, zona.bounds.min.x, zona.bounds.max.x });
        Exigir(visual.color.a > 0f && visual.color.a < 1f, "Fade in gradual");
        avanzar.Invoke(ilusiones2, new object[] { 0.175f, true, zona.bounds.min.x, zona.bounds.max.x });
        Exigir(objeto.layer == 9 && objeto.GetComponent<AlmaEstudiante>() == null &&
            objeto.GetComponent<Collider2D>() == null, "Fantasma solo visual, sin cupos ni collider");
        Exigir(e2.IntentarDisiparFantasma(objeto.transform.position, 0.01f) &&
            p2.Almas.Cantidad == puntos && !p2.GetComponent<EstadoCombateJugador>().EstaAturdido,
            "Interactuar con falso no entrega puntos");
        avanzar.Invoke(ilusiones2, new object[] { 0.1f, true, zona.bounds.min.x, zona.bounds.max.x });
        Exigir(objeto != null && visual.color.a > 0f && visual.color.a < 1f, "Consumir usa fade out sin salto de alfa");
        Exigir(!e1.IntentarDisiparFantasma(objeto.transform.position, 100f), "Otro jugador no interactua con fantasmas ajenos");
        e1.AplicarConfusion(10f);
        var ilusiones1 = p1.GetComponent<IlusionesConfusionJugador>();
        tick.Invoke(ilusiones1, null);
        Exigir(ilusiones1.Cantidad == 24, "Ambos jugadores pueden estar confundidos a la vez");
        Campo(e2, "finConfusion", Time.time - 1f);
        tick.Invoke(ilusiones2, null);
        avanzar.Invoke(ilusiones2, new object[] { 0.4f, false, zona.bounds.min.x, zona.bounds.max.x });
        Exigir(!p2.EstaConfundido && ilusiones2.Cantidad == 0 && ilusiones1.Cantidad == 24,
            "Expirar limpia solo ilusiones propias y revela mano");
        e1.Limpiar(); e2.Limpiar();
        Exigir(ilusiones1.Cantidad == 0 && ilusiones2.Cantidad == 0, "Limpieza total");
    }

    private static void VerificarRepartos(SistemaCartas sistema, CicloPartida partida,
        ManoCartasJugador p1, ManoCartasJugador p2, DefinicionCarta normal1, DefinicionCarta normal2,
        DefinicionCarta potente1, DefinicionCarta potente2)
    {
        var evaluar = typeof(SistemaCartas).GetMethod("EvaluarRepartosProgramados", BindingFlags.NonPublic | BindingFlags.Instance);
        var cancelar = typeof(SistemaCartas).GetMethod("CancelarPresentaciones", BindingFlags.NonPublic | BindingFlags.Instance);
        var mano1 = (System.Collections.Generic.List<DefinicionCarta>)LeerCampo(p1, "cartas");
        var mano2 = (System.Collections.Generic.List<DefinicionCarta>)LeerCampo(p2, "cartas");
        var realizados = (System.Collections.Generic.List<int>)LeerCampo(sistema, "repartosRealizados");
        mano1.Clear(); mano2.Clear(); realizados.Clear();
        cancelar.Invoke(sistema, null);
        Propiedad(partida, "Terminada", false);
        Propiedad(partida, "EnCurso", true);
        Campo(sistema, "catalogo", new[] { normal1, normal2 });
        Campo(sistema, "momentosReparto", new[] { 60f, 90f, 120f, 150f });
        float[] esperas = (float[])LeerCampo(sistema, "espera");
        esperas[0] = 17f; esperas[1] = 19f;
        Propiedad(partida, "TiempoRestante", 120.01f);
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 0 && p2.Cantidad == 0, "No repartir antes de 60 segundos");
        Propiedad(partida, "TiempoRestante", 120f);
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 1 && p2.Cantidad == 1 && !p1.Cartas[0].EsPotente && !p2.Cartas[0].EsPotente,
            "Reparto a ambos a los 60 segundos");
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 1 && p2.Cantidad == 1, "No repetir reparto en siguientes frames");
        p1.Recibir(normal1); p1.Recibir(normal1);
        Propiedad(partida, "TiempoRestante", 90f);
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 3 && p2.Cantidad == 2, "Reparto 90 omite solo la mano llena");
        mano1.Clear();
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 0, "Liberar mano despues no recupera entrega omitida");
        mano2.Clear();
        Campo(sistema, "catalogo", new[] { potente1, potente2 });
        Propiedad(partida, "TiempoRestante", 60f);
        evaluar.Invoke(sistema, null);
        Exigir(p1.TienePotente && p2.TienePotente && p1.Cantidad == 0 && p2.Cantidad == 0,
            "Reparto 120 reserva potentes del mismo nivel y conserva su animacion");
        cancelar.Invoke(sistema, null);
        evaluar.Invoke(sistema, null);
        Exigir(p1.EspaciosOcupados == 0 && p2.EspaciosOcupados == 0, "No repetir reparto 120");
        Propiedad(partida, "TiempoRestante", 30f);
        evaluar.Invoke(sistema, null);
        Exigir(p1.TienePotente && p2.TienePotente && realizados.Count == 4, "Cuarto reparto a los 150 segundos");
        Exigir(esperas[0] == 17f && esperas[1] == 19f, "Repartos no alteran cooldown de ayuda por desventaja");
        cancelar.Invoke(sistema, null);
        realizados.Clear();
        Campo(sistema, "catalogo", new[] { normal1, normal2 });
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 3 && p2.Cantidad == 3 && realizados.Count == 4,
            "Saltar el reloj procesa eventos cruzados una vez sin superar capacidad");
        mano1.Clear(); mano2.Clear();
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 0 && p2.Cantidad == 0, "No hay deuda tras salto temporal y manos llenas");
        Campo(sistema, "catalogo", new[] { normal1, normal2, potente1, potente2 });
        Exigir(p1.Recibir(potente1), "Preparar mano con potente");
        typeof(SistemaCartas).GetMethod("RepartirAmbos", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(sistema, null);
        Exigir(p1.Cantidad == 2 && !p1.Cartas[1].EsPotente && p2.Cantidad == 1 && !p2.Cartas[0].EsPotente,
            "Si uno ya tiene potente, ambos reciben normales");
        mano1.Clear(); mano2.Clear();
        realizados.Clear();
        Propiedad(partida, "Terminada", true);
        evaluar.Invoke(sistema, null);
        Exigir(p1.Cantidad == 0 && p2.Cantidad == 0, "No repartir despues del final");
    }

    private static void VerificarSeguimiento(UnityEngine.SceneManagement.Scene escena)
    {
        int verificadas = 0;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (CamaraPasillo seguimiento in raiz.GetComponentsInChildren<CamaraPasillo>(true))
            {
                Transform jugador = (Transform)new SerializedObject(seguimiento).FindProperty("jugador").objectReferenceValue;
                BoxCollider2D zona = jugador.GetComponent<MovimientoJugador2D>().ZonaMovimiento;
                Camera camara = seguimiento.GetComponent<Camera>();
                Vector3 escalaOriginal = zona.transform.localScale;
                Vector3 posicionOriginal = jugador.position;
                try
                {
                    float mitadVista = camara.orthographicSize * camara.aspect;
                    zona.transform.localScale = new Vector3(200f, escalaOriginal.y, escalaOriginal.z);
                    Physics2D.SyncTransforms();
                    float spawnLejano = zona.bounds.center.x + mitadVista * 2f;
                    jugador.position = new Vector3(spawnLejano, posicionOriginal.y, posicionOriginal.z);
                    seguimiento.ActualizarSeguimiento();
                    Exigir(Mathf.Approximately(camara.transform.position.x, spawnLejano),
                        seguimiento.name + " sigue un spawn lejano con zona ampliada");
                    jugador.position = new Vector3(zona.bounds.max.x + 100f, posicionOriginal.y, posicionOriginal.z);
                    seguimiento.ActualizarSeguimiento();
                    Exigir(Mathf.Approximately(camara.transform.position.x, zona.bounds.max.x - mitadVista),
                        seguimiento.name + " respeta el nuevo borde del mapa");
                    verificadas++;
                }
                finally
                {
                    jugador.position = posicionOriginal;
                    zona.transform.localScale = escalaOriginal;
                    Physics2D.SyncTransforms();
                    seguimiento.ActualizarSeguimiento();
                }
            }
        }
        Exigir(verificadas == 2, "Seguimiento comprobado para P1 y P2");
    }

    private static void Propiedad(object objeto, string nombre, object valor) =>
        objeto.GetType().GetProperty(nombre).SetValue(objeto, valor);
    private static void Campo(object objeto, string nombre, object valor) =>
        objeto.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(objeto, valor);
    private static object LeerCampo(object objeto, string nombre) =>
        objeto.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(objeto);
    private static void Evaluar(SistemaCartas sistema, ManoCartasJugador p1, ManoCartasJugador p2) =>
        typeof(SistemaCartas).GetMethod("EvaluarEntrega", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(sistema, new object[] { 0, p1, p2 });
}
