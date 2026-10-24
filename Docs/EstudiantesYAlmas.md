# Estudiantes y almas

La escena `Assets/Scenes/PasilloEstudiantesAlmas.unity` contiene el objeto `Estudiantes`,
con un `GeneradorEstudiantes` conectado a `ZoneMov`, `CameraP1` y `CameraP2`.
El prefab `Assets/Prefabs/Estudiante.prefab` usa un sprite provisional.

## Ajustes en el Inspector

- En `Estudiantes`: poblacion inicial (10), limite simultaneo (16), intervalo
  aleatorio entre intentos de aparicion (2 a 4 s), tramos (6), velocidades
  (1.2 a 2.2), separacion y margenes.
- En el prefab, componente `AlmaEstudiante`: `Valor Almas` (10).
  Un prefab variante puede dar, por ejemplo, 12 almas.
  Agregalo a `Prefabs Estudiantes` del generador para incluirlo en las apariciones.
- En `P1` y `P2`, componente `AlmasJugador`: `Cantidad` muestra el total durante Play.
- En cada jugador, componente `SuccionJugador`: `Tiempo Drenaje` (3 s),
  `Alcance` (1.6 unidades desde los pies) y su propia referencia de Input Actions.
- En el prefab, `EstadoVisualEstudiante`: tono gris aplicado mientras esta agotado.

## Comportamiento

Los estudiantes iniciales se reparten a lo largo del pasillo. Cada estudiante
camina a izquierda o derecha, conservando su Y. Las nuevas apariciones se
eligen entre las franjas horizontales que quedan fuera de ambas camaras,
incluyendo el margen configurado. Se priorizan los tramos con menos estudiantes;
entre opciones con la misma poblacion se elige al azar segun su longitud oculta.
Si una opcion no tiene espacio libre, se prueban las siguientes. Si no queda
espacio oculto y libre, se omite ese intento hasta el siguiente intervalo aleatorio.
Salir de una camara no elimina al estudiante; se elimina al cruzar la salida
del pasillo en su sentido de marcha, con un margen para ocultar el sprite completo.

Un estudiante ocupado se detiene. Tras completar la extraccion, entrega su
valor directamente al jugador y continua caminando agotado hasta salir del pasillo.
Cada estudiante entrega almas una sola vez; no se recupera automaticamente.
`SortingGroup` usa la posicion del objeto raiz junto con la ordenacion Y ya
configurada en el Renderer2D, para mantener juntos los sprites de cada personaje.

## Controles de succion

P1 usa WASD y mantiene **E** para drenar. P2 usa IJKL o flechas y mantiene **O**.
Las acciones `Jugador1/Succionar` y `Jugador2/Succionar` estan en
`Assets/Settings/ControlesJugadores.inputactions`.

Acercate al estudiante y pulsa succionar: se elige el mas cercano disponible
dentro del alcance. El estudiante se detiene durante el drenaje. Mantener la
tecla durante 3 segundos entrega +10 almas y lo deja gris permanentemente.
Durante la absorcion tambien se suman puntos fraccionarios (2 por segundo,
ajustables en `Puntos Por Segundo`).

Soltar la tecla, alejarse, perder el foco de la ventana o desactivar el componente
cancela la succion y reinicia el progreso, sin entregar el premio por convertir.
Los puntos de absorcion ya sumados se conservan. Tras completar o
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

## PvP y cancelacion

- P1 pulsa **C** y P2 pulsa **M**. Cada pulsacion inicia un golpe si termino
  su cooldown de 2 segundos, contado desde el inicio del ataque.
- El circulo provisional recorre un arco de arriba hacia abajo delante del
  ultimo lado horizontal al que miro el jugador. P1 empieza mirando a la
  derecha y P2 a la izquierda; moverse verticalmente no cambia ese lado.
- Un impacto normal agrega 2 segundos de stun. Si la victima estaba succionando,
  agrega 5 segundos, cancela su progreso y libera inmediatamente al estudiante.
  El estudiante interrumpido se asusta y huye, siguiendo el comportamiento de
  cancelacion del estudiante; ya no admite otra absorcion.
- El stun se acumula sobre el tiempo que aun quedaba: un golpe normal recibido
  con 4 segundos restantes deja 6 segundos. No hay inmunidad adicional.
- Durante el stun se bloquean movimiento, golpe y succion. El empujon de
  0.3 unidades se aplica suavemente y respeta los limites del pasillo.
- Si chocan los circulos de ataques opuestos, ambos golpes se cancelan y cada
  jugador recibe 0.5 segundos de stun y un empujon de 0.15 unidades. Ambos
  conservan su cooldown. Se comprueba el recorrido entre frames para detectar
  tambien los circulos que se cruzan entre dos actualizaciones.
- Cada ataque solo puede impactar una vez. Atacar cancela la propia succion;
  hay que volver a pulsar E/O para empezar a drenar de nuevo. Los golpes no
  quitan ni transfieren almas.
- El combate respeta la pausa de la cuenta atras y del final de partida:
  con el tiempo detenido no se inician golpes ni se resuelven impactos o choques.

Un circulo amarillo sobre la cabeza indica stun y un destello blanco indica
choque. Son placeholders editables en los hijos `GolpePvP`, `AturdimientoPvP`
y `CombatePvP/DestelloBloqueo`.

En cada `AtaqueJugador` se ajustan cooldown, duracion (0.4 s), preparacion
(0.16 s), alcance horizontal (1.8), tolerancia Y (0.8), arco, tiempos de stun
y empujones. La tolerancia Y compara posiciones en el piso para evitar golpear
a alguien que pasa por otra profundidad. `MovimientoJugador2D` controla la
rapidez del empujon (3 unidades/s).

`EstadoCombateJugador` guarda el stun y emite `Interrumpido` para cancelar
acciones. `CombatePvP` resuelve ambos ataques una vez por frame despues de leer
ambas entradas y antes de que avance la succion, sin favorecer a P1 por orden
de componentes. La reserva del estudiante funciona asi:

1. Llamar a `AlmaEstudiante.IntentarOcupar(almasJugador)` al iniciar. Devuelve
   `false` si esta agotado o si otro jugador ya lo ocupa.
2. Llamar a `Liberar(almasJugador)` al cancelar o interrumpirse. Solo el
   ocupante puede liberar su reserva; el ataque debe cancelar la succion del rival.
3. Llamar a `IntentarExtraer(almasJugador)` al completar el drenaje. Solo el
   ocupante puede cobrar; queda agotado antes de notificar la recompensa,
   impidiendo cobros repetidos sobre el mismo estudiante.

`AlmasJugador.CantidadCambiada` y `AlmaEstudiante.EstadoCambiado` permiten
conectar el marcador y el feedback sin acoplarlos al spawner.
