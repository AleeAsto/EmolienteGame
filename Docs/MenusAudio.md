# Escenas de trabajo de audio y menus

El flujo de audio y menus esta integrado con los avances de Ale de los commits
`7628035` (casilleros y estudiantes ocultos) y `a55e42c` (puertas animadas).
`PartidaAudioMenus` conserva su mapa, sus 14 casilleros, sprites y ajustes de
jugadores, ademas de los menus de pausa y resultado. Los scripts y assets de
audio son compartidos. Las escenas originales se conservan.

Abrir `Assets/Scenes/AudioMenus/MenuAudioMenus.unity` y pulsar Play inicia el
flujo completo. Jugar carga `PartidaAudioMenus`; Revancha recarga esa misma copia
y Volver al menu regresa a `MenuAudioMenus`. Estas dos escenas aparecen primero
en Build Settings. Las entradas originales siguen disponibles despues de ellas.

## Flujo

- Inicio: Jugar, Opciones, Como jugar, Creditos y Salir.
- Opciones: volumen general, musica y efectos/pasos con valores porcentuales,
  aplicados inmediatamente y guardados al salir del panel. Configurar controles
  permite cambiar el teclado de ambos jugadores.
- Como jugar: muestra las teclas elegidas para cada jugador. Por defecto,
  WASD/X/C para P1 e IJKL o flechas/N/M para P2. Explica cartas, puertas y raros.
- Creditos: Picatzo, Malica, Nico F, Cris R, AleeAsto y Zayd-A.
- Pausa: Escape o el boton superior; Continuar, Opciones y Volver al
  menu. Escape desde opciones vuelve a pausa; desde pausa reanuda.
- Resultado: ganador P1, P2 o empate y almas reales. Revancha reinicia puntos,
  estudiantes y reloj mediante la recarga de escena.

La pausa solo se permite despues de la cuenta inicial y antes del final. Congela
el reloj y el juego, conservando el estado de la ronda. La musica continua, los
efectos de partida se pausan y los botones siguen sonando. Los menus admiten
raton y navegacion por teclado con flechas y Enter. Escape vuelve desde las
pantallas secundarias del menu principal. En el editor Salir detiene Play; en
la build cierra el juego.

## Configuracion de teclado

Desde Opciones > Configurar controles, seleccionar el boton de una accion y
pulsar la nueva tecla. Hay cuatro direcciones, absorber/confirmar y golpear/cartas
para cada jugador, ademas de cuatro alternativas de movimiento para P2. La
direccion arriba tambien se usa para entrar en puertas. Mantener golpear sigue
abriendo las cartas y absorber confirma la seleccion.

Los cambios se guardan automaticamente en PlayerPrefs y se aplican a las mismas
Input Actions usadas por la partida. Sobreviven a cambios de escena y reinicios.
No modifican el archivo de Input Actions del proyecto. El panel tambien esta en
las opciones de pausa; las ayudas del menu se actualizan con las teclas elegidas.

No se admiten teclas repetidas entre acciones ni entre jugadores. Escape cancela
la captura sin salir de opciones y sigue reservado para pausar/volver. La captura
se cancela tras 10 segundos o al perder el foco. Durante ella se suspende la
navegacion de UGUI para poder asignar flechas o Enter sin activar otros botones.
Supr sobre una alternativa de P2 permite dejarla vacia y liberar esa tecla.
Restablecer recupera los controles originales para ambos jugadores.

Se verifico en Play con eventos simulados del Input System: captura desde los
botones de P1 y P2, rechazo de duplicados, cancelacion con Escape, guardado al
reiniciar Play, reasignacion durante pausa y restablecimiento. En partida se
probaron las cuatro direcciones, movimiento real, succion, golpe y abrir/confirmar
cartas de ambos jugadores con sus nuevas teclas; las anteriores quedaron libres.
Tambien se verifico que asignar T o B no activa sus atajos de desarrollo. Los
informes temporales estan en Logs/Controles, ignorado por Git.

## Edicion y comprobaciones

Todos los paneles son objetos UGUI guardados en las escenas y pueden editarse
desde Unity. `MenuPrincipal/Escena Partida` y `MenuPartida/Escena Menu` permiten
cambiar los destinos al fusionar mas adelante.

`ConstruirMenusAudio` reconstruye solo las dos copias, con los controles y los
nombres actuales. No ejecutarlo sobre una copia cuyo diseno se haya editado a
mano sin guardar primero esos cambios en Git: reconstruye sus paneles.

La copia de partida incluye `FondoPantalla`, que limpia los margenes fuera del
70% central ocupado por las camaras, evitando restos visuales del menu anterior.
Tambien se protege el cierre de `VistaCartas` cuando las camaras ya se destruyeron
al cambiar de escena.

El viaje por puertas conserva la animacion de entrada y el flash exclusivo de
cada jugador. Durante el viaje no se mueve, ataca, drena ni emite pasos. Los
sonidos de apertura y cierre se mantienen. Al pausar se congelan la animacion
y sus efectos de sonido hasta continuar.

Se comprobo en Play el flujo principal, los tres volumenes, la pausa con Escape,
la congelacion del reloj, continuar mediante Submit de UGUI, los resultados de
P1/P2/empate, dos revanchas con puntos reiniciados y el regreso al menu. Las
pruebas y capturas temporales estan en `Logs/Menus`, ignorado por Git.
