using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerificarVictoria
{
    private static double validarDesde = -1;
    private static int ganador;
    private static AudioSource musicaPrueba;
    private static AudioClip clipAnterior;
    private static float tiempoAnterior, volumenAnterior;
    private static bool comprobarAudio, bajoVolumen, reinicioAudio, subioVolumen;
    private static double limiteAudio;
    private const string Solicitud = "Temp/VerificarVictoria.request";
    private const string Resultado = "Temp/VerificarVictoria.resultado.txt";
    static VerificarVictoria() => EditorApplication.update += Consultar;
    private static void Consultar()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (comprobarAudio) SeguirAudio();
            if (validarDesde >= 0 && EditorApplication.timeSinceStartup >= validarDesde)
            {
                validarDesde = -1;
                Validar();
            }
            if (!File.Exists(Solicitud)) return;
            string comando = File.ReadAllText(Solicitud).Trim();
            if (comando == "revancha" || comando == "revanchaAudio" || comando == "menu")
            {
                string nombre = comando == "menu" ? "MenuPrincipal" : "Revancha";
                Button boton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Single(b => b.name == nombre && b.transform.parent.name == "ResultadoVictoria");
                File.Delete(Solicitud);
                if (comando == "revanchaAudio")
                {
                    musicaPrueba = AudioJuego.Instancia.GetComponentsInChildren<AudioSource>().Single(a => a.name == "Musica");
                    clipAnterior = musicaPrueba.clip;
                    tiempoAnterior = musicaPrueba.time;
                    volumenAnterior = musicaPrueba.volume;
                    bajoVolumen = reinicioAudio = subioVolumen = false;
                    comprobarAudio = true;
                    limiteAudio = EditorApplication.timeSinceStartup + 20f;
                }
                boton.onClick.Invoke();
                File.WriteAllText(Resultado, "OK: boton " + nombre + " invocado; esperando cambio de escena.");
                return;
            }
            CicloPartida partida = UnityEngine.Object.FindObjectsByType<CicloPartida>(FindObjectsSortMode.None).FirstOrDefault();
            if (partida == null || !partida.EnCurso) return;
            File.Delete(Solicitud);
            ganador = comando == "p1" ? 1 : comando == "p2" ? 2 : 0;
            var datos = new SerializedObject(partida);
            for (int i = 1; i <= 2; i++)
            {
                var jugador = new SerializedObject(datos.FindProperty("jugador" + i).objectReferenceValue);
                jugador.FindProperty("cantidad").floatValue = ganador == i ? 50f : 0f;
                jugador.ApplyModifiedPropertiesWithoutUndo();
            }
            typeof(CicloPartida).GetProperty("TiempoRestante").SetValue(partida, 0f);
            validarDesde = EditorApplication.timeSinceStartup + 2.2;
        }
        catch (Exception error) { validarDesde = -1; File.WriteAllText(Resultado, error.ToString()); }
    }

    private static void SeguirAudio()
    {
        if (musicaPrueba == null) throw new Exception("Desaparecio la fuente musical.");
        float tiempo = musicaPrueba.time;
        float volumen = musicaPrueba.volume;
        if (!reinicioAudio && volumen < volumenAnterior - 0.001f) bajoVolumen = true;
        if (tiempo < tiempoAnterior - 0.05f) reinicioAudio = true;
        if (reinicioAudio && volumen > volumenAnterior + 0.001f) subioVolumen = true;
        tiempoAnterior = tiempo;
        volumenAnterior = volumen;
        if (bajoVolumen && reinicioAudio && subioVolumen && !TransicionEscena.EnCurso)
        {
            comprobarAudio = false;
            if (musicaPrueba.clip != clipAnterior) throw new Exception("Cambio de cancion en vez de reiniciar.");
            File.WriteAllText(Resultado, "OK revancha musical: fade out, reinicio desde cero del mismo clip y fade in.");
        }
        else if (EditorApplication.timeSinceStartup > limiteAudio)
        {
            comprobarAudio = false;
            throw new Exception($"Fallo audio: salida={bajoVolumen}, reinicio={reinicioAudio}, entrada={subioVolumen}.");
        }
    }

    private static void Validar()
    {
        var presentacion = UnityEngine.Object.FindObjectsByType<PresentacionVersus>(FindObjectsSortMode.None).Single();
        Canvas canvas = presentacion.GetComponentInChildren<Canvas>();
        Text titulo = canvas.GetComponentsInChildren<Text>().Single(t => t.name == "Winner");
        if (titulo.text != (ganador == 0 ? "EMPATE" : "¡VICTORIA!")) throw new Exception("Titulo incorrecto.");
        Button[] botones = canvas.GetComponentsInChildren<Button>();
        if (botones.Length != 2 || botones.Any(b => !b.IsInteractable())) throw new Exception("Faltan botones activos.");
        if (ganador > 0)
        {
            RectTransform panel = (RectTransform)canvas.transform.Find(ganador == 1 ? "MarcoMorado" : "MarcoRojo");
            if (Mathf.Abs(panel.rect.width - ((RectTransform)canvas.transform).rect.width) > 1f)
                throw new Exception("El marco ganador no llena la pantalla.");
            string ruta = AssetDatabase.GetAssetPath(panel.GetComponentsInChildren<Image>().First(i => i.name == "Retrato").sprite);
            if (!ruta.EndsWith(ganador == 1 ? "win2.jpg" : "win1.jpg")) throw new Exception("Imagen de victoria incorrecta.");
        }
        else
        {
            string[] marcos = { "MarcoMorado", "MarcoRojo" };
            string[] imagenes = { "empate2.jpg", "empate1.jpg" };
            for (int i = 0; i < 2; i++)
            {
                RectTransform panel = (RectTransform)canvas.transform.Find(marcos[i]);
                string ruta = AssetDatabase.GetAssetPath(panel.GetComponentsInChildren<Image>().First(im => im.name == "Retrato").sprite);
                if (!ruta.EndsWith(imagenes[i])) throw new Exception("Imagen de empate incorrecta.");
                if (Mathf.Abs(panel.rect.width * 2f - ((RectTransform)canvas.transform).rect.width) > 1f)
                    throw new Exception("Los marcos de empate no ocupan mitades iguales.");
            }
        }
        File.WriteAllText(Resultado, $"OK ganador={ganador}: marco completo, imagen correcta, titulo y dos botones activos.");
    }
}
