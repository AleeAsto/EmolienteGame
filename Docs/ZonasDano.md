# Cajas de dano en el pasillo

Cada jugador tiene un hijo `ZonaDano` con `ZonaDanoJugador` y un BoxCollider2D
trigger propio. `EstadoCombateJugador > Zona Dano` referencia ese componente.
Los golpes normales, el latigo y el contacto con la profesora consultan esta caja.
La caja original de los pies sigue sirviendo para movimiento y puertas.

La nueva caja comienza centrada en los pies y es un poco menor que la de
movimiento. Ajustar Size y Offset en su BoxCollider2D permite cambiar la zona
vulnerable sin alterar las puertas. Al seleccionar ZonaDano se dibuja en naranja.
Para esta perspectiva conviene mantenerla baja y poco profunda: las cabezas y
los cuerpos pueden superponerse al pasar por filas distintas del pasillo.

La profesora tiene un hijo `ZonaContacto` con una caja trigger de 0.8 x 0.4.
Se ajusta desde `ProfesoraPasillo > Zona Contacto`. Se comprueba el recorrido
horizontal completo de cada frame para evitar atravesar jugadores sin detectar
el impacto. La caja de contacto de la profesora debe mantenerse sin rotacion.
Su stun y empujon conservan los valores anteriores. No se inflige dano durante
la reaparicion fuera de pantalla ni durante la invulnerabilidad del jugador.

Los ataques ahora parten de los pies. El golpe normal comprueba su rectangulo
frontal contra ZonaDano; el latigo comprueba su elipse contra la caja completa,
incluidos sus bordes, offsets, escala y rotacion.

`Emoliente > Verificar zonas de dano` ejecuta comprobaciones geometricas en una
escena de preview sin modificar la escena abierta. Para ajustar la sensacion en
Play: pasar delante y detras de la profesora, rozar el borde, recibir un golpe
de frente y repetir con el latigo. No agrandar la caja hasta la cabeza: eso
convertiria la superposicion visual entre filas en un contacto injusto.
