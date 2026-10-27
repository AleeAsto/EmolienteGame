using UnityEngine;

public enum DificultadCPU { Facil, Normal, Dificil }

// Se conserva en revancha, pero se reinicia al entrar en Play o abrir otro modo.
public static class ConfiguracionPartida
{
    public static bool ContraCPU { get; private set; }
    public static DificultadCPU Dificultad { get; private set; } = DificultadCPU.Normal;
    public static string NombreRival => ContraCPU ? "CPU - " + NombreDificultad(Dificultad) : "JUGADOR 2";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ElegirPvP()
    {
        ContraCPU = false;
        Dificultad = DificultadCPU.Normal;
    }

    public static void ElegirCPU(DificultadCPU dificultad)
    {
        ContraCPU = true;
        Dificultad = dificultad;
    }

    public static string NombreDificultad(DificultadCPU dificultad) =>
        dificultad == DificultadCPU.Facil ? "FACIL" : dificultad == DificultadCPU.Dificil ? "DIFICIL" : "NORMAL";

    public static string ResultadoCPU(int ganador)
    {
        string resultado = ganador == 1 ? "VENCISTE A LA CPU" :
            ganador == 2 ? "GANO LA CPU" : "EMPATE CONTRA CPU";
        return resultado + " - " + NombreDificultad(Dificultad);
    }
}
