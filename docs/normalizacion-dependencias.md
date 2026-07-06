# Normalizacion de dependencias

## Problema detectado

La tabla `Dependencia` tenia registros duplicados para el mismo espacio institucional:

- `Direccion` y `Dirección`.
- `Laboratorio Computacion` y `Laboratorio de Computación`.

Esto genera inconsistencia porque una asignacion puede apuntar a una version y otra asignacion a otra version, aunque ambas representan el mismo lugar fisico.

## Criterio aplicado

Se definio una sola dependencia canonica por espacio:

- `Dirección`, responsable `Directora`.
- `Laboratorio de Computación`, responsable `Encargada Tecnología`.
- `Biblioteca`, responsable `Encargada Biblioteca`.
- `Sala 1 Basico A`, responsable `Docente jefe`.
- `Sala 4 Basico B`, responsable `Docente jefe`.

Para detectar duplicados se usa un nombre normalizado:

- Convierte a mayusculas.
- Elimina tildes.
- Elimina signos no relevantes.
- Trata `de` como palabra no diferenciadora para este catalogo.

Ejemplo: `Laboratorio Computacion` y `Laboratorio de Computación` se normalizan como `LABORATORIO COMPUTACION`.

## Integridad referencial

Antes de eliminar duplicados, el sistema actualiza las tablas relacionadas:

- `Asignaciones.id_dependencia`.
- `Salida_insumo.id_dependencia`, si la tabla existe.

Luego elimina el registro duplicado. Esto conserva el historial y evita referencias rotas.

## Prevencion

La base de datos tiene el indice unico `UX_Dependencia_Nombre_Normalizado` sobre `Dependencia.nombre_dependencia_normalizado`.

Ademas, el formulario de creacion valida antes de guardar y muestra un mensaje claro si se intenta registrar una dependencia duplicada por tildes o variaciones de escritura.
