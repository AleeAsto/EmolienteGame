using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerificarZonasDano
{
    private const string Solicitud = "Temp/VerificarZonasDano.request";

    [InitializeOnLoadMethod]
    private static void AlRecargar()
    {
        if (File.Exists(Solicitud)) EditorApplication.delayCall += EjecutarSolicitud;
    }

    private static void EjecutarSolicitud()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += EjecutarSolicitud;
            return;
        }
        File.Delete(Solicitud);
        Verificar();
    }

    [MenuItem("Emoliente/Verificar zonas de dano")]
    public static void Verificar()
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var objeto = new GameObject("PruebaZonaDano");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(objeto, escena);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(1f, 0.4f);
            caja.offset = new Vector2(0f, -2f);
            var zona = objeto.AddComponent<ZonaDanoJugador>();
            var datos = new SerializedObject(zona);
            datos.FindProperty("caja").objectReferenceValue = caja;
            datos.ApplyModifiedPropertiesWithoutUndo();
            Exigir(!zona.IntersectaRectangulo(Vector2.zero, Vector2.one), "El torso no activa la caja de los pies");
            Exigir(zona.IntersectaRectangulo(new Vector2(0f, -2f), Vector2.one), "Contacto real en los pies");
            Exigir(!zona.IntersectaRectangulo(new Vector2(0f, -1f), new Vector2(1f, 0.4f)), "Se puede pasar por otra fila");
            Exigir(!zona.IntersectaElipse(Vector2.zero, Vector2.one), "Latigo fuera de alcance");
            Exigir(zona.IntersectaElipse(new Vector2(0f, -2f), new Vector2(0.1f, 0.1f)), "Elipse contenida en caja");
            Exigir(zona.IntersectaElipse(new Vector2(0.6f, -2f), new Vector2(0.2f, 0.1f)), "Latigo toca el borde sin alcanzar el centro");
            Exigir(!zona.IntersectaElipse(new Vector2(0.8f, -2f), new Vector2(0.2f, 0.1f)), "Latigo separado del borde");
            objeto.transform.position = new Vector3(4f, 1f, 0f);
            Exigir(zona.IntersectaRectangulo(new Vector2(4f, -1f), Vector2.one), "Movimiento por Transform sin retraso fisico");
            caja.enabled = false;
            Exigir(!zona.IntersectaRectangulo(new Vector2(4f, -1f), Vector2.one), "Caja desactivada no recibe impactos");
            caja.enabled = true;
            objeto.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Exigir(zona.IntersectaRectangulo(zona.Centro, Vector2.one), "Offset respeta rotacion");
            File.WriteAllText("Temp/VerificarZonasDano.resultado.txt", "OK: 10 comprobaciones de contacto, offset, movimiento y latigo.");
            Debug.Log("Zonas de dano: 10 comprobaciones correctas.");
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/VerificarZonasDano.resultado.txt", error.ToString());
            throw;
        }
        finally { EditorSceneManager.ClosePreviewScene(escena); }
    }

    private static void Exigir(bool condicion, string caso)
    {
        if (!condicion) throw new InvalidOperationException(caso);
    }
}
