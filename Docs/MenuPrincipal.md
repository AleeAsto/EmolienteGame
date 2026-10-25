# Menu principal

Abre `Assets/Scenes/MenuPrincipal.unity` para editar el menu y probarlo en Play.
Los paneles Titulo, Opciones, Tutorial y Creditos estan bajo Canvas. Sus textos,
colores y posiciones se editan directamente en el Inspector. Para previsualizar
otro panel, desactiva Titulo y activa el panel correspondiente; al iniciar Play
siempre se muestra Titulo.

Jugar carga PartidaIntegrada de forma asincrona y evita pulsaciones repetidas.
Opciones permite ajustar el volumen general y guardarlo en PlayerPrefs al volver.
La reasignacion de controles esta pendiente. Creditos tiene texto provisional para
completar con los integrantes del equipo. Tutorial contiene las reglas generales.

El Canvas usa Scale With Screen Size (1280 x 720), con navegacion de botones por
teclado y raton mediante InputSystemUIInputModule. No se incluye un boton Salir
porque el destino es el navegador.

La lista global del build incluye MenuPrincipal en primer lugar y PartidaIntegrada
despues. Si el perfil Web usa una lista propia, incluye las dos en ese orden.
Prueba Play desde MenuPrincipal y verifica Jugar y los tres botones Volver.
Antes de publicar, comprueba tambien la navegacion y el volumen en el build Web.

`Emoliente > Crear menu principal` genera la escena solo si no existe. No reemplaza
la escena existente ni sus cambios de diseno.
