using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AjustarPuertas
{
    static AjustarPuertas() => EditorApplication.update += Consultar;
    private static void Consultar()
    {
        const string solicitud = "Temp/AjustarPuertas.request";
        if (EditorApplication.isCompiling || !File.Exists(solicitud)) return;
        try
        {
            string comando = File.ReadAllText(solicitud).Trim();
            if (comando == "verificar" && !EditorApplication.isPlaying) return;
            if (comando == "verificar" && Time.timeScale <= 0f) return;
            if (comando != "verificar" && EditorApplication.isPlaying) return;
            File.Delete(solicitud);
            if (comando == "verificar") Verificar();
            else Ajustar();
        }
        catch (Exception error) { File.WriteAllText("Temp/AjustarPuertas.resultado.txt", error.ToString()); }
    }

    private static void Verificar()
    {
        var anfitrion = UnityEngine.Object.FindObjectsByType<UsarPuertaJugador>(FindObjectsSortMode.None).First();
        File.WriteAllText("Temp/AjustarPuertas.resultado.txt", "Verificacion de animacion en curso...");
        anfitrion.StartCoroutine(VerificarAnimacion());
    }

    private static System.Collections.IEnumerator VerificarAnimacion()
    {
        var jugadores = UnityEngine.Object.FindObjectsByType<UsarPuertaJugador>(FindObjectsSortMode.None);
        var puertas = UnityEngine.Object.FindObjectsByType<PuertaTeletransporte>(FindObjectsSortMode.None);
        var informe = new StringBuilder();
        foreach (var jugador in jugadores)
        {
            var movimiento = jugador.GetComponent<MovimientoJugador2D>();
            Vector3 origen = jugador.transform.position;
            var datos = new SerializedObject(jugador);
            var red = (RedPuertas)datos.FindProperty("red").objectReferenceValue;
            foreach (var puerta in puertas) puerta.ActivarCooldown(0f);
            Vector3 diferenciaPies = jugador.PosicionPies - jugador.transform.position;
            movimiento.Teletransportar(puertas[0].PosicionSalida - diferenciaPies);
            Transform visual = jugador.transform.Find("Sprite");
            Vector3 escalaOriginal = visual.localScale;
            if (!red.IntentarTransportar(jugador)) throw new Exception(jugador.name + ": no puede entrar.");
            var destino = puertas.Single(p => !p.Disponible);
            float duracionEntrada = datos.FindProperty("duracionEntrada").floatValue;
            yield return new WaitForSeconds(duracionEntrada * 0.6f);
            if (!jugador.EstaViajando || visual.localScale.magnitude >= escalaOriginal.magnitude)
                throw new Exception(jugador.name + ": no se encoge al entrar.");
            Color tinte = visual.GetComponent<SpriteRenderer>().color;
            if (tinte.r + tinte.g + tinte.b > 0.3f) throw new Exception(jugador.name + ": no se oscurece.");
            yield return new WaitForSeconds(duracionEntrada * 0.5f);
            var flash = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Single(c => c.name == "FlashPuerta_" + jugador.name);
            var camara = (Camera)new SerializedObject(jugador).FindProperty("camaraJugador").objectReferenceValue;
            RectTransform pantalla = (RectTransform)flash.transform.GetChild(0);
            if (pantalla.anchorMin != camara.rect.min || pantalla.anchorMax != camara.rect.max)
                throw new Exception(jugador.name + ": el flash no respeta su pantalla.");
            while (jugador.EstaViajando) yield return null;
            if ((visual.localScale - escalaOriginal).sqrMagnitude > 0.0001f)
                throw new Exception(jugador.name + ": no restaura su escala.");
            float error = Vector2.Distance(jugador.PosicionPies, destino.PosicionSalida);
            if (error > 0.1f) throw new Exception($"{jugador.name}: salida incorrecta, error={error}; pies={jugador.PosicionPies}; salida={destino.PosicionSalida}");
            var datosPuerta = new SerializedObject(destino);
            var panel = (SpriteRenderer)datosPuerta.FindProperty("panel").objectReferenceValue;
            if (panel.sprite != datosPuerta.FindProperty("spriteCerrada").objectReferenceValue)
                throw new Exception(destino.name + ": no se cierra al llegar.");
            destino.ActivarCooldown(0f);
            if (panel.sprite != datosPuerta.FindProperty("spriteAbierta").objectReferenceValue)
                throw new Exception(destino.name + ": no reabre al terminar cooldown.");
            informe.AppendLine($"OK {jugador.name}: encoge, oscurece, flash exclusivo, restaura visual y llega a {destino.name}; destino cierra y reabre.");
            movimiento.Teletransportar(origen);
        }
        File.WriteAllText("Temp/AjustarPuertas.resultado.txt", informe.ToString());
    }

    [MenuItem("Emoliente/Alinear accesos de puertas")]
    public static void Ajustar()
    {
        var puertas = Resources.FindObjectsOfTypeAll<PuertaTeletransporte>().Where(p =>
            !EditorUtility.IsPersistent(p) && p.gameObject.scene.IsValid() &&
            !EditorSceneManager.IsPreviewScene(p.gameObject.scene)).ToArray();
        var informe = new StringBuilder();
        foreach (var puerta in puertas)
        {
            Undo.RegisterFullObjectHierarchyUndo(puerta.gameObject, "Alinear accesos de puerta");
            puerta.AjustarAccesos();
            var datos = new SerializedObject(puerta);
            var panel = (SpriteRenderer)datos.FindProperty("panel").objectReferenceValue;
            if (panel == null) throw new Exception(puerta.name + ": falta el sprite.");
            if (datos.FindProperty("spriteAbierta").objectReferenceValue == null ||
                datos.FindProperty("spriteCerrada").objectReferenceValue == null)
                throw new Exception(puerta.name + ": faltan sprites de estado.");
            informe.AppendLine($"{puerta.name}: base sprite={panel.bounds.min.y:F2}; salida={puerta.PosicionSalida}; entrada={puerta.GetComponent<BoxCollider2D>().bounds}");
            EditorSceneManager.MarkSceneDirty(puerta.gameObject.scene);
        }
        foreach (var escena in puertas.Select(p => p.gameObject.scene).Distinct()) EditorSceneManager.SaveScene(escena);
        File.WriteAllText("Temp/AjustarPuertas.resultado.txt", informe.ToString());
    }
}
