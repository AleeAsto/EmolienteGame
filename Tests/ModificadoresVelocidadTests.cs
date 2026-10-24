using System;

public static class ModificadoresVelocidadTests
{
    public static int Main()
    {
        var efectos = new ModificadoresVelocidad();
        Igual(efectos.Multiplicador(0f), 1f, "Inicial");
        efectos.Aplicar(0.5f, 6f, 0f);
        Igual(efectos.Multiplicador(5.99f), 0.5f, "Debuff activo");
        Igual(efectos.Multiplicador(6f), 1f, "Expira exactamente al finalizar");
        efectos.Aplicar(0.5f, 6f, 10f);
        efectos.Aplicar(0.5f, 6f, 14f);
        Igual(efectos.Multiplicador(16f), 0.5f, "Renueva sin multiplicar copias");
        Igual(efectos.Multiplicador(20f), 1f, "Expira renovacion");
        efectos.Aplicar(3f, 6f, 30f);
        efectos.Aplicar(0.5f, 6f, 32f);
        Igual(efectos.Multiplicador(34f), 1.5f, "Efectos opuestos se combinan");
        Igual(efectos.Multiplicador(36f), 0.5f, "Duraciones independientes");
        Igual(efectos.Multiplicador(38f), 1f, "Vuelve a velocidad base");
        efectos.Aplicar(3f, 6f, 40f);
        efectos.Aplicar(float.NaN, 6f, 41f);
        efectos.Aplicar(-1f, 6f, 41f);
        Igual(efectos.Multiplicador(41f), 3f, "Rechaza multiplicadores invalidos");
        efectos.Limpiar();
        Igual(efectos.Multiplicador(41f), 1f, "Limpieza al terminar o desactivar");
        efectos.Aplicar(3f, 20f, 50f);
        efectos.Aplicar(3f, 8f, 51f);
        Igual(efectos.Multiplicador(69f), 3f, "Normal no acorta potente activa");
        Igual(efectos.Multiplicador(70f), 1f, "Potente expira a los veinte segundos");
        efectos.Aplicar(0.5f, 20f, 80f);
        efectos.Aplicar(0.5f, 8f, 81f);
        Igual(efectos.Multiplicador(99f), 0.5f, "Debuff normal no acorta potente activa");
        Igual(efectos.Multiplicador(100f), 1f, "Debuff potente expira");
        Console.WriteLine("OK: 14 comprobaciones de expiracion, renovacion, potentes, combinacion y limpieza.");
        return 0;
    }

    private static void Igual(float actual, float esperado, string caso)
    {
        if (Math.Abs(actual - esperado) > 0.0001f)
            throw new Exception(caso + ": esperado " + esperado + ", obtenido " + actual);
    }
}
