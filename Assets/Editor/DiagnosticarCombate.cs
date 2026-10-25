using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DiagnosticarCombate
{
    private static double proximaConsulta;
    private static double trazarHasta;
    private static string anterior;
    static DiagnosticarCombate() => EditorApplication.update += Actualizar;

    private static void Actualizar()
    {
        const string solicitud = "Temp/DiagnosticarCombate.request";
        const string resultado = "Temp/DiagnosticarCombate.resultado.txt";
        if (EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < trazarHasta)
        {
            string actual = Capturar();
            if (actual != anterior) File.AppendAllText(resultado, actual + "\n");
            anterior = actual;
        }
        if (EditorApplication.timeSinceStartup < proximaConsulta) return;
        proximaConsulta = EditorApplication.timeSinceStartup + 0.2;
        if (!File.Exists(solicitud)) return;
        string comando = File.ReadAllText(solicitud).Trim();
        File.Delete(solicitud);
        try
        {
            if (comando == "reparar" && !EditorApplication.isPlaying)
            {
                var actores = Actores();
                var p1 = actores.Single(a => a.name == "P1");
                var p2 = actores.Single(a => a.name == "P2");
                var combate = Resources.FindObjectsOfTypeAll<CombatePvP>().Single(c =>
                    !EditorUtility.IsPersistent(c) && c.gameObject.scene == p1.gameObject.scene);
                Undo.RecordObject(combate, "Reparar referencias del combate PvP");
                var datos = new SerializedObject(combate);
                datos.FindProperty("jugador1").objectReferenceValue = p1;
                datos.FindProperty("jugador2").objectReferenceValue = p2;
                datos.ApplyModifiedProperties();
                combate.enabled = true;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(combate.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(combate.gameObject.scene);
            }
            File.WriteAllText(resultado, Capturar());
            if (EditorApplication.isPlaying && (comando == "probar1" || comando == "probar2"))
            {
                var ataque = Actores().First(a => a.name == (comando == "probar1" ? "P1" : "P2"));
                File.AppendAllText(resultado, "\nIntentarAtacar: " + ataque.IntentarAtacar() + "\n");
                trazarHasta = EditorApplication.timeSinceStartup + 0.5;
                anterior = null;
            }
        }
        catch (Exception error) { File.AppendAllText(resultado, error.ToString()); }
    }

    private static AtaqueJugador[] Actores() => Resources.FindObjectsOfTypeAll<AtaqueJugador>()
        .Where(a => !EditorUtility.IsPersistent(a) && a.gameObject.scene.IsValid() &&
            !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(a.gameObject.scene)).ToArray();

    private static object Campo(object objeto, string nombre) => objeto.GetType()
        .GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(objeto);

    private static string Capturar()
    {
        var texto = new StringBuilder();
        texto.AppendLine($"Play={EditorApplication.isPlaying} timeScale={Time.timeScale} frame={Time.frameCount}");
        var actores = Actores();
        foreach (var a in actores)
        {
            var e = a.GetComponent<EstadoCombateJugador>();
            var pies = a.GetComponent<BoxCollider2D>();
            texto.AppendLine($"{a.name}: enabled={a.isActiveAndEnabled}, cooldown={Campo(a, "cooldown")}, duracion={Campo(a, "duracionGolpe")}, prep={Campo(a, "preparacionGolpe")}, recarga={a.RecargaRestante}, ataque={a.EstaAtacando}, puedeImpactar={a.PuedeImpactar}, direccion={a.Direccion}, tiempo={Campo(a, "tiempoAtaque")}, stun={e.EstaAturdido}, invulnerable={e.EsInvulnerable}");
            texto.AppendLine($"pies={pies.transform.TransformPoint(pies.offset):F3}, zona={e.ZonaDano}, disponible={e.ZonaDano != null && e.ZonaDano.Disponible}");
            if (e.ZonaDano != null) texto.AppendLine($"centroDano={e.ZonaDano.Centro:F3} caja={Campo(e.ZonaDano, "caja")}");
            if (EditorApplication.isPlaying)
                foreach (var otro in actores.Where(o => o != a))
                    texto.AppendLine($"{a.name} -> {otro.name}: EnAlcance={a.EnAlcance(otro)}");
        }
        foreach (var c in Resources.FindObjectsOfTypeAll<CombatePvP>().Where(c=>!EditorUtility.IsPersistent(c) && c.gameObject.scene.IsValid()))
            texto.AppendLine($"Combate enabled={c.isActiveAndEnabled} P1={Campo(c,"jugador1")} P2={Campo(c,"jugador2")}");
        return texto.ToString();
    }
}
