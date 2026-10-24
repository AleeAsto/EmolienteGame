// Reloj explicito para probar renovacion y expiracion sin depender de frames de Unity.
public sealed class ModificadoresVelocidad
{
    private float buff = 1f;
    private float debuff = 1f;
    private float finBuff;
    private float finDebuff;

    public void Aplicar(float multiplicador, float duracion, float ahora)
    {
        if (float.IsNaN(multiplicador) || float.IsInfinity(multiplicador) ||
            float.IsNaN(duracion) || float.IsInfinity(duracion) || multiplicador <= 0f || duracion <= 0f)
            return;
        // Una instancia de cada tipo: repetir renueva, sin acumular copias.
        if (multiplicador > 1f)
        {
            if (ahora >= finBuff || multiplicador != buff) finBuff = ahora + duracion;
            else finBuff = System.Math.Max(finBuff, ahora + duracion);
            buff = multiplicador;
        }
        else if (multiplicador < 1f)
        {
            if (ahora >= finDebuff || multiplicador != debuff) finDebuff = ahora + duracion;
            else finDebuff = System.Math.Max(finDebuff, ahora + duracion);
            debuff = multiplicador;
        }
    }

    public float Multiplicador(float ahora) =>
        (ahora < finBuff ? buff : 1f) * (ahora < finDebuff ? debuff : 1f);

    public void Limpiar()
    {
        buff = debuff = 1f;
        finBuff = finDebuff = 0f;
    }
}
