# Cartas de habilidad

La escena de prueba es `Assets/Scenes/ActionCards.unity`. Abrirla y pulsar Play.
La escena anterior conserva sus controles originales; solo los jugadores que
tienen `ManoCartasJugador` usan pulsacion corta/mantener para golpear o abrir cartas.

## Probar

- P1: mantener C durante 0.35 s para abrir; A/D para elegir; E para usar.
- P2: mantener M para abrir; J/L o flechas para elegir; O para usar.
- Soltar C/M conserva el menu abierto. Volver a pulsarlo cierra sin golpear.
- Una pulsacion corta de C/M golpea al soltar, respetando el cooldown existente.
- G entrega una carta aleatoria del catalogo a P1; H a P2. Solo durante la ronda, en el editor o builds
  de desarrollo, con `Atajos Desarrollo` activo. No cambia puntos ni genera una deuda
  de recompensas. Pulsar varias veces permite comprobar el limite de tres.
- B suma 100 puntos a cada jugador por pulsacion, con las mismas condiciones
  de los atajos de desarrollo.
- La carta cuesta 20 puntos reales. Absorber estudiantes para alcanzarlos o modificar
  `Cantidad` de `AlmasJugador` en Play, o pulsar B. El numero del marcador puede estar redondeado.
- Con menos puntos, E/O muestra el costo sin consumir la carta ni cerrar el menu.
- Al usarla, desaparece de la mano y descuenta su costo en puntos. Sube verticalmente hasta salir
  por arriba de la camara propia (0.4 s). Luego entra a la presentacion central desde
  abajo para P1 o desde arriba para P2 (0.45 s), muestra el frente quieto (1.2 s),
  gira mostrando ambos sprites (2 s) y sale verticalmente de la pantalla (0.5 s).
  Un buff sale hacia el lado del lanzador; un debuff hacia el rival. Para P1 esto
  significa buff abajo y debuff arriba; para P2, buff arriba y debuff abajo.
  No vuelve a la mano ni se encoge al salir. La habilidad se aplica al terminar la salida.
  `Duracion Vista Frente` y `Duracion Giro` se ajustan en `SistemaCartas`.
- Aceleracion tiene destino propio y Pesadez destino rival.
  Dos usos simultaneos se presentan en cola; los
  confirmados en el mismo frame se ordenan P1, P2.

El menu bloquea solo movimiento voluntario, golpe y absorcion propios. No pausa la
partida ni protege al jugador. Un golpe aceptado lo cierra y conserva las cartas;
el empujon funciona. Tras confirmar se recupera el control y el lanzamiento ya no
se interrumpe por golpes. La confirmacion no inicia una absorcion en ese mismo frame.
Perder foco cierra el menu. El final cancela la cola y la animacion.

La mano de P1 aparece en la esquina inferior izquierda de su camara y la de P2
en la inferior derecha. Las cartas muestran siempre su frente en la mano, incluido
el costo en la parte superior. Cerradas, solo asoman por el borde; al abrir,
solo la seleccion se eleva y agranda con un borde del color del jugador; las otras
siguen asomando por el borde inferior y se oscurecen mientras el menu esta abierto.
La elevacion de seleccion recorre la mitad de la distancia del prototipo inicial.
El reverso se usa unicamente durante el giro de activacion.
El Canvas de cartas es independiente del HUD del marcador para evitar heredar
su escala y quedar comprimido sobre la barra central.
Al recibir, solo la ultima carta recibida se levanta brevemente; las anteriores
siguen abajo. Al abrir el menu termina ese aviso y solo se eleva la seleccionada.
La carta en vuelo usa un Canvas con orden 300, encima del marcador (100) y del
Canvas de cuenta regresiva (200).

## Habilidades de velocidad

| Carta | Tipo y color | Destino | Multiplicador | Duracion | Costo |
| --- | --- | --- | --- | --- | --- |
| Aceleracion | Buff verde | Lanzador | 1.4 (+40 %) | 8 s | 20 puntos |
| Pesadez | Debuff rojo | Rival | 0.6 (-40 %) | 8 s | 20 puntos |

Ambas afectan movimiento voluntario y progreso de absorcion, incluidos sus puntos
por segundo. Una extraccion completa conserva su premio: los 3 segundos base pasan
a 5 s con Pesadez o aproximadamente 2.14 s con Aceleracion si se mantiene el efecto.
Cambiar de velocidad durante una absorcion conserva su progreso. Los empujones,
golpes, cooldowns y tiempos de stun no cambian.

La duracion empieza al terminar la animacion, usa tiempo de juego y respeta pausas.
Repetir el mismo tipo renueva sus 8 s sin acumular copias. Un buff y un debuff pueden
coexistir con temporizadores independientes: mientras coinciden, el multiplicador
es 1.4 * 0.6 = 0.84. Al expirar uno queda el otro. Se limpian al finalizar la ronda
o desactivar el sistema. Un golpe no los elimina.

`DefinicionCarta` permite editar multiplicador, duracion, costo, destino y tipo
de efecto desde el asset. El color verde/rojo depende del destino propio/rival y se
mantiene en la mano y durante la animacion; las cartas no seleccionadas se oscurecen.
`EfectosJugador` se agrega a P1/P2 al iniciar la escena y administra sus modificadores.

## Entrega normal

`SistemaCartas` esta en `HUDVersus/MarcadorVersus`, junto con `CicloPartida`.
Todos estos valores se editan en su Inspector:

| Ajuste | Inicial |
| --- | --- |
| Inicio | Tras 1/3 de la duracion (60 s de 180) |
| Diferencia minima | 25 puntos |
| Proporcion maxima del perdedor respecto al rival | 0.75 |
| Persistencia continua | 8 s |
| Espera tras cada entrega, por jugador | 25 s |
| Cierre de entregas | Ultimos 10 s |
| Limite por mano | 3 |

Las condiciones se comprueban sobre puntos reales, sin la base visual del marcador.
La espera debe terminar antes de acumular otros 8 s. Recuperarse, llenar la mano o
salir de la ventana de entregas reinicia la persistencia. No se almacenan entregas
pendientes. Las cartas recibidas se conservan aunque cambie el lider.

## Assets y responsabilidades

- `Assets/Cartas/CartaAceleracion.asset` y `CartaPesadez.asset`: definiciones
  `ScriptableObject`; identificador, titulo, frente, reverso, costo, destino y efecto.
  `CartaPrueba.asset` se conserva como referencia sin efecto, fuera del catalogo.
  Cambiar sus sprites reemplaza el arte
  provisional sin tocar codigo. El costo se dibuja como texto encima del frente.
- `ManoCartasJugador`: inventario, seleccion, controles y validacion del costo.
  `Recibir` sera la entrada para los futuros NPC especiales; devuelve false con mano
  llena o fuera de partida.
- `SistemaCartas`: entrega por desventaja, cola central y aplicacion de la habilidad
  al objetivo al terminar el vuelo.
- `VistaCartas`: Canvas de runtime que se ajusta a los rectangulos de ambas camaras;
  recorta cada mano al borde inferior de su pantalla y no captura inputs de UI.

La seleccion y el vuelo usan el tiempo de juego. La pausa detiene sus animaciones.
Los NPC especiales y las demas habilidades quedan pendientes.
Estas dos cartas son normales. Las potentes quedan pendientes del balance de esta
primera prueba; por ahora no se agregan bonus de puntos a las habilidades de velocidad.

## Verificacion

`Emoliente > Verificar cartas (sin Play)` comprueba en una escena de preview los
sprites, las referencias, el limite de inventario, el costo, el consumo con
descuento de puntos, el cierre sin consumir y las reglas de desventaja, espera,
empate y cierre de entregas, ademas de objetivos, renovacion y combinacion de habilidades.
No modifica la escena abierta. El resultado se guarda
en `Temp/VerificarCartas.resultado.txt`. Los controles y la animacion se comprueban
manualmente en Play siguiendo los pasos anteriores.

`Tests/ModificadoresVelocidadTests.cs` verifica expiracion, renovacion sin copias,
temporizadores independientes, combinacion, rechazo de valores invalidos y limpieza
sin requerir el runtime de Unity.
