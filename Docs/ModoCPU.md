# Modo CPU

En `MenuAudioMenus`, Jugar abre CPU / PvP. CPU abre Facil / Normal / Dificil
(Normal seleccionado al entrar). Escape y Volver retroceden un nivel.
P1 conserva sus controles configurados; la CPU sustituye a P2 y conserva la
pantalla dividida. Revancha mantiene modo y dificultad. Elegir PvP restaura
ambos jugadores humanos.

## Reglas compartidas

`EntradaJugador` elige teclado o intenciones de la CPU. Los componentes
existentes siguen resolviendo movimiento, colisiones, drenaje, golpes, stun,
invulnerabilidad, puertas, pago de cartas y recompensas. La CPU no recibe
almas, velocidad, alcance, dano, cartas ni cooldowns especiales.

`ConfiguracionPartida` conserva la seleccion durante la sesion. `CicloPartida`
crea `ControladorCPU` exclusivamente en P2 al comenzar una partida CPU.
No hace falta duplicar ni modificar la escena de juego para cada modo.

## Decisiones

- Compara recompensa y tiempo de viaje/drenaje de estudiantes visibles en
  cualquiera de las dos vistas. Da valor adicional a los raros y sus cartas.
- Prepara la captura mientras sigue acercandose, en vez de detenerse en el
  borde del alcance. Una captura real se mantiene cada frame hasta completarse,
  salvo una amenaza inmediata. Suelta el boton tras consumir un intento.
- Ambos jugadores quedan inmoviles durante la absorcion, con puertas y dash
  directo bloqueados. Soltar, recibir un golpe o cancelar para huir libera el
  movimiento; los empujones de golpes siguen aplicandose tras la cancelacion.
- Estima una interrupcion con longitud de ruta, velocidad efectiva, reaccion,
  preparacion y cooldown. La compara con el drenaje restante real del rival.
- Respeta la invulnerabilidad; puede contraatacar o abandonar una succion
  peligrosa. La esquiva de la profesora tiene prioridad cada frame, incluso
  eligiendo cartas o con proteccion a punto de terminar. Predice ambos
  movimientos con las cajas de contacto reales y valora rutas por encima o
  por debajo del carril, considerando limites y casilleros.
- En dificil inicia golpes cercanos aunque el rival tambien este preparado.
  Estima su velocidad observando desplazamientos, anticipa el punto de alcance
  y lo acompana mientras prepara el golpe. No lee las teclas del rival.
- Mantiene brevemente una persecucion para no cambiar de idea en cada revision.
  Si no puede ganar una carrera recta, vuelve a valorar recoleccion y cartas.
  Evita entrar en un ataque cuando tiene su propio golpe en cooldown, y se
  posiciona antes de que termine la invulnerabilidad sin golpearla.
- Las cartas se valoran por efecto, coste, situacion y tiempo de partida.
  Se abren y confirman mediante la mano normal, con el tiempo de mantener
  configurado. El latigo genera una persecucion mientras dure su marca.
  Espera a que el latigo se active antes de gastar el golpe y a que el swap
  se resuelva antes de empezar un drenaje que ese viaje cancelaria.
  El swap valora el viaje ahorrado y la interrupcion del drenaje rival,
  especialmente un raro. Descarta ese objetivo si el drenaje termina antes
  de activarse, y rechaza una llegada que prevea peligrosa por la profesora.
- Confusion introduce los mismos fantasmas visibles en sus objetivos y
  bloquea la lectura de las identidades de las cartas para decidir su uso.
- La dificil compara el ahorro esperado de las puertas. Nunca elige el
  destino aleatorio; lo sortea `RedPuertas`, con los bloqueos normales.
- Una malla A* del piso rodea los casilleros. El desplazamiento final sigue
  pasando por `MovimientoJugador2D` y sus colisiones.

## Dificultades

| Nivel | Reaccion | Reevaluacion | Comportamiento |
| --- | --- | --- | --- |
| Facil | 0.70 s | 0.65 s | Prioriza cercania con errores de eleccion; interrumpe solo cerca; esquiva tarde a la profesora. |
| Normal | 0.25 s | 0.24 s | Compara rendimiento, calcula interrupciones, evita peligros y valora cartas. |
| Dificil | 0.08 s | 0.09 s | Mayor anticipacion, disputa de raros, estrategia por marcador y uso de atajos aleatorios. |

Son parametros iniciales para ajustar jugando. No se garantiza que la CPU
dificil gane siempre frente a cualquier jugador.

## Editor

`Emoliente/Agregar seleccion de modo CPU` actualiza solo las dos pantallas
nuevas del menu actual, conservando las cuatro pantallas anteriores.
`ConstruirMenusAudio` tambien incluye ese paso al regenerar el menu.
No modifica assets ni animaciones de los personajes.
