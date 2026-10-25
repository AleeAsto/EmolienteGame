# Musica y efectos

Los dieciseis OGG del musico se importaron sin modificar el audio original en
`Assets/Audio/Musica` y `Assets/Audio/SFX`.

La configuracion esta en `Assets/Resources/AudioJuego.asset`. Permite cambiar
clips, volumen de musica (0.3), efectos (0.75), pasos (1) y transicion musical
(0.4 s). El volumen general del menu sigue controlando todos los canales y se
guarda con `VolumenGeneral`, incluso si se inicia directamente la partida.
Los menus de AudioMenus tambien permiten regular musica y efectos por separado;
`VolumenMusica` y `VolumenEfectos` guardan factores de 0 a 1 sobre esta mezcla.
El control de efectos incluye pasos y modifica tambien los sonidos ya activos.

## Asignaciones

- `menu theme`: menu principal y sus pantallas internas, en bucle.
- `music gameplay`: partida, en bucle, con fundido entre temas. Continua durante
  la cuenta inicial y al terminar la ronda.
- `boton-del-menu`: clic o confirmacion de los botones del menu, incluido Jugar.
- `cambiar de carta`: abrir la mano y cambiar realmente de carta seleccionada.
- `usar carta`: confirmar un uso valido y pagado; no suena si faltan almas.
- `abrir-puerta` y `cerrar-puerta`: viaje aceptado por una puerta, en secuencia.
  Los intentos rechazados por cooldown o falta de destino no emiten sonido.
- `correr_1`: pasos de ambos jugadores al desplazarse voluntariamente. Se cortan
  al detenerse, quedar bloqueado en un limite, recibir stun o pausar la partida.
  Los teletransportes, empujones sin entrada y el dash no producen pasos. Las
  cartas de velocidad modifican el tono y ritmo dentro del rango 0.7 a 1.6.
- `V.S.`: presentacion versus, despues del fundido de carga.
- `WIN`: inicio de la presentacion de victoria; usa el canal de interfaz para
  sonar con el reloj detenido. No se dispara en empates.
- `Golpe`: ataque aceptado; un intento en cooldown no repite el sonido.
- `muerte-de-NPC`: extraccion completada, una sola vez por conversion.
- `casillero-abierto` y `casillero-cerrado`: cambio real de estado del casillero.
- `absorber-hablando`: bucle propio de cada jugador mientras drena un objetivo.
  Se detiene al cancelar o completar la succion.
- `stune-pajaros`: bucle propio de cada jugador mientras dura su aturdimiento.
  Se detiene al recuperarse. Ambos bucles respetan pausa y volumen de efectos.

Los niveles independientes `volumenSuccion` (0.45) y `volumenAturdimiento` (0.4)
permiten ajustar la mezcla de estos dos bucles desde AudioJuego.asset. El cambio
de volumen de efectos se aplica tambien a los bucles que ya estan activos.

`movimiento.ogg` se excluyo porque el equipo confirmo que pertenece a otro proyecto.

## Reproduccion

`AudioJuego` se crea automaticamente al entrar en Play o al ejecutar la build.
Persiste entre escenas para conservar el clic de Jugar y realizar el fundido.
No hace falta arrastrarlo a las escenas. Usa audio 2D y un solo AudioListener;
desactiva los listeners de las camaras durante Play para evitar duplicados en
pantalla dividida. Cada jugador recibe su propio `AudioPasosJugador` al cargar
la escena, de modo que puede detenerse uno sin cortar los pasos del otro.
`AudioEstadoJugador` agrega las dos fuentes de succion y stun de cada jugador.

La musica usa streaming. Los SFX usan Decompress On Load para reproducirse sin
esperar a decodificar en el momento de pulsar. Los assets se incluyen en la
build mediante las referencias de la configuracion en Resources.
