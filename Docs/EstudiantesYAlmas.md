# Estudiantes y almas

## Ampliar el mapa y mover el spawn

Mover los objetos raiz `P1` y `P2` cambia sus posiciones iniciales. Mover solamente
su hijo `Sprite` cambia el dibujo, pero no el jugador seguido por la camara.
`CamaraPasillo` sigue horizontalmente a cada jugador durante Play y tambien al
editar cuando `Seguir En Editor` esta activo. Se coloca al activar la camara,
antes de generar estudiantes; mover la camara en X manualmente no cambia el spawn.

Las camaras toman los bordes del mismo `ZoneMov` usado por movimiento y generacion.
Al ampliar su collider o escala, los limites de seguimiento se actualizan sin mover
`ExtremoI`/`ExtremoD`. `Zona Pasillo` permite asignar otra zona explicitamente; los
extremos solo se usan si no existe una zona asignada ni una zona en el jugador.
La camara se detiene cerca de los bordes para no mostrar fuera del mapa. Si el
mapa es mas estrecho que la vista, se centra. La altura Y sigue siendo manual.

La escena de juego es `Assets/Scenes/PartidaIntegrada.unity` y es la unica habilitada
en Build Settings. Contiene el objeto `Estudiantes`,
con un `GeneradorEstudiantes` conectado a `ZoneMov`, `CameraP1` y `CameraP2`.
El prefab `Assets/Prefabs/Estudiante.prefab` usa un sprite provisional.

## Estudiantes raros

`Assets/Prefabs/EstudianteRaro.prefab` es una variante con tinte dorado y cinco
brillos animados. Entrega 30 almas al completar la extraccion, ademas de los
puntos por segundo habituales; con el drenaje base de 3 s, una extraccion
completa sin interrupciones suma 36 (30 + 6). La recompensa completa se entrega
una sola vez. Despues se agota, se vuelve gris y desaparece en 2 s, como el normal.

Al completar el drenaje tambien entrega una carta aleatoria al jugador que lo
termino: basica antes de mitad de partida y potente desde la mitad exacta
(90 s transcurridos en una ronda de 180 s). Si ya tiene una potente, incluso
en animacion de entrega, recibe una basica. Con tres cupos ocupados no entrega
carta; las 30 almas se conservan. Los cupos incluyen cartas pendientes en animacion.
La potente usa su animacion habitual y reserva su cupo antes de empezar.
Cancelar el drenaje no da carta; cada raro completo solo puede recompensar una vez.

`AlmaEstudiante/Es Raro` evita que se asuste al interrumpir: se libera, conserva
su sentido de marcha y vuelve a caminar. Puede drenarlo de nuevo cualquiera de
los jugadores; el progreso de ese intento vuelve a cero. Los normales conservan
su huida al cancelar. Perfume y Bomba siguen afectando a ambos tipos.

En `GeneradorEstudiantes`, `Raros Por Partida` vale 3 y `Multiplicador Velocidad
Raro` vale 1.25: se aplica al mismo rango base de velocidad. El prefab raro esta
separado de la poblacion normal, de modo que no aparece al comenzar ni se elige
para los estudiantes falsos de Confusion.

Se programan tres momentos aleatorios separados usando la duracion de la ronda.
En una ronda de 180 s, sus ventanas aproximadas son 26-52, 68-94 y 110-136 s
transcurridos. Cada raro consume una de esas tres apariciones, aunque luego
salga del pasillo sin ser drenado. Se respetan el limite simultaneo, la separacion
y las zonas fuera de ambas camaras. Si no hay espacio o cupo, se reintenta cada
0.5 s con prioridad sobre los normales; no se descarta la aparicion pendiente.
La pausa no avanza la programacion ni los brillos.

Para probar sin esperar, durante Play abre el menu contextual de
`Estudiantes/GeneradorEstudiantes` y usa `Adelantar siguiente raro (Play)`.
Adelanta uno de los tres pendientes, sin crear apariciones adicionales.

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
valor directamente al jugador y continua caminando agotado durante 2 segundos,
reduciendo gradualmente el alfa de sus sprites hasta cero. Entonces se destruye y
libera su cupo en el generador. `Duracion Salida Agotado` en `EstudiantePasillo`
permite ajustar ese tiempo en el prefab. La pausa detiene movimiento y desvanecimiento.
Cada estudiante entrega almas una sola vez; no se recupera automaticamente.
`SortingGroup` usa la posicion del objeto raiz junto con la ordenacion Y ya
configurada en el Renderer2D, para mantener juntos los sprites de cada personaje.

## Controles de succion

P1 usa WASD y mantiene **X** para drenar. P2 usa IJKL o flechas y mantiene **N**.
Las acciones `Jugador1/Succionar` y `Jugador2/Succionar` estan en
`Assets/Settings/ControlesJugadores.inputactions`.

Acercate al estudiante y pulsa succionar: se elige el mas cercano disponible
dentro del alcance. El estudiante se detiene durante el drenaje. Mantener la
tecla durante 3 segundos entrega +10 almas y lo deja gris mientras camina y desaparece.
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

El reparto incluye una base visual de 20 puntos por lado, que no suma puntuacion
real: P1 ocupa `(P1 + base) / (P1 + P2 + 2 * base)` del carril. Con 20 contra 10,
el celeste ocupa 40/70 del ancho. Con 0 contra 0, el reparto es 50/50 y se ocultan
los ceros. Los numeros mantienen su altura dentro de la barra incluso si su tramo es muy estrecho.
El reparto visual no termina la partida.

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
  aplica 5 segundos, cancela su progreso y libera inmediatamente al estudiante.
  El estudiante interrumpido se asusta y huye, siguiendo el comportamiento de
  cancelacion del estudiante; ya no admite otra absorcion.
- Durante todo el stun y durante 0.5 segundos adicionales tras recuperarse,
  el jugador es invulnerable: los golpes no agregan ni reinician stun y tampoco
  aplican empujon ni cancelan succion. Durante la proteccion posterior puede actuar.
- Durante el stun se bloquean movimiento, golpe y succion. El empujon de
  0.3 unidades se aplica suavemente y respeta los limites del pasillo.
- Si chocan los circulos de ataques opuestos, ambos golpes se cancelan y cada
  jugador recibe 0.5 segundos de stun y un empujon de 0.15 unidades. Ambos
  conservan su cooldown. Se comprueba el recorrido entre frames para detectar
  tambien los circulos que se cruzan entre dos actualizaciones.
- Cada ataque solo puede impactar una vez. Atacar cancela la propia succion;
  hay que volver a pulsar X/N para empezar a drenar de nuevo.
- Cada golpe aceptado quita hasta 5 almas a la victima, sin bajar de cero ni
  transferirlas al atacante. Los choques y los golpes ignorados por invulnerabilidad
  no quitan almas. `Almas Perdidas Por Golpe` se ajusta en `CombatePvP`.
- El combate respeta la pausa de la cuenta atras y del final de partida:
  con el tiempo detenido no se inician golpes ni se resuelven impactos o choques.

Un circulo amarillo sobre la cabeza indica stun; el mismo indicador celeste
senala la proteccion posterior. Un destello blanco indica choque.
Son placeholders editables en los hijos `GolpePvP`, `AturdimientoPvP`
y `CombatePvP/DestelloBloqueo`.

En cada `AtaqueJugador` se ajustan cooldown, duracion (0.4 s), preparacion
(0.16 s), alcance horizontal (1.8), tolerancia Y (0.8), arco, tiempos de stun
y empujones. La tolerancia Y compara posiciones en el piso para evitar golpear
a alguien que pasa por otra profundidad. `MovimientoJugador2D` controla la
rapidez del empujon (3 unidades/s).

`EstadoCombateJugador` permite ajustar `Invulnerabilidad Tras Stun` y los colores.
`RecibirGolpe` devuelve `false` si rechaza un golpe por proteccion o pausa.
Guarda el stun y emite `Interrumpido` para cancelar
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

## Profesora

`Profesora` es un cuadrado morado provisional en `PartidaIntegrada`. Recorre el
pasillo horizontalmente a 2.5 unidades/s, conservando su Y. Cada pasada comienza
fuera de un extremo del pasillo y de ambas camaras, con margen adicional de
2 unidades para que el cubito entero quede oculto. Cruza hasta salir por el otro extremo y desaparecer
fuera de ambas vistas. Espera 4 s antes de otra pasada en sentido contrario;
no gira ni reaparece dentro de la pantalla. Al cruzarse con un jugador a no mas
de 0.65 unidades en Y y
0.8 en X, aplica 3.5 s de stun y cancela succion y ataque mediante el estado
de combate compartido. Respeta la invulnerabilidad durante el stun y los 0.5 s
posteriores. Empuja 0.5 unidades en su direccion de avance (el golpe PvP empuja
0.3), sin quitar almas. El empujon es suave y respeta los limites del pasillo.

En `ProfesoraPasillo` se ajustan velocidad, direccion inicial, margen fuera de
vista, intervalo entre pasadas, alcance, stun y empujon.
Para mover su carril, cambia la Y del objeto raiz.
El recorrido se pausa durante la cuenta inicial y al terminar la partida.

## Ciclo de partida

`CicloPartida` conserva la cuenta inicial 3-2-1-GO y el temporizador de 180 s.
El tiempo de juego se congela durante la cuenta atras y al terminar la ronda;
movimiento, succion y PvP respetan esa pausa. Gana quien tenga mas puntos reales,
incluyendo las fracciones de absorcion. La tecla T mantiene el atajo de desarrollo
para adelantar el final a tres segundos.
