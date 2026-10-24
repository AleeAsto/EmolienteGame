# Estudiantes y almas

La escena `Assets/Scenes/PasilloEstudiantesAlmas.unity` contiene el objeto `Estudiantes`,
con un `GeneradorEstudiantes` conectado a `ZoneMov`, `CameraP1` y `CameraP2`.
El prefab `Assets/Prefabs/Estudiante.prefab` usa un sprite provisional.

## Ajustes en el Inspector

- En `Estudiantes`: poblacion inicial (10), limite simultaneo (16), intervalo
  entre intentos de aparicion (2.5 s), velocidades (1.2 a 2.2), separacion y margenes.
- En el prefab, componente `AlmaEstudiante`: `Valor Almas` (10) y
  `Tiempo Recuperacion` (15 s). Un prefab variante puede dar, por ejemplo, 12 almas.
  Agregalo a `Prefabs Estudiantes` del generador para incluirlo en las apariciones.
- En `P1` y `P2`, componente `AlmasJugador`: `Cantidad` muestra el total durante Play.
- En cada jugador, componente `SuccionJugador`: `Tiempo Drenaje` (3 s),
  `Alcance` (1.6 unidades desde los pies) y su propia referencia de Input Actions.
- En el prefab, `EstadoVisualEstudiante`: tono gris aplicado mientras esta agotado.

## Comportamiento

Los estudiantes iniciales se reparten a lo largo del pasillo. Cada estudiante
camina a izquierda o derecha, conservando su Y. Las nuevas apariciones se
eligen entre las franjas horizontales que quedan fuera de ambas camaras,
incluyendo el margen configurado. Si no queda espacio oculto, se omite ese intento.
Salir de una camara no elimina al estudiante; se elimina al cruzar la salida
del pasillo en su sentido de marcha, con un margen para ocultar el sprite completo.

Un estudiante ocupado se detiene. Tras completar la extraccion, entrega su
valor directamente al jugador y continua caminando mientras se recupera.
Los temporizadores usan tiempo de juego: pausar la partida pausa la recuperacion.
`SortingGroup` usa la posicion del objeto raiz junto con la ordenacion Y ya
configurada en el Renderer2D, para mantener juntos los sprites de cada personaje.

## Controles de succion

P1 usa WASD y mantiene **E** para drenar. P2 usa IJKL o flechas y mantiene **O**.
Las acciones `Jugador1/Succionar` y `Jugador2/Succionar` estan en
`Assets/Settings/ControlesJugadores.inputactions`.

Acercate al estudiante y pulsa succionar: se elige el mas cercano disponible
dentro del alcance. El estudiante se detiene durante el drenaje. Mantener la
tecla durante 3 segundos entrega +10 almas y lo deja gris durante su recuperacion
de 15 segundos. Al recuperarse vuelve a su color original.

Soltar la tecla, alejarse, perder el foco de la ventana o desactivar el componente
cancela la succion y reinicia el progreso, sin entregar almas. Tras completar o
cancelar un drenaje hay que soltar y volver a pulsar para iniciar otro; mantener
la tecla no encadena objetivos automaticamente. Cada jugador solo usa su accion.

## Marcador versus

`HUDVersus/MarcadorVersus` muestra la puntuacion de ambos jugadores en la
division horizontal entre camaras. P1 ocupa el tramo celeste y P2 el rojo.
El aro se desplaza con una transicion breve cuando cambian las almas.

El reparto compara los puntos actuales: P1 ocupa `P1 / (P1 + P2)` del carril.
Con 20 contra 10, el celeste ocupa dos tercios. Con 0 contra 0, el reparto es
50/50. Los numeros de un tramo muy estrecho se elevan sobre la barra para seguir
visibles cuando el aro llega a un extremo. El reparto visual no termina la partida.

En el Inspector de `MarcadorVersus` se pueden ajustar colores y tiempo de
transicion. Los textos, aro y barra son objetos UI editables dentro del Canvas.
El Canvas se adapta a la resolucion y las referencias de puntuacion apuntan
directamente a `AlmasJugador` de P1/P2 mediante `CantidadCambiada`.

## Integracion con ataque

El ataque PvP sigue pendiente. `SuccionJugador.CancelarSuccion()`
permite que un ataque interrumpa al ocupante y libere al estudiante. Internamente:

1. Llamar a `AlmaEstudiante.IntentarOcupar(almasJugador)` al iniciar. Devuelve
   `false` si esta agotado o si otro jugador ya lo ocupa.
2. Llamar a `Liberar(almasJugador)` al cancelar o interrumpirse. Solo el
   ocupante puede liberar su reserva; el ataque debe cancelar la succion del rival.
3. Llamar a `IntentarExtraer(almasJugador)` al completar el drenaje. Solo el
   ocupante puede cobrar; la recuperacion empieza antes de notificar la recompensa,
   impidiendo cobros repetidos durante el enfriamiento.

`AlmasJugador.CantidadCambiada` y `AlmaEstudiante.EstadoCambiado` permiten
conectar el marcador y el feedback sin acoplarlos al spawner.
