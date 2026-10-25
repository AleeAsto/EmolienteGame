using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Emoliente/Configuracion de controles")]
public class ConfiguracionControles : ScriptableObject
{
    public const string ClaveGuardado = "ControlesVersus.v1";
    [SerializeField] private InputActionAsset acciones;
    public InputActionAsset Acciones => acciones;
    public static ConfiguracionControles Actual { get; private set; }
    public static event Action Cambiados;

    public sealed class Control
    {
        public readonly int Jugador, Fila;
        public readonly string Accion, BindingId;
        public readonly bool Alternativo;
        public Control(int jugador, int fila, string accion, string id, bool alternativo = false)
        { Jugador = jugador; Fila = fila; Accion = accion; BindingId = id; Alternativo = alternativo; }
    }

    public static readonly string[] Etiquetas = { "Arriba", "Abajo", "Izquierda", "Derecha", "Absorber / confirmar", "Golpear / cartas" };
    public static readonly Control[] Controles = {
        new Control(1,0,"Move","b47193cc-a387-43b3-90bd-6b307c7d0576"),
        new Control(1,1,"Move","ceafee82-0db8-4c1a-b4ac-e73eba9bd216"),
        new Control(1,2,"Move","63ef0551-fa08-4f88-b965-365ad943810d"),
        new Control(1,3,"Move","52cb6f45-4243-4c86-8e32-470288d5d876"),
        new Control(1,4,"Succionar","e203e6a5-b732-4781-8489-b63434cd28ee"),
        new Control(1,5,"Golpear","481f7f46-b0a4-419e-bc46-eeb6d5f88140"),
        new Control(2,0,"Move","647487b3-ddc8-4146-b425-a46a4fefd8d2"),
        new Control(2,1,"Move","034e2c4d-5cc7-4cf2-abf1-c776456f704a"),
        new Control(2,2,"Move","cff7d0cf-2876-4d86-9cd6-fbe748d087ed"),
        new Control(2,3,"Move","dcaced05-93a6-4d2a-884c-6c6b427dd633"),
        new Control(2,4,"Succionar","70d42753-713f-4394-9769-9a11357bf645"),
        new Control(2,5,"Golpear","6247e825-f2bc-4edd-974f-8ef9c2882c8e"),
        new Control(2,0,"Move","202029a2-c15b-402f-823e-25a2c7174856",true),
        new Control(2,1,"Move","ac0622ce-a1f5-47e2-97ef-54ada10b1bf2",true),
        new Control(2,2,"Move","c42eb5c4-a574-4024-8923-bf7c05d787b9",true),
        new Control(2,3,"Move","3fca0e42-af0b-454f-a263-549fc7ff0815",true)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { Actual = null; Cambiados = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        Actual = Resources.Load<ConfiguracionControles>("ConfiguracionControles");
        if (Actual == null || Actual.acciones == null)
        {
            Debug.LogError("Falta Resources/ConfiguracionControles con las Input Actions de los jugadores.");
            return;
        }
        Actual.CargarGuardado();
    }

    public InputAction Resolver(int indice, out int binding)
    {
        Control control = Controles[indice];
        var action = acciones.FindAction($"Jugador{control.Jugador}/{control.Accion}", true);
        for (int i = 0; i < action.bindings.Count; i++)
            if (action.bindings[i].id.ToString() == control.BindingId) { binding = i; return action; }
        throw new InvalidOperationException("No se encuentra el binding de " + Etiquetas[control.Fila]);
    }

    public string Ruta(int indice)
    {
        var action = Resolver(indice, out int binding);
        return action.bindings[binding].effectivePath;
    }

    public bool IntentarAsignar(int indice, string path, out string mensaje)
    {
        if (indice < 0 || indice >= Controles.Length) { mensaje = "Control no valido."; return false; }
        bool quitar = Controles[indice].Alternativo && string.Equals(path, "<Keyboard>/delete", StringComparison.OrdinalIgnoreCase);
        if (quitar) path = "";
        if ((!Controles[indice].Alternativo && string.IsNullOrEmpty(path)) ||
            (!string.IsNullOrEmpty(path) && (!path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/escape", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/anyKey", StringComparison.OrdinalIgnoreCase))))
        { mensaje = "Elige una tecla del teclado. Escape esta reservado para cancelar y pausar."; return false; }
        for (int i = 0; i < Controles.Length; i++)
        {
            if (i == indice || string.IsNullOrEmpty(path) || !string.Equals(Ruta(i), path, StringComparison.OrdinalIgnoreCase)) continue;
            mensaje = $"Ya la usa P{Controles[i].Jugador}: {Etiquetas[Controles[i].Fila]}.";
            return false;
        }
        var action = Resolver(indice, out int binding);
        action.ApplyBindingOverride(binding, path);
        PlayerPrefs.SetString(ClaveGuardado, acciones.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        Cambiados?.Invoke();
        mensaje = quitar ? "Alternativa eliminada. Tecla disponible para otra accion." : "Tecla guardada.";
        return true;
    }

    public void CargarGuardado()
    {
        acciones.RemoveAllBindingOverrides();
        try
        {
            if (PlayerPrefs.HasKey(ClaveGuardado)) acciones.LoadBindingOverridesFromJson(PlayerPrefs.GetString(ClaveGuardado));
            for (int i = 0; i < Controles.Length; i++)
            {
                string path = Ruta(i);
                if (string.IsNullOrEmpty(path) && Controles[i].Alternativo) continue;
                if (string.IsNullOrEmpty(path) || !path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("/escape", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Tecla guardada no valida");
                for (int j = 0; j < i; j++) if (string.Equals(path, Ruta(j), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Teclas guardadas duplicadas");
            }
        }
        catch (Exception error)
        {
            acciones.RemoveAllBindingOverrides(); PlayerPrefs.DeleteKey(ClaveGuardado);
            Debug.LogWarning("Se restauraron los controles por una configuracion guardada no valida: " + error.Message);
        }
        Cambiados?.Invoke();
    }

    public void Restaurar()
    {
        acciones.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(ClaveGuardado); PlayerPrefs.Save(); Cambiados?.Invoke();
    }

    public static bool TeclaEnUso(Key key)
    {
        if (Actual == null || Keyboard.current == null) return false;
        string path = "<Keyboard>/" + Keyboard.current[key].name;
        for (int i = 0; i < Controles.Length; i++) if (string.Equals(Actual.Ruta(i), path, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static string Nombre(int indice)
    {
        if (Actual == null) return "-";
        string path = Actual.Ruta(indice);
        if (string.IsNullOrEmpty(path)) return "SIN TECLA";
        switch (path.Substring(path.LastIndexOf('/') + 1).ToLowerInvariant())
        {
            case "uparrow": return "ARRIBA";
            case "downarrow": return "ABAJO";
            case "leftarrow": return "IZQ.";
            case "rightarrow": return "DER.";
            case "space": return "ESPACIO";
            case "enter": return "ENTER";
            case "leftctrl": return "CTRL IZQ.";
            case "rightctrl": return "CTRL DER.";
        }
        var action = Actual.Resolver(indice, out int binding);
        return action.GetBindingDisplayString(binding).ToUpperInvariant();
    }

    public static string Movimiento(int jugador)
    {
        int inicio = jugador == 1 ? 0 : 6;
        return $"{Nombre(inicio)} {Nombre(inicio + 2)} {Nombre(inicio + 1)} {Nombre(inicio + 3)}";
    }
}
