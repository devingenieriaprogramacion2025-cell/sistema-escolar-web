using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Helpers;
using SistemaEscolarWeb.Models;
using SistemaEscolarWeb.Permissions;

namespace SistemaEscolarWeb.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            await context.Database.EnsureCreatedAsync();
            await EnsureOptionalTablesAsync(context);
            await EnsureAuditUserColumnLengthsAsync(context);
            await EnsureAsignacionesNullableDestinatariosAsync(context);
            await EnsureSolicitudImpresionCantidadPaginasAsync(context);
            await SeedCatalogosTecnologiaAsync(context);
            await SeedRolesAsync(context);
            await SeedPermisosAsync(context);
            await SeedDependenciasAsync(context);
            await EnsureDependenciaUniqueIndexAsync(context);
            await SeedPersonalYUsuariosAsync(context);
            await SeedTecnologiaAsync(context);
            await SeedFlujosAsync(context);
            await NormalizeEstadosImpresionAsync(context);
            await SeedInsumosAsync(context);
            await SeedBitacoraAsync(context);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "No se pudo inicializar la base de datos. Revise la cadena de conexion en appsettings.json.");
        }
    }

    private static async Task EnsureOptionalTablesAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[Insumo]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[Tipo_insumo]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Tipo_insumo](
            [id_tipoinsumo] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [nombre_tipoinsumo] nvarchar(120) NOT NULL
        );
    END

    CREATE TABLE [Insumo](
        [id_insumo] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_tipoinsumo] int NOT NULL,
        [nombre_insumo] nvarchar(160) NOT NULL,
        [descripcion_insumo] nvarchar(250) NULL,
        [unidad_medida] nvarchar(40) NOT NULL,
        [estado] bit NOT NULL,
        [toxicidad] nvarchar(80) NULL,
        [stock_actual] int NOT NULL,
        [stock_minimo] int NOT NULL,
        CONSTRAINT [FK_Insumo_Tipo_insumo] FOREIGN KEY ([id_tipoinsumo]) REFERENCES [Tipo_insumo]([id_tipoinsumo])
    );
END
IF OBJECT_ID(N'[Entrada_insumo]', N'U') IS NULL
BEGIN
    CREATE TABLE [Entrada_insumo](
        [id_entradainsumo] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_insumo] int NOT NULL,
        [id_proveedor] int NOT NULL,
        [numero_factura] nvarchar(80) NOT NULL,
        [fecha_entrega] date NOT NULL,
        [cantidad] int NOT NULL,
        CONSTRAINT [FK_Entrada_insumo_Insumo] FOREIGN KEY ([id_insumo]) REFERENCES [Insumo]([id_insumo]),
        CONSTRAINT [FK_Entrada_insumo_Proveedor] FOREIGN KEY ([id_proveedor]) REFERENCES [Proveedor]([id_proveedor])
    );
END
IF OBJECT_ID(N'[Salida_insumo]', N'U') IS NULL
BEGIN
    CREATE TABLE [Salida_insumo](
        [id_salidainsumo] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_insumo] int NOT NULL,
        [id_dependencia] int NOT NULL,
        [rut_personal] nvarchar(40) NOT NULL,
        [fecha_salida] date NOT NULL,
        [cantidad] int NOT NULL,
        CONSTRAINT [FK_Salida_insumo_Insumo] FOREIGN KEY ([id_insumo]) REFERENCES [Insumo]([id_insumo]),
        CONSTRAINT [FK_Salida_insumo_Dependencia] FOREIGN KEY ([id_dependencia]) REFERENCES [Dependencia]([id_dependencia]),
        CONSTRAINT [FK_Salida_insumo_Personal] FOREIGN KEY ([rut_personal]) REFERENCES [Personal]([rut_personal])
    );
END
IF OBJECT_ID(N'[Bitacoras]', N'U') IS NULL
BEGIN
    CREATE TABLE [Bitacoras](
        [IdBitacora] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Usuario] nvarchar(160) NOT NULL,
        [Rol] nvarchar(80) NOT NULL,
        [Modulo] nvarchar(80) NOT NULL,
        [Accion] nvarchar(180) NOT NULL,
        [Fecha] datetime2 NOT NULL
    );
END
IF OBJECT_ID(N'[Permisos]', N'U') IS NULL
BEGIN
    CREATE TABLE [Permisos](
        [id_permiso] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre_permiso] nvarchar(120) NOT NULL
    );
END
IF OBJECT_ID(N'[Rol_permisos]', N'U') IS NULL
BEGIN
    CREATE TABLE [Rol_permisos](
        [id_rol] int NOT NULL,
        [id_permiso] int NOT NULL,
        [fecha_rol] datetime2 NOT NULL,
        [activo] bit NOT NULL,
        CONSTRAINT [PK_Rol_permisos] PRIMARY KEY ([id_rol], [id_permiso]),
        CONSTRAINT [FK_Rol_permisos_Rol] FOREIGN KEY ([id_rol]) REFERENCES [Rol]([id_rol]),
        CONSTRAINT [FK_Rol_permisos_Permiso] FOREIGN KEY ([id_permiso]) REFERENCES [Permisos]([id_permiso])
    );
END
IF OBJECT_ID(N'[Rol_permisos]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'Rol_permisos', N'fecha_rol') IS NULL
        ALTER TABLE [Rol_permisos] ADD [fecha_rol] datetime2 NOT NULL CONSTRAINT [DF_Rol_permisos_fecha_rol] DEFAULT SYSUTCDATETIME();

    IF COL_LENGTH(N'Rol_permisos', N'activo') IS NULL
        ALTER TABLE [Rol_permisos] ADD [activo] bit NOT NULL CONSTRAINT [DF_Rol_permisos_activo] DEFAULT 1;

    ;WITH Duplicados AS (
        SELECT *,
            ROW_NUMBER() OVER (
                PARTITION BY [id_rol], [id_permiso]
                ORDER BY [activo] DESC, [fecha_rol] DESC
            ) AS rn
        FROM [Rol_permisos]
    )
    DELETE FROM Duplicados
    WHERE rn > 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Rol_permisos_Rol_Permiso' AND object_id = OBJECT_ID(N'[Rol_permisos]'))
        CREATE UNIQUE INDEX [UX_Rol_permisos_Rol_Permiso] ON [Rol_permisos]([id_rol], [id_permiso]);
END
""");
    }

    private static async Task SeedPermisosAsync(ApplicationDbContext context)
    {
        var permisos = new[]
        {
            "dashboard.ver",
            "insumos.gestionar",
            "tecnologia.gestionar",
            "tecnologia.asignar",
            "reparaciones.solicitar",
            "reparaciones.aprobar",
            "bajas.solicitar",
            "bajas.aprobar",
            "impresiones.solicitar",
            "impresiones.gestionar",
            "personas.ver",
            "dependencias.ver",
            "usuarios.gestionar",
            "Ver usuarios",
            "Crear usuarios",
            "Editar usuarios",
            "Eliminar usuarios",
            "Ver tecnología",
            "Crear tecnología",
            "Editar tecnología",
            "Eliminar tecnología",
            "Ver insumos",
            "Crear insumos",
            "Editar insumos",
            "Eliminar insumos",
            "Ver asignaciones",
            "Crear asignaciones",
            "Aprobar asignaciones",
            "Ver reparaciones",
            "Crear reparaciones",
            "Aprobar reparaciones",
            "Ver bajas",
            "Solicitar bajas",
            "Aprobar bajas",
            "Ver impresiones",
            "Solicitar impresiones",
            "Aprobar impresiones",
            "Ver reportes",
            "Ver bitácora"
        };

        foreach (var permiso in permisos)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [nombre_permiso] = {permiso})
BEGIN
    INSERT INTO [Permisos] ([nombre_permiso]) VALUES ({permiso});
END
""");
        }

        await context.Database.ExecuteSqlRawAsync("""
DECLARE @AdminId int = (
    SELECT TOP 1 [id_rol]
    FROM [Rol]
    WHERE [nombre_rol] = N'Administrador'
    ORDER BY [id_rol]
);

IF @AdminId IS NOT NULL
BEGIN
    INSERT INTO [Rol_permisos] ([id_rol], [id_permiso], [fecha_rol], [activo])
    SELECT @AdminId, p.[id_permiso], SYSDATETIME(), 1
    FROM [Permisos] p
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Rol_permisos] rp
        WHERE rp.[id_rol] = @AdminId
          AND rp.[id_permiso] = p.[id_permiso]
    );

    UPDATE rp
    SET [activo] = 1
    FROM [Rol_permisos] rp
    WHERE rp.[id_rol] = @AdminId;
END
""");
    }

    private static async Task EnsureAsignacionesNullableDestinatariosAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[Asignaciones]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[Asignaciones]')
          AND name = N'id_dependencia'
          AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [Asignaciones] ALTER COLUMN [id_dependencia] int NULL;
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[Asignaciones]')
          AND name = N'rut_personal'
          AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [Asignaciones] ALTER COLUMN [rut_personal] nvarchar(20) NULL;
    END
END
""");
    }

    private static async Task EnsureSolicitudImpresionCantidadPaginasAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[Solicitud_impresion]', N'U') IS NOT NULL
   AND COL_LENGTH(N'Solicitud_impresion', N'cantidad_paginas') IS NULL
BEGIN
    ALTER TABLE [Solicitud_impresion]
    ADD [cantidad_paginas] int NOT NULL
        CONSTRAINT [DF_Solicitud_impresion_cantidad_paginas] DEFAULT 1;
END
""");
    }

    private static async Task EnsureAuditUserColumnLengthsAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[De_baja]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'De_baja', N'usuario_registra_baja') < 320
        ALTER TABLE [De_baja] ALTER COLUMN [usuario_registra_baja] nvarchar(160) NULL;

    IF COL_LENGTH(N'De_baja', N'usuario_autoriza_baja') < 320
        ALTER TABLE [De_baja] ALTER COLUMN [usuario_autoriza_baja] nvarchar(160) NULL;
END

IF OBJECT_ID(N'[Reparacion]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'Reparacion', N'usuario_solicita') < 320
        ALTER TABLE [Reparacion] ALTER COLUMN [usuario_solicita] nvarchar(160) NULL;

    IF COL_LENGTH(N'Reparacion', N'usuario_aprueba') < 320
        ALTER TABLE [Reparacion] ALTER COLUMN [usuario_aprueba] nvarchar(160) NULL;
END
""");
    }

    private static async Task SeedCatalogosTecnologiaAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM Proveedor)
BEGIN
    INSERT INTO Proveedor (nombre_proveedor, rut_proveedor, correo, telefono)
    VALUES (N'Proveedor inicial', N'76.000.000-0', N'proveedor@colegio.cl', N'900000000');
END

IF NOT EXISTS (SELECT 1 FROM Marca)
BEGIN
    INSERT INTO Marca (nombre_marca)
    VALUES (N'Generica');
END

IF NOT EXISTS (SELECT 1 FROM Modelo)
BEGIN
    DECLARE @MarcaId int = (SELECT TOP 1 id_marca FROM Marca ORDER BY id_marca);
    INSERT INTO Modelo (id_marca, nombre_modelo)
    VALUES (@MarcaId, N'Modelo institucional');
END

IF NOT EXISTS (SELECT 1 FROM Tipo_tecnologia)
BEGIN
    INSERT INTO Tipo_tecnologia (nombre_tipotecnologia, descripcion)
    VALUES
        (N'Notebook', N'Equipo portatil'),
        (N'Proyector', N'Equipo audiovisual'),
        (N'Impresora', N'Equipo de impresion'),
        (N'Tablet', N'Dispositivo movil');
END

IF NOT EXISTS (SELECT 1 FROM Entrada_tecnologia)
BEGIN
    DECLARE @ProveedorId int = (SELECT TOP 1 id_proveedor FROM Proveedor ORDER BY id_proveedor);
    INSERT INTO Entrada_tecnologia (id_proveedor, fecha_entrada, cantidad, numero_factura)
    VALUES (@ProveedorId, CAST(GETDATE() AS date), 5, N'INI-2026');
END
""");
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        await NormalizeRolesAsync(context);

        var roles = RoleNames.All;

        var existentes = await context.Roles.ToListAsync();
        foreach (var rol in roles)
        {
            var rolNormalizado = InputValidationHelper.NormalizeKey(RoleNames.Normalize(rol));
            var existente = existentes.FirstOrDefault(r => InputValidationHelper.NormalizeKey(RoleNames.Normalize(r.NombreRol)) == rolNormalizado);

            if (existente == null)
            {
                context.Roles.Add(new Rol { NombreRol = rol });
            }
            else
            {
                existente.NombreRol = rol;
            }
        }

        await context.SaveChangesAsync();
        await NormalizeRolesAsync(context);
        await EnsureRoleUniqueIndexAsync(context);
    }

    private static async Task NormalizeRolesAsync(ApplicationDbContext context)
    {
        var canonicos = RoleNames.All.ToDictionary(InputValidationHelper.NormalizeKey, rol => rol);

        var roles = await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.IdRol)
            .ToListAsync();

        foreach (var grupo in roles.GroupBy(r => InputValidationHelper.NormalizeKey(RoleNames.Normalize(r.NombreRol))))
        {
            if (!canonicos.TryGetValue(grupo.Key, out var nombreCanonico))
                continue;

            var principal = grupo.FirstOrDefault(r => r.NombreRol == nombreCanonico) ?? grupo.First();

            foreach (var duplicado in grupo.Where(r => r.IdRol != principal.IdRol))
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Personal]
SET [id_rol] = {principal.IdRol}
WHERE [id_rol] = {duplicado.IdRol};

UPDATE [Usuario]
SET [id_rol] = {principal.IdRol}
WHERE [id_rol] = {duplicado.IdRol};

IF OBJECT_ID(N'[Rol_permisos]', N'U') IS NOT NULL
BEGIN
    UPDATE [Rol_permisos]
    SET [id_rol] = {principal.IdRol}
    WHERE [id_rol] = {duplicado.IdRol}
      AND NOT EXISTS (
          SELECT 1
          FROM [Rol_permisos] rp
          WHERE rp.[id_rol] = {principal.IdRol}
            AND rp.[id_permiso] = [Rol_permisos].[id_permiso]
      );

    DELETE FROM [Rol_permisos]
    WHERE [id_rol] = {duplicado.IdRol};
END

DELETE FROM [Rol]
WHERE [id_rol] = {duplicado.IdRol};
""");
            }

            await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Rol]
SET [nombre_rol] = {nombreCanonico}
WHERE [id_rol] = {principal.IdRol};
""");
        }

        context.ChangeTracker.Clear();
    }

    private static async Task EnsureRoleUniqueIndexAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'Rol', N'nombre_rol_normalizado') IS NULL
BEGIN
    ALTER TABLE [Rol]
    ADD [nombre_rol_normalizado] AS
        LTRIM(RTRIM(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(
                                                REPLACE(UPPER([nombre_rol]), N'Á', N'A'),
                                            N'É', N'E'),
                                        N'Í', N'I'),
                                    N'Ó', N'O'),
                                N'Ú', N'U'),
                            N'Ü', N'U'),
                        N'Ñ', N'N'),
                    N'LIBRERIA', N'BIBLIOTECA'),
                N'ENCARGADO TECNOLOGIA', N'ENCARGADA TECNOLOGIA'),
            N'  ', N' ')
        )) PERSISTED;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Rol_Nombre_Normalizado'
      AND object_id = OBJECT_ID(N'Rol')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Rol_Nombre_Normalizado]
    ON [Rol]([nombre_rol_normalizado]);
END
""");
    }

    private static async Task SeedDependenciasAsync(ApplicationDbContext context)
    {
        await NormalizeDependenciasAsync(context);

        var dependencias = new[]
        {
            new Dependencia { IdTipoDependencia = 1, NombreDependencia = "Dirección", ResponsableDependencia = "Director" },
            new Dependencia { IdTipoDependencia = 1, NombreDependencia = "Laboratorio de Computación", ResponsableDependencia = "Encargada Tecnología" },
            new Dependencia { IdTipoDependencia = 2, NombreDependencia = "Biblioteca", ResponsableDependencia = "Encargado Biblioteca" },
            new Dependencia { IdTipoDependencia = 3, NombreDependencia = "Sala 1 Basico A", ResponsableDependencia = "Profesor jefe" },
            new Dependencia { IdTipoDependencia = 3, NombreDependencia = "Sala 4 Basico B", ResponsableDependencia = "Profesor jefe" }
        };

        var existentes = await context.Dependencias.ToListAsync();
        foreach (var dependencia in dependencias)
        {
            var dependenciaNormalizada = InputValidationHelper.NormalizeKey(dependencia.NombreDependencia);
            var existente = existentes.FirstOrDefault(d => InputValidationHelper.NormalizeKey(d.NombreDependencia) == dependenciaNormalizada);

            if (existente == null)
            {
                context.Dependencias.Add(dependencia);
            }
            else
            {
                existente.IdTipoDependencia = dependencia.IdTipoDependencia;
                existente.NombreDependencia = dependencia.NombreDependencia;
                existente.ResponsableDependencia = dependencia.ResponsableDependencia;
            }
        }

        await context.SaveChangesAsync();
        await NormalizeDependenciasAsync(context);
    }

    private static async Task NormalizeDependenciasAsync(ApplicationDbContext context)
    {
        var canonicas = new Dictionary<string, (string Nombre, string Responsable, int Tipo)>
        {
            [InputValidationHelper.NormalizeKey("Dirección")] = ("Dirección", "Director", 1),
            [InputValidationHelper.NormalizeKey("Laboratorio de Computación")] = ("Laboratorio de Computación", "Encargada Tecnología", 1),
            [InputValidationHelper.NormalizeKey("Biblioteca")] = ("Biblioteca", "Encargado Biblioteca", 2),
            [InputValidationHelper.NormalizeKey("Sala 1 Basico A")] = ("Sala 1 Basico A", "Profesor jefe", 3),
            [InputValidationHelper.NormalizeKey("Sala 4 Basico B")] = ("Sala 4 Basico B", "Profesor jefe", 3)
        };

        var dependencias = await context.Dependencias
            .AsNoTracking()
            .OrderBy(d => d.IdDependencia)
            .ToListAsync();

        foreach (var grupo in dependencias.GroupBy(d => InputValidationHelper.NormalizeKey(d.NombreDependencia)))
        {
            var canonica = canonicas.GetValueOrDefault(grupo.Key);
            var principal = canonica.Nombre == null
                ? grupo.First()
                : grupo.FirstOrDefault(d => d.NombreDependencia == canonica.Nombre) ?? grupo.First();

            foreach (var duplicada in grupo.Where(d => d.IdDependencia != principal.IdDependencia))
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Asignaciones]
SET [id_dependencia] = {principal.IdDependencia}
WHERE [id_dependencia] = {duplicada.IdDependencia};

IF OBJECT_ID(N'[Salida_insumo]', N'U') IS NOT NULL
BEGIN
    UPDATE [Salida_insumo]
    SET [id_dependencia] = {principal.IdDependencia}
    WHERE [id_dependencia] = {duplicada.IdDependencia};
END

DELETE FROM [Dependencia]
WHERE [id_dependencia] = {duplicada.IdDependencia};
""");
            }

            if (canonica.Nombre != null)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Dependencia]
SET [id_tipodependencia] = {canonica.Tipo},
    [nombre_dependencia] = {canonica.Nombre},
    [responsable_dependencia] = {canonica.Responsable}
WHERE [id_dependencia] = {principal.IdDependencia};
""");
            }
        }

        context.ChangeTracker.Clear();
    }

    private static async Task EnsureDependenciaUniqueIndexAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'Dependencia', N'nombre_dependencia_normalizado') IS NULL
BEGIN
    ALTER TABLE [Dependencia]
    ADD [nombre_dependencia_normalizado] AS
        LTRIM(RTRIM(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(UPPER([nombre_dependencia]), N'Á', N'A'),
                                        N'É', N'E'),
                                    N'Í', N'I'),
                                N'Ó', N'O'),
                            N'Ú', N'U'),
                        N'Ü', N'U'),
                    N'Ñ', N'N'),
                N' DE ', N' '),
            N'  ', N' ')
        )) PERSISTED;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Dependencia_Nombre_Normalizado'
      AND object_id = OBJECT_ID(N'Dependencia')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Dependencia_Nombre_Normalizado]
    ON [Dependencia]([nombre_dependencia_normalizado]);
END
""");
    }

    private static async Task SeedPersonalYUsuariosAsync(ApplicationDbContext context)
    {
        var roles = await context.Roles.ToDictionaryAsync(r => r.NombreRol, r => r.IdRol);
        var personasBase = new[]
        {
            new Personal { RutPersonal = "11.111.111-1", Nombre = "Admin", Apellido = "Sistema", Correo = "admin@colegio.cl", Telefono = "900000001", Cargo = "Administrador", IdRol = roles[RoleNames.Administrador], Activo = true },
            new Personal { RutPersonal = "22.222.222-2", Nombre = "Mariela", Apellido = "Rojas", Correo = "directora@colegio.cl", Telefono = "900000002", Cargo = "Director", IdRol = roles[RoleNames.Director], Activo = true },
            new Personal { RutPersonal = "33.333.333-3", Nombre = "Carlos", Apellido = "Vega", Correo = "inspector@colegio.cl", Telefono = "900000003", Cargo = "Inspector", IdRol = roles[RoleNames.Inspector], Activo = true },
            new Personal { RutPersonal = "44.444.444-4", Nombre = "Paula", Apellido = "Molina", Correo = "secretaria@colegio.cl", Telefono = "900000004", Cargo = "Secretaria", IdRol = roles[RoleNames.Secretaria], Activo = true },
            new Personal { RutPersonal = "55.555.555-5", Nombre = "Laura", Apellido = "Silva", Correo = "biblioteca@colegio.cl", Telefono = "900000005", Cargo = "Encargado Biblioteca", IdRol = roles[RoleNames.EncargadoBiblioteca], Activo = true },
            new Personal { RutPersonal = "66.666.666-6", Nombre = "Ana", Apellido = "Fuentes", Correo = "profesor@colegio.cl", Telefono = "900000006", Cargo = "Profesor", IdRol = roles[RoleNames.Profesor], Activo = true }
        };

        foreach (var persona in personasBase)
        {
            var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(persona.RutPersonal);
            var existente = await context.Personal.FirstOrDefaultAsync(p =>
                p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
            if (existente == null)
            {
                context.Personal.Add(persona);
            }
            else
            {
                existente.IdRol = persona.IdRol;
                existente.Nombre = persona.Nombre;
                existente.Apellido = persona.Apellido;
                existente.Correo = persona.Correo;
                existente.Telefono = persona.Telefono;
                existente.Cargo = persona.Cargo;
                existente.Activo = true;
            }
        }

        await context.SaveChangesAsync();

        var password = "Admin123*";
        foreach (var persona in personasBase)
        {
            var rutKey = ChileanFormatHelper.NormalizeRutLookupKey(persona.RutPersonal);
            var rutRegistrado = await context.Personal
                .Where(p => p.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey)
                .Select(p => p.RutPersonal)
                .FirstAsync();

            if (!await context.Usuarios.AnyAsync(u =>
                u.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey))
            {
                context.Usuarios.Add(new Usuario
                {
                    RutPersonal = rutRegistrado,
                    IdRol = persona.IdRol,
                    PasswordHash = password,
                    Activo = true,
                    CreadoEn = DateTime.Now
                });
            }
            else
            {
                var usuario = await context.Usuarios.FirstAsync(u =>
                    u.RutPersonal.ToUpper().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty) == rutKey);
                usuario.IdRol = persona.IdRol;
                usuario.PasswordHash = password;
                usuario.Activo = true;
            }
        }

        foreach (var usuario in await context.Usuarios.ToListAsync())
        {
            usuario.PasswordHash = password;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTecnologiaAsync(ApplicationDbContext context)
    {
        if (await context.Tecnologias.AnyAsync()) return;

        var modeloId = await context.Database.SqlQueryRaw<int>("SELECT TOP 1 id_modelo AS Value FROM Modelo ORDER BY id_modelo").FirstAsync();
        var entradaId = await context.Database.SqlQueryRaw<int>("SELECT TOP 1 id_entradatecnologia AS Value FROM Entrada_tecnologia ORDER BY id_entradatecnologia").FirstAsync();
        var tipos = await context.Database.SqlQueryRaw<int>("SELECT id_tipotecnologia AS Value FROM Tipo_tecnologia ORDER BY id_tipotecnologia").ToListAsync();
        var tipoNotebook = tipos.ElementAtOrDefault(0);
        var tipoProyector = tipos.ElementAtOrDefault(1) == 0 ? tipoNotebook : tipos.ElementAtOrDefault(1);
        var tipoImpresora = tipos.ElementAtOrDefault(2) == 0 ? tipoNotebook : tipos.ElementAtOrDefault(2);
        var tipoTablet = tipos.ElementAtOrDefault(3) == 0 ? tipoNotebook : tipos.ElementAtOrDefault(3);

        context.Tecnologias.AddRange(
            new Tecnologia { IdModelo = modeloId, IdTipoTecnologia = tipoNotebook, IdEntradaTecnologia = entradaId, Estado = true, SkuCodigoInventario = "NB-001" },
            new Tecnologia { IdModelo = modeloId, IdTipoTecnologia = tipoNotebook, IdEntradaTecnologia = entradaId, Estado = true, SkuCodigoInventario = "NB-002" },
            new Tecnologia { IdModelo = modeloId, IdTipoTecnologia = tipoProyector, IdEntradaTecnologia = entradaId, Estado = true, SkuCodigoInventario = "PROY-001" },
            new Tecnologia { IdModelo = modeloId, IdTipoTecnologia = tipoImpresora, IdEntradaTecnologia = entradaId, Estado = true, SkuCodigoInventario = "IMP-001" },
            new Tecnologia { IdModelo = modeloId, IdTipoTecnologia = tipoTablet, IdEntradaTecnologia = entradaId, Estado = true, SkuCodigoInventario = "TAB-001" });

        await context.SaveChangesAsync();
    }

    private static async Task SeedFlujosAsync(ApplicationDbContext context)
    {
        if (!await context.EstadosImpresion.AnyAsync())
        {
            context.EstadosImpresion.AddRange(
                new EstadoImpresion { Estado = Estado.Pendiente },
                new EstadoImpresion { Estado = Estado.EnProceso },
                new EstadoImpresion { Estado = Estado.Entregada },
                new EstadoImpresion { Estado = Estado.Rechazada });
            await context.SaveChangesAsync();
        }

        await NormalizeEstadosImpresionAsync(context);

        if (!await context.Asignaciones.AnyAsync())
        {
            var notebook = await context.Tecnologias.FirstAsync(t => t.SkuCodigoInventario == "NB-001");
            var sala = await context.Dependencias
                .OrderByDescending(d => d.NombreDependencia.Contains("Sala"))
                .FirstAsync();
            var profesorRut = await context.Personal
                .Where(p => p.Correo == "profesor@colegio.cl")
                .Select(p => p.RutPersonal)
                .FirstAsync();
            context.Asignaciones.Add(new Asignacion
            {
                IdTecnologia = notebook.IdTecnologia,
                IdDependencia = sala.IdDependencia,
                RutPersonal = profesorRut,
                FechaAsignacion = DateTime.Now.AddDays(-12),
                TipoAsignacion = "Uso docente",
                EstadoAsignacion = "Vigente"
            });
        }

        if (!await context.Reparaciones.AnyAsync())
        {
            var proyector = await context.Tecnologias.FirstAsync(t => t.SkuCodigoInventario == "PROY-001");
            context.Reparaciones.Add(new Reparacion
            {
                IdTecnologia = proyector.IdTecnologia,
                Destino = "Servicio tecnico externo",
                FechaEnvio = DateTime.Now.AddDays(-4),
                Detalle = "Revision por falla de encendido.",
                EstadoReparacion = Estado.Pendiente,
                UsuarioSolicita = "Paula Molina"
            });
        }

        if (!await context.Bajas.AnyAsync())
        {
            var tablet = await context.Tecnologias.FirstAsync(t => t.SkuCodigoInventario == "TAB-001");
            context.Bajas.Add(new Baja
            {
                IdTecnologia = tablet.IdTecnologia,
                IdMotivo = 2,
                FechaBaja = DateTime.Now.AddDays(-2),
                Detalle = "Pantalla quebrada, reparacion no conveniente.",
                UsuarioRegistraBaja = "Carlos Vega",
                Estado = Estado.Pendiente
            });
        }

        if (!await context.SolicitudesImpresion.AnyAsync())
        {
            var profesorRut = await context.Personal
                .Where(p => p.Correo == "profesor@colegio.cl")
                .Select(p => p.RutPersonal)
                .FirstAsync();
            var inspectorRut = await context.Personal
                .Where(p => p.Correo == "inspector@colegio.cl")
                .Select(p => p.RutPersonal)
                .FirstAsync();

            context.SolicitudesImpresion.AddRange(
                new SolicitudImpresion { RutPersonal = profesorRut, IdEstadoImpresion = 1, FechaSolicitud = DateTime.Now.AddDays(-1), Archivo = "guia-matematica.pdf", CantidadPaginas = 4, CantidadCopias = 30, Color = "Blanco y negro", DobleCara = true, Detalle = "Guias para 4 basico" },
                new SolicitudImpresion { RutPersonal = inspectorRut, IdEstadoImpresion = 3, FechaSolicitud = DateTime.Now.AddDays(-8), FechaEntrega = DateTime.Now.AddDays(-7), Archivo = "circular-apoderados.pdf", CantidadPaginas = 2, CantidadCopias = 120, Color = "Color", DobleCara = false, Detalle = "Circular reunion" });
        }

        await context.SaveChangesAsync();
    }

    private static async Task NormalizeEstadosImpresionAsync(ApplicationDbContext context)
    {
        var estados = await context.EstadosImpresion
            .AsNoTracking()
            .OrderBy(e => e.IdEstadoImpresion)
            .ToListAsync();

        var requeridos = new[] { Estado.Pendiente, Estado.EnProceso, Estado.Entregada, Estado.Rechazada };
        foreach (var requerido in requeridos)
        {
            if (!estados.Any(e => InputValidationHelper.NormalizeKey(e.Estado) == InputValidationHelper.NormalizeKey(requerido)))
            {
                context.EstadosImpresion.Add(new EstadoImpresion { Estado = requerido });
            }
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        estados = await context.EstadosImpresion
            .AsNoTracking()
            .OrderBy(e => e.IdEstadoImpresion)
            .ToListAsync();

        var enProceso = estados.FirstOrDefault(e => InputValidationHelper.NormalizeKey(e.Estado) == InputValidationHelper.NormalizeKey(Estado.EnProceso));
        var aprobada = estados.FirstOrDefault(e => InputValidationHelper.NormalizeKey(e.Estado) == InputValidationHelper.NormalizeKey(Estado.Aprobada));

        if (enProceso != null && aprobada != null && aprobada.IdEstadoImpresion != enProceso.IdEstadoImpresion)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Solicitud_impresion]
SET [id_estado_impresion] = {enProceso.IdEstadoImpresion}
WHERE [id_estado_impresion] = {aprobada.IdEstadoImpresion};

DELETE FROM [Estado_impresion]
WHERE [id_estado_impresion] = {aprobada.IdEstadoImpresion};
""");
        }

        estados = await context.EstadosImpresion
            .AsNoTracking()
            .OrderBy(e => e.IdEstadoImpresion)
            .ToListAsync();

        foreach (var grupo in estados.GroupBy(e => InputValidationHelper.NormalizeKey(e.Estado)))
        {
            var nombreCanonico = grupo.Key switch
            {
                "PENDIENTE" => Estado.Pendiente,
                "EN PROCESO" => Estado.EnProceso,
                "ENTREGADA" => Estado.Entregada,
                "RECHAZADA" => Estado.Rechazada,
                _ => null
            };

            if (nombreCanonico == null)
                continue;

            var principal = grupo.First();
            foreach (var duplicado in grupo.Where(e => e.IdEstadoImpresion != principal.IdEstadoImpresion))
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Solicitud_impresion]
SET [id_estado_impresion] = {principal.IdEstadoImpresion}
WHERE [id_estado_impresion] = {duplicado.IdEstadoImpresion};

DELETE FROM [Estado_impresion]
WHERE [id_estado_impresion] = {duplicado.IdEstadoImpresion};
""");
            }

            await context.Database.ExecuteSqlInterpolatedAsync($"""
UPDATE [Estado_impresion]
SET [estado_impresion] = {nombreCanonico}
WHERE [id_estado_impresion] = {principal.IdEstadoImpresion};
""");
        }

        context.ChangeTracker.Clear();
    }

    private static async Task SeedInsumosAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM Tipo_insumo WHERE nombre_tipoinsumo = N'Material de aseo')
    INSERT INTO Tipo_insumo (nombre_tipoinsumo) VALUES (N'Material de aseo');

IF NOT EXISTS (SELECT 1 FROM Tipo_insumo WHERE nombre_tipoinsumo = N'Libreria')
    INSERT INTO Tipo_insumo (nombre_tipoinsumo) VALUES (N'Libreria');
""");

        if (await context.Insumos.AnyAsync()) return;

        await context.Database.ExecuteSqlRawAsync("""
DECLARE @Aseo int = (SELECT TOP 1 id_tipoinsumo FROM Tipo_insumo WHERE nombre_tipoinsumo = N'Material de aseo');
DECLARE @Libreria int = (SELECT TOP 1 id_tipoinsumo FROM Tipo_insumo WHERE nombre_tipoinsumo = N'Libreria');

INSERT INTO Insumo (id_tipoinsumo, nombre_insumo, descripcion_insumo, unidad_medida, estado, toxicidad, stock_actual, stock_minimo)
VALUES
    (@Libreria, N'Resma carta', N'Papel tamano carta para guias y circulares', N'Resma', 1, N'No toxico', 34, 10),
    (@Libreria, N'Toner negro', N'Toner para impresora institucional', N'Unidad', 1, N'Baja', 6, 3),
    (@Libreria, N'Tinta color', N'Cartucho color para material pedagogico', N'Unidad', 1, N'Baja', 4, 2),
    (@Aseo, N'Cloro', N'Insumo de limpieza institucional', N'Litro', 1, N'Baja', 12, 4);
""");
    }

    private static async Task SeedBitacoraAsync(ApplicationDbContext context)
    {
        if (await context.Bitacoras.AnyAsync()) return;

        context.Bitacoras.AddRange(
            new Bitacora { Usuario = "Admin Sistema", Rol = RoleNames.Administrador, Modulo = "Sistema", Accion = "Carga inicial de datos", Fecha = DateTime.Now.AddDays(-10) },
            new Bitacora { Usuario = "Paula Molina", Rol = RoleNames.Secretaria, Modulo = "Tecnologia", Accion = "Registro de activos iniciales", Fecha = DateTime.Now.AddDays(-9) },
            new Bitacora { Usuario = "Laura Silva", Rol = RoleNames.EncargadoBiblioteca, Modulo = "Impresiones", Accion = "Entrega de circular", Fecha = DateTime.Now.AddDays(-7) });

        await context.SaveChangesAsync();
    }
}
