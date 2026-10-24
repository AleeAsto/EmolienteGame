# Cartas de habilidad

La escena de prueba es `Assets/Scenes/ActionCards.unity`. Abrirla y pulsar Play.
Para Confusion, usar `Assets/Scenes/ConfusionCard.unity`.
`Assets/Scenes/PartidaIntegrada.unity` combina las cartas actuales con el mapa
ampliado, las cuatro puertas y la profesora, y es la escena habilitada en Build Settings.
La escena anterior conserva sus controles originales; solo los jugadores que
tienen `ManoCartasJugador` usan pulsacion corta/mantener para golpear o abrir cartas.

## Probar

- P1: mantener C durante 0.35 s para abrir; A/D para elegir; boton de absorcion para usar.
- P2: mantener M para abrir; J/L o flechas para elegir; boton de absorcion para usar.
- Soltar C/M conserva el menu abierto. Volver a pulsarlo cierra sin golpear.
- Una pulsacion corta de C/M golpea al soltar, respetando el cooldown existente.
- G entrega una carta aleatoria del catalogo a P1; H a P2. Solo durante la ronda, en el editor o builds
  de desarrollo, con `Atajos Desarrollo` activo. No cambia puntos ni genera una deuda
  de recompensas. Pulsar varias veces permite comprobar el limite de tres.
  Estos atajos ignoran el requisito de mitad de partida para potentes, pero conservan
  su giro de recepcion y los limites de inventario (tres cartas, una potente como maximo).
- Shift+G / Shift+H fuerzan una potente para P1/P2, incluso antes de mitad de partida.
  Necesitan un cupo libre y no tener ya otra potente (incluidas entregas en animacion).
  La consola indica la carta sorteada, costo, duracion, candidatas normales/potentes
  y los motivos de un intento bloqueado. G/H sin Shift conservan el sorteo aleatorio.
  El encabezado dentro de la carta muestra NORMAL/POTENTE y la duracion, para
  distinguir 8 s de 20 s sin necesitar ver la parte inferior de la descripcion.
- B suma 100 puntos a cada jugador por pulsacion, con las mismas condiciones
  de los atajos de desarrollo.
- F/U entregan Perfume a P1/P2; R/P entregan Bomba apestosa a P1/P2.
  Mantener Shift con esos atajos entrega la version potente. Ya no hay un atajo
  especifico de Confusion; sigue disponible en el catalogo aleatorio de G/H.
- La carta cuesta 20 puntos reales. Absorber estudiantes para alcanzarlos o modificar
  `Cantidad` de `AlmasJugador` en Play, o pulsar B. El numero del marcador puede estar redondeado.
- Con menos puntos, confirmar muestra el costo sin consumir la carta ni cerrar el menu.
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
| Aceleracion | Buff verde | Lanzador | 3 (+200 %) | 8 s | 20 puntos |
| Pesadez | Debuff rojo | Rival | 0.5 (-50 %) | 8 s | 20 puntos |
| Aceleracion ★ | Potente verde | Lanzador | 3 (+200 %) | 20 s | 40 puntos |
| Pesadez ★ | Potente roja | Rival | 0.5 (-50 %) | 20 s | 40 puntos |

Ambas afectan movimiento voluntario, rapidez y recarga del golpe, y progreso de absorcion, incluidos sus puntos
por segundo. Una extraccion completa conserva su premio: Aceleracion permite
completar los 3 segundos base en 1 segundo. Pesadez hace avanzar la absorcion a
la mitad del ritmo normal mientras esta activa; al expirar recupera el ritmo base.
Cambiar de velocidad durante una absorcion conserva su progreso. El ataque tambien
conserva el progreso de su preparacion, arco y recarga cuando cambia el multiplicador.
Con Aceleracion x3, el golpe base de 0.4 s dura aproximadamente 0.13 s y su recarga
de 2 s tarda 0.67 s. Con Pesadez x0.5, tarda 0.8 s y recarga en 4 s.
Los empujones, alcance, perdida de puntos por impacto y tiempos de stun no cambian.

La duracion empieza al terminar la animacion, usa tiempo de juego y respeta pausas.
Repetir el mismo tipo renueva su duracion sin acumular copias. Una normal nunca
acorta el tiempo restante de una potente activa. Un buff y un debuff pueden
coexistir con temporizadores independientes: mientras coinciden, el multiplicador
es 3 * 0.5 = 1.5. Al expirar uno queda el otro. Se limpian al finalizar la ronda
o desactivar el sistema. Un golpe no los elimina.

`DefinicionCarta` permite editar multiplicador, duracion, costo, destino y tipo
de efecto desde el asset. El color verde/rojo depende del destino propio/rival y se
mantiene en la mano y durante la animacion; las cartas no seleccionadas se oscurecen.
`EfectosJugador` se agrega a P1/P2 al iniciar la escena y administra sus modificadores.

## Repartos a ambos jugadores


Ademas de la ayuda por desventaja, hay cuatro repartos independientes: a los
60, 90, 120 y 150 segundos transcurridos. `Momentos Reparto` en `SistemaCartas`
permite modificar esos tiempos. Son segundos de juego, sin contar pausas ni la
cuenta inicial, y cada evento se ejecuta una sola vez aunque se salten frames.

Ambos reciben una carta del mismo nivel, aunque las habilidades pueden ser distintas.
Antes de mitad de partida solo salen normales. Despues, si ambos pueden recibir
potentes, se sortea normal/potente al 50 %. Si uno ya tiene una potente, el reparto
comun es normal para respetar el maximo de una potente por mano. Las potentes
conservan su giro de recepcion.

Una mano llena (incluidas reservas en animacion) omite su entrega; el otro jugador
puede recibir la suya. No queda deuda al liberar espacio despues. Los repartos no
reinician ni consumen el cooldown o la persistencia de la ayuda por desventaja.

## Ayuda por desventaja

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

- `Assets/Resources/CatalogoCartas.asset`: catalogo compartido con las cinco normales
  y las cinco potentes. `SistemaCartas` lo carga automaticamente al iniciar y tras
  una recarga de scripts. La lista antigua guardada en una escena deja de decidir
  que cartas salen. `Catalogo Compartido` permite asignar otro catalogo explicitamente.

- `Assets/Cartas/CartaAceleracion.asset` y `CartaPesadez.asset`: definiciones
  `ScriptableObject`; identificador, titulo, frente, reverso, costo, destino y efecto.
  `CartaPrueba.asset` se conserva como referencia sin efecto, fuera del catalogo.
  Cambiar sus sprites reemplaza el arte
  provisional sin tocar codigo. El costo se dibuja como texto encima del frente.
  `CartaAceleracionPotente.asset` y `CartaPesadezPotente.asset` son las variantes
  con `Potente` activo, costo 40 y duracion 20 s.
- `ManoCartasJugador`: inventario, seleccion, controles y validacion del costo.
  `Recibir` sera la entrada para los futuros NPC especiales; devuelve false con mano
  llena o fuera de partida.
- `SistemaCartas`: entrega por desventaja, cola central y aplicacion de la habilidad
  al objetivo al terminar el vuelo.
- `VistaCartas`: Canvas de runtime que se ajusta a los rectangulos de ambas camaras;
  recorta cada mano al borde inferior de su pantalla y no captura inputs de UI.

La seleccion y el vuelo usan el tiempo de juego. La pausa detiene sus animaciones.
Los NPC especiales y las demas habilidades quedan pendientes.
Las potentes se distinguen con ★ en el titulo y el costo. Mantienen los mismos
multiplicadores que las normales; solo duran mas. No agregan bonus de puntos.

## Recepcion de potentes

En las entregas automaticas, el catalogo de potentes se habilita al llegar a la
mitad de la duracion de la ronda: 90 s transcurridos en una partida de 180 s.
`Inicio Potentes` en `SistemaCartas` permite retrasarlo, nunca adelantarlo a menos
de la mitad. Desde ese momento se sortean normales y potentes del catalogo elegible.
Cada mano admite como maximo una potente, contando tambien las entregas en animacion.

Antes de entrar en la mano, la potente aparece grande en la camara del receptor,
gira mostrando frente/reverso con borde dorado, mantiene brevemente el frente y
viaja a su hueco (2.2 s en total). Mientras gira reserva un cupo: no puede usarse
todavia ni permitir que otra entrega exceda el limite. El jugador puede seguir jugando.
G/H usan exactamente la misma animacion y solo omiten la restriccion temporal.
Las normales conservan su recepcion corta habitual. Las entregas potentes y los
usos comparten una cola de presentacion para no taparse unos a otros. El final
de ronda cancela las animaciones y libera las reservas pendientes.

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

## Confusion

Normal: 20 puntos, 10 segundos. Potente roja ★: 40 puntos, 16 segundos. Ambas
se aplican al rival cuando termina la animacion de uso. Los controles permanecen
normales; no se invierten movimiento, golpe ni absorcion.

Aparecen 24 estudiantes falsos en la camara del afectado, copiando los sprites,
materiales y colores de los prefabs del generador. Caminan y se reponen mientras
dura el efecto. `Cantidad Fantasmas` en `IlusionesConfusionJugador` permite ajustar
la densidad durante Play. No tienen AlmaEstudiante ni colliders: no ocupan cupos,
no afectan a la profesora ni entregan puntos. Cada camara excluye la capa privada
del otro jugador (FantasmasP1 = 8, FantasmasP2 = 9).

Al pulsar absorber, se compara la distancia del estudiante real y del fantasma
mas cercanos. Si el falso esta mas cerca dentro del alcance, se desvanece en
0.35 segundos y consume esa pulsacion, sin puntos ni stun. Mantener el boton no
encadena intentos. Una absorcion real ya iniciada continua normalmente.

La mano se mezcla una sola vez al comenzar el efecto. Las cartas muestran su
giro durante 0.4 s y despues se deslizan durante 0.55 s hacia los nuevos huecos,
con una elevacion y arco breve para ver el barajado. Al terminar Confusion giran
de nuevo para revelar el frente, conservando el orden mezclado.
Mientras estan volteadas muestran el
reverso neutro, sin texto, costo, estrella ni tinte verde/rojo. El resaltado de
seleccion permanece visible, pero no revela la identidad de la carta.
Se puede seleccionar y usar una carta a ciegas. Al terminar se revela el frente,
conservando el nuevo orden. Nuevas cartas recibidas tambien permanecen ocultas.
Renovar Confusion no mezcla de nuevo ni permite acortar una potente con una normal.
La pausa congela el efecto. Al expirar, desactivar o finalizar la ronda se eliminan
los fantasmas; ambas camaras pueden sufrir Confusion simultaneamente sin compartirlos.
Los falsos aparecen con fade in de 0.35 s y desaparecen con fade out de 0.35 s,
tambien al terminar Confusion. Durante el efecto se renuevan cada 4-7 segundos:
se reubican solo al quedar invisibles y reaparecen gradualmente. No se pueden
absorber mientras aun son invisibles o ya estan desapareciendo. Los fades respetan
la pausa. Desactivar el sistema o terminar la partida limpia todo inmediatamente.

Para probar Confusion, recibirla con G/H (Shift fuerza nivel potente), abrir la
mano con C/M y confirmar con el boton de absorcion configurado. No tiene atajo propio.

## Perfume y Bomba apestosa

La carta Perfume es verde (buff) y su efecto visual rosa. Se aplica al lanzador y atrae estudiantes disponibles dentro de
8 unidades. Esperan en distintos puntos alrededor del jugador, dentro del alcance
de absorcion. La carta Bomba apestosa es roja (debuff), con efecto visual verde, y se aplica al rival: los estudiantes se
alejan caminando un 25 % mas rapido (1.25x), sin entrar en huida permanente.
Con la bomba giran horizontalmente para alejarse y conservan ese nuevo sentido al
salir del alcance o terminar el efecto, retomando su rapidez normal. No se empujan
contra los bordes verticales ni se quedan detenidos en el limite de ZoneMov.

Normal: 20 puntos, 8 segundos. Potente: 40 puntos, 20 segundos. Solo cambia la
duracion; radio e intensidad son iguales. Renovar una normal no acorta una potente.
El alcance de las auras es invisible. Solo se ve un halo difuminado alrededor de la
silueta del personaje: rosa para Perfume, verde para Bomba, sin anillos ni lineas de olor.
Si un jugador tiene ambos efectos, Bomba domina mientras coinciden. Ante varios
jugadores se sigue el olor mas cercano; en igualdad de distancia gana Bomba.

Los estudiantes ocupados, agotados o asustados no cambian de comportamiento;
ninguna aura interrumpe una absorcion en curso. Los fantasmas imitan la reaccion
para no delatar a los estudiantes reales. Los movimientos respetan ZoneMov.
Las auras no alteran puntos, controles ni velocidad de los jugadores. Se limpian
al finalizar la ronda o desactivar el sistema y sus duraciones respetan la pausa.

F/U entregan Perfume a P1/P2; R/P entregan Bomba a P1/P2. Shift elige potente.
N queda libre para la absorcion de P2, sin generar cartas al pulsarlo.
