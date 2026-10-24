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
        var escena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ActionCards.unity");
        try
        {
            AssetDatabase.ImportAsset("Assets/Cartas/CartaAceleracion.asset", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Cartas/CartaPesadez.asset", ImportAssetOptions.ForceUpdate);
            DefinicionCarta carta = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaAceleracion.asset");
            DefinicionCarta pesadez = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaPesadez.asset");
            Exigir(carta != null && carta.Frente != null && carta.Reverso != null, "Definicion y ambos sprites validos");
            Exigir(pesadez != null && pesadez.Frente != null && pesadez.Reverso != null, "Sprites de Pesadez validos");
            Exigir(carta.PuntosNecesarios == 20f && pesadez.PuntosNecesarios == 20f &&
                carta.DuracionEfecto == 8f && pesadez.DuracionEfecto == 8f,
                "Cartas normales: costo 20 y duracion 8 segundos");
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
            Exigir(sistema != null, "Sistema presente en ActionCards");
            var serializado = new SerializedObject(sistema);
            foreach (string campo in new[] { "partida", "jugador1", "jugador2", "camara1", "camara2" })
                Exigir(serializado.FindProperty(campo).objectReferenceValue != null, "Referencia " + campo);
            var catalogo = serializado.FindProperty("catalogo");
            Exigir(catalogo.arraySize == 2 && catalogo.GetArrayElementAtIndex(0).objectReferenceValue == carta &&
                catalogo.GetArrayElementAtIndex(1).objectReferenceValue == pesadez,
                "Catalogo conectado a ambas habilidades");

            var partida = (CicloPartida)serializado.FindProperty("partida").objectReferenceValue;
            var p1 = (ManoCartasJugador)serializado.FindProperty("jugador1").objectReferenceValue;
            var p2 = (ManoCartasJugador)serializado.FindProperty("jugador2").objectReferenceValue;
            p1.Inicializar(sistema);
            p2.Inicializar(sistema);
            Exigir(!p1.Recibir(carta), "No entregar antes del inicio");
            Propiedad(partida, "EnCurso", true);
            Propiedad(partida, "TiempoRestante", 180f);
            Exigir(sistema.EnCurso, "Ronda simulada habilitada en preview");
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
            Exigir(Mathf.Approximately(efectos1.MultiplicadorVelocidad, 1.4f) &&
                Mathf.Approximately(efectos2.MultiplicadorVelocidad, 1f), "Buff afecta solo al lanzador");
            aplicar.Invoke(sistema, new object[] { p1, pesadez });
            aplicar.Invoke(sistema, new object[] { p1, pesadez });
            Exigir(Mathf.Approximately(efectos2.MultiplicadorVelocidad, 0.6f), "Debuff afecta rival y no acumula copias");
            aplicar.Invoke(sistema, new object[] { p2, carta });
            Exigir(Mathf.Approximately(efectos2.MultiplicadorVelocidad, 0.84f), "Buff y debuff combinados");
            efectos1.Limpiar();
            efectos2.Limpiar();
            Exigir(efectos1.MultiplicadorVelocidad == 1f && efectos2.MultiplicadorVelocidad == 1f, "Limpieza de efectos");

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
            Propiedad(partida, "Terminada", true);
            Exigir(!p1.Recibir(carta), "No entregar despues del final");

            VerificarSeguimiento(escena);
            const string mensaje = "OK: cartas, cobro, efectos y seguimiento de ambas camaras con spawn lejano y mapa ampliado. No prueba input ni animacion en Play.";
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
