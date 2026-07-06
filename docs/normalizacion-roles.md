# Normalizacion de roles

## Problema detectado

La tabla `Rol` tenia perfiles duplicados por diferencias de escritura:

- `Encargada Tecnología`.
- `Encargada Tecnologia`.

Tambien existian variantes equivalentes para biblioteca:

- `Encargada Librería`.
- `Encargada Biblioteca`.

Esto afecta la autorizacion porque el sistema usa el nombre del rol para permitir o negar vistas y acciones.

## Criterio aplicado

Se definieron seis roles oficiales:

- `Administrador`.
- `Directora`.
- `Inspector`.
- `Encargada Tecnologia`.
- `Encargada Biblioteca`.
- `Docente`.

Los registros duplicados fueron consolidados y sus referencias fueron migradas al rol oficial correspondiente.

## Tablas actualizadas

Antes de eliminar un rol duplicado, el sistema actualiza:

- `Personal.id_rol`.
- `Usuario.id_rol`.
- `Rol_permisos.id_rol`, si existen permisos asociados.

Esto conserva la integridad referencial y evita que usuarios o funcionarios queden apuntando a roles inexistentes.

## Prevencion

La base de datos tiene el indice unico `UX_Rol_Nombre_Normalizado` sobre `Rol.nombre_rol_normalizado`.

Este indice impide volver a insertar variantes equivalentes, por ejemplo `Encargada Tecnología` cuando ya existe `Encargada Tecnologia`.
