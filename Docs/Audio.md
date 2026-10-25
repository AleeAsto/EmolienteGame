# Musica y efectos

Los ocho OGG del musico se importaron sin modificar el audio original en
`Assets/Audio/Musica` y `Assets/Audio/SFX`.

La configuracion esta en `Assets/Resources/AudioJuego.asset`. Permite cambiar
clips, volumen de musica (0.3), efectos (0.75), pasos (1) y transicion musical
(0.4 s). El volumen general del menu sigue controlando todos los canales y se
guarda con `VolumenGeneral`, incluso si se inicia directamente la partida.

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

`movimiento.ogg` se excluyo porque el equipo confirmo que pertenece a otro proyecto.

## Reproduccion

`AudioJuego` se crea automaticamente al entrar en Play o al ejecutar la build.
Persiste entre escenas para conservar el clic de Jugar y realizar el fundido.
No hace falta arrastrarlo a las escenas. Usa audio 2D y un solo AudioListener;
desactiva los listeners de las camaras durante Play para evitar duplicados en
pantalla dividida. Cada jugador recibe su propio `AudioPasosJugador` al cargar
la escena, de modo que puede detenerse uno sin cortar los pasos del otro.

La musica usa streaming. Los SFX usan Decompress On Load para reproducirse sin
esperar a decodificar en el momento de pulsar. Los assets se incluyen en la
build mediante las referencias de la configuracion en Resources.
