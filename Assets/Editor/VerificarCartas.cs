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
            DefinicionCarta carta = AssetDatabase.LoadAssetAtPath<DefinicionCarta>("Assets/Cartas/CartaPrueba.asset");
            Exigir(carta != null && carta.Frente != null && carta.Reverso != null, "Definicion y ambos sprites validos");
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
            Exigir(catalogo.arraySize > 0 && catalogo.GetArrayElementAtIndex(0).objectReferenceValue == carta,
                "Catalogo conectado a la carta de prueba");

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
            Exigir(!p1.IntentarUsar() && p1.Cantidad == 3 && p1.MenuAbierto, "Rechazar por puntos insuficientes");
            p1.Almas.Recibir(carta.PuntosNecesarios);
            float puntos = p1.Almas.Cantidad;
            Exigir(p1.IntentarUsar() && p1.Cantidad == 2 && !p1.MenuAbierto, "Consumir una carta y cerrar menu");
            Exigir(p1.Almas.Cantidad == puntos, "Usar sin descontar puntos");
            Exigir(!p1.IntentarUsar() && p1.Cantidad == 2, "No usar fuera del menu");
            Propiedad(p1, "MenuAbierto", true);
            p1.Cerrar();
            Exigir(p1.Cantidad == 2 && !p1.MenuAbierto, "Cerrar sin consumir");

            // Acelera unicamente el preview para comprobar entrega, espera y empate.
            Campo(sistema, "inicioFraccionPartida", 0f);
            Campo(sistema, "persistencia", 0f);
            p2.Almas.Recibir(100f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 3, "Entrega al jugador en desventaja");
            Propiedad(p1, "MenuAbierto", true);
            Exigir(p1.IntentarUsar(), "Liberar espacio durante espera");
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "Espera impide entrega inmediata");
            ((float[])LeerCampo(sistema, "espera"))[0] = 0f;
            p1.Almas.Recibir(60f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "Empate no entrega cartas");
            p1.Almas.Perder(60f);
            Propiedad(partida, "TiempoRestante", 10f);
            Evaluar(sistema, p1, p2);
            Exigir(p1.Cantidad == 2, "No entregar en ultimos diez segundos");
            Propiedad(partida, "Terminada", true);
            Exigir(!p1.Recibir(carta), "No entregar despues del final");

            const string mensaje = "OK: sprites, referencias, inventario, requisito, consumo sin gasto, cierre, desventaja, espera, empate y cierre de entregas. No prueba input ni animacion en Play.";
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
