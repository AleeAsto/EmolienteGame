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
  aplicados inmediatamente y guardados al salir del panel.
- Como jugar: WASD/X/C para P1; IJKL o flechas/N/M para P2. Explica cartas,
  puertas y estudiantes raros.
- Creditos: Picatzo, Malica, Nico F, Cris R, AleeAsto y Zayd-A.
- Pausa: Escape o el boton superior; Continuar, Opciones de audio y Volver al
  menu. Escape desde opciones vuelve a pausa; desde pausa reanuda.
- Resultado: ganador P1, P2 o empate y almas reales. Revancha reinicia puntos,
  estudiantes y reloj mediante la recarga de escena.

La pausa solo se permite despues de la cuenta inicial y antes del final. Congela
el reloj y el juego, conservando el estado de la ronda. La musica continua, los
efectos de partida se pausan y los botones siguen sonando. Los menus admiten
raton y navegacion por teclado con flechas y Enter. Escape vuelve desde las
pantallas secundarias del menu principal. En el editor Salir detiene Play; en
la build cierra el juego.

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
