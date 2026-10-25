# Integracion de assets del 25 de octubre

Origen: GAME JAM EMOLIENTES-20261025T213204Z-1-001. Los PNG originales se copiaron
sin alterar sus pixeles a Assets/Images/ArteFinal. Los recortes son metadatos de
sprites de Unity; se conservan los archivos originales del equipo de arte.

## Menu y UI

MenuAudioMenus utiliza el logo Social Outcasts y cinco botones separados: Play,
How to Play, Options, Credits y Quit. History queda fuera por decision del equipo.
Cada boton conserva sus eventos reales y tiene navegacion vertical explicita,
el circulo rojo Seleccion y respuesta al foco de teclado o al pasar el raton.
El logo principal usa ahora el PNG ampliado independiente de la entrega 22:15.

Opciones, controles, pausa y resultado conservan sus comportamientos. Los botones
de opciones, controles y pausa usan papel sobre negro; los volumenes, rojo. La
pantalla de creditos utiliza el encabezado dibujado y conserva los seis nombres.
Los textos de ayuda siguen mostrando las teclas configuradas por los jugadores.

## Pasillo

PartidaAudioMenus usa Fondom y pintarm como fondo limpio, sin muebles dibujados
encima. Los 14 casilleros y las cuatro puertas son objetos interactivos separados
con sus versiones abierta y cerrada a color. Los estados de puerta, glow, entrada,
flash de viaje y cooldown se conservan.

Se verificaron sin cambios los valores de posicion, escala, tamano y offset de
los 26 BoxCollider2D de la escena. Los casilleros dimensionan el renderer mediante
SpriteDrawMode.Sliced, sin escalar su Transform fisico al sustituir el dibujo.
La puerta abierta exportada tiene un velo de baja opacidad fuera del dibujo:
MaterialPuerta usa SpriteRecorteAlpha para descartarlo al renderizar. El interior
blanco de la ilustracion se conserva tal como fue entregado.

Las animaciones y sprites de personajes de Ale se conservan. No se duplicaron
las hojas de animacion que ya estaba integrando ella. movimiento.ogg sigue
excluido porque pertenece a otro proyecto.

## Ilustraciones de cartas (entrega 22:15)

Se importaron los doce PNG de Tarjetas: cinco frentes y un reverso, cada uno en
version pequena y ampliada. Se recortan sus margenes transparentes en los metadatos
de Unity, sin editar los originales. Las trece cartas del catalogo usan este arte.
Speed representa aceleracion y pesadez; Bomb Perfume representa perfume y bomba.
Sus nombres en espanol y la ficha de efecto distinguen cada comportamiento.

La mano usa los sprites pequenos; al seleccionar, recibir o lanzar una carta se
usa la ilustracion ampliada. Se conserva la proporcion y los colores del dibujo.
Solo el precio de uso aparece en una etiqueta encima del arte, tanto en la mano
como durante usos y entregas. No se muestran descripciones, nombres adicionales,
niveles ni fichas laterales: los jugadores descubren los efectos jugando. Al abrir
la mano las tres cartas se elevan. Las potentes conservan el marco dorado y las
cartas sin almas suficientes se atenuan.

Confusion oculta los frentes y el precio; usa el reverso dibujado. Los usos y entregas
conservan sus giros y trayectos de UI. No se modificaron costes, duraciones,
efectos, reglas de reparto ni animaciones de personajes.

## Edicion

Emoliente > Integrar arte final y nuevos efectos vuelve a aplicar la integracion
solo en las dos escenas AudioMenus. Es una herramienta explicita: no se ejecuta
automaticamente al importar. Guarda cualquier edicion manual antes de usarla.
ConstruirMenusAudio tambien aplica el estilo del menu al reconstruir sus paneles.
Emoliente > Integrar cartas y selector del menu actualiza solo los sprites del
catalogo y la escena de menu, sin reconstruir el mapa. Las comprobaciones de esta
entrega y las capturas estan en Logs/CartasArte, ignorado por Git.

La configuracion de los ocho efectos nuevos esta en Resources/AudioJuego.asset;
sus asignaciones y niveles se describen en Audio.md. Las comprobaciones de Play,
informes de colisiones y capturas de referencia estan en Logs/ArteFinal, ignorado
por Git.
