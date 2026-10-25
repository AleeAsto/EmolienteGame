# Integracion de assets del 25 de octubre

Origen: GAME JAM EMOLIENTES-20261025T213204Z-1-001. Los PNG originales se copiaron
sin alterar sus pixeles a Assets/Images/ArteFinal. Los recortes son metadatos de
sprites de Unity; se conservan los archivos originales del equipo de arte.

## Menu y UI

MenuAudioMenus utiliza el logo Social Outcasts y cinco botones separados: Play,
How to Play, Options, Credits y Quit. History queda fuera por decision del equipo.
Cada boton conserva sus eventos reales y tiene navegacion vertical explicita,
indicador rojo y respuesta al foco de teclado o al pasar el raton.

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

## Edicion

Emoliente > Integrar arte final y nuevos efectos vuelve a aplicar la integracion
solo en las dos escenas AudioMenus. Es una herramienta explicita: no se ejecuta
automaticamente al importar. Guarda cualquier edicion manual antes de usarla.
ConstruirMenusAudio tambien aplica el estilo del menu al reconstruir sus paneles.

La configuracion de los ocho efectos nuevos esta en Resources/AudioJuego.asset;
sus asignaciones y niveles se describen en Audio.md. Las comprobaciones de Play,
informes de colisiones y capturas de referencia estan en Logs/ArteFinal, ignorado
por Git.
