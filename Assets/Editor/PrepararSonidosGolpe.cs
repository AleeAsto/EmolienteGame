using System;
using UnityEditor;
using UnityEngine;

public static class PrepararSonidosGolpe
{
    [MenuItem("Emoliente/Configurar sonidos de golpe separados")]
    public static void Configurar()
    {
        AssetDatabase.Refresh();
        var config = AssetDatabase.LoadAssetAtPath<ConfiguracionAudio>("Assets/Resources/AudioJuego.asset");
        var variantes = new AudioClip[5];
        for (int i = 0; i < variantes.Length; i++)
        {
            string ruta = $"Assets/Audio/SFX/Golpes/Golpe{i + 1:00}.wav";
            variantes[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (variantes[i] == null) throw new InvalidOperationException("Falta la variante de golpe: " + ruta);
            var importador = (AudioImporter)AssetImporter.GetAtPath(ruta);
            var ajustes = importador.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.preloadAudioData = true;
            importador.defaultSampleSettings = ajustes;
            importador.SaveAndReimport();
        }
        config.variantesGolpe = variantes;
        // La referencia individual tambien debe contener un solo impacto.
        config.golpe = variantes[0];
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }
}
