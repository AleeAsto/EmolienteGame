# Pasillo con puertas

Abrir `Assets/Scenes/PartidaIntegrada.unity` y pulsar Play. Es la escena habilitada
en Build Settings. Parte de la version actualizada de `ActionCards`, conserva
las cartas de aceleracion y pesadez, el seguimiento de camaras y la salida
anticipada de estudiantes agotados. Incluye el mapa y las puertas de
`PasilloPuertas` y la profesora de `PartidaCycle`. Las escenas anteriores se conservan.

El pasillo mide 84 unidades de largo (antes, unas 57.84); suelo, zona de movimiento
y extremos de camara se ampliaron juntos. Hay cuatro puertas distribuidas por
el pasillo. Son placeholders: sustituir los hijos visuales de cada puerta por
assets de casillero o puerta, manteniendo el objeto raiz y `Salida`.

## Usar

- Acercarse a una puerta y moverse hacia arriba: P1 W, P2 I o flecha arriba,
  usando la misma Input Action de movimiento del jugador.
- Se elige al azar otra puerta disponible, nunca la misma ni una bloqueada.
- La puerta de salida se bloquea para ambos durante 10 s: no puede usarse como
  entrada ni seleccionarse como destino. Su texto muestra los segundos restantes
  y el panel se vuelve gris. Al estar disponible, vuelve a verde y muestra ARRIBA.
- La puerta de entrada sigue disponible. Cada jugador tiene, aparte, 5 s de
  cooldown entre viajes. Los intentos fallidos no generan cooldown.
- Hay que soltar la direccion arriba despues de viajar antes de intentar otro
  viaje. Mantenerla mientras se camina hacia una puerta permite entrar al acercarse.
- No se puede viajar durante el stun, con el menu de cartas abierto ni con la
  partida pausada. Al viajar se cancelan succion y ataque, se libera el estudiante
  y se elimina el empujon pendiente. Se conservan almas, cartas y direccion.
- Las camaras siguen al jugador en su nueva posicion. Ambos cooldowns usan
  tiempo de juego y se congelan durante las pausas.

## Ajustes

`Puertas/RedPuertas`: referencias de las cuatro puertas y bloqueo de salida (10 s).
`P1` y `P2`, `UsarPuertaJugador`: cooldown personal (5 s).
Cada `PuertaTeletransporte`: numero, salida, alcance X/Y y visual del estado.
El punto `Salida` esta delante del marco y dentro de los limites del suelo.
