# Sistema Escolar Web

Primera base funcional del proyecto ASP.NET Core MVC para tesis.

## Incluye

- ASP.NET Core MVC .NET 8
- SQL Server con conexión a `LAPTOP-RJDFHG1A`
- Login con tabla `Usuario`, `Personal` y `Rol`
- Claims y Cookie Authentication
- Roles oficiales: Administrador, Directora, Inspector, Encargada Tecnología, Encargada Librería, Docente
- Normalización temporal: si la BD aún tiene rol `Director`, el sistema lo trata como `Inspector`
- Sidebar dinámico por rol
- Dashboard por rol
- Layout institucional Bootstrap 5
- Controladores protegidos por roles/policies

## Ejecutar

```bash
dotnet restore
dotnet build
dotnet run
```

Abrir:

```text
http://localhost:5000
```

Usuario inicial:

```text

admin@colegio.com
Admin123*
```
