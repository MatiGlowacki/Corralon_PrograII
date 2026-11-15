/* =====================================================================
   TPI Programación II - TUP UTN FRC
   Sistema de facturación para un CORRALÓN DE MATERIALES
   Motor: SQL Server (T-SQL)
   ===================================================================== */

IF DB_ID(N'CorralonDB') IS NULL
    CREATE DATABASE CorralonDB;
GO

USE CorralonDB;
GO

/* =====================================================================
   1. SEGURIDAD (login + JWT)
   ===================================================================== */

CREATE TABLE dbo.Roles (
    idRol        INT IDENTITY(1,1) NOT NULL,
    nombre       NVARCHAR(50)      NOT NULL,
    descripcion  NVARCHAR(200)     NULL,
    CONSTRAINT PK_Roles        PRIMARY KEY (idRol),
    CONSTRAINT UQ_Roles_nombre UNIQUE (nombre)
);
GO

CREATE TABLE dbo.Usuarios (
    idUsuario       INT IDENTITY(1,1) NOT NULL,
    nombreUsuario   NVARCHAR(50)      NOT NULL,
    passwordHash    NVARCHAR(255)     NOT NULL,   -- NUNCA texto plano (hash generado desde C#)
    nombreCompleto  NVARCHAR(150)     NOT NULL,
    email           NVARCHAR(150)     NULL,
    idRol           INT               NOT NULL,
    activo          BIT               NOT NULL CONSTRAINT DF_Usuarios_activo DEFAULT (1),
    fechaAlta       DATETIME2(0)      NOT NULL CONSTRAINT DF_Usuarios_fechaAlta DEFAULT (SYSDATETIME()),
    ultimoAcceso    DATETIME2(0)      NULL,
    CONSTRAINT PK_Usuarios               PRIMARY KEY (idUsuario),
    CONSTRAINT UQ_Usuarios_nombreUsuario UNIQUE (nombreUsuario),
    CONSTRAINT FK_Usuarios_Roles           FOREIGN KEY (idRol) REFERENCES dbo.Roles (idRol)
);
GO

/* =====================================================================
   2. TABLAS DE SOPORTE (ABMC del menú "Soporte")
   ===================================================================== */

CREATE TABLE dbo.FormasPago (
    idFormaPago  INT IDENTITY(1,1) NOT NULL,
    nombre       NVARCHAR(50)      NOT NULL,
    descripcion  NVARCHAR(200)     NULL,
    activo       BIT               NOT NULL CONSTRAINT DF_FormasPago_activo DEFAULT (1),
    CONSTRAINT PK_FormasPago        PRIMARY KEY (idFormaPago),
    CONSTRAINT UQ_FormasPago_nombre UNIQUE (nombre)
);
GO

CREATE TABLE dbo.CondicionesIva (
    idCondicionIva INT IDENTITY(1,1) NOT NULL,
    nombre         NVARCHAR(60)      NOT NULL,
    CONSTRAINT PK_CondicionesIva        PRIMARY KEY (idCondicionIva),
    CONSTRAINT UQ_CondicionesIva_nombre UNIQUE (nombre)
);
GO

CREATE TABLE dbo.Provincias (
    idProvincia INT IDENTITY(1,1) NOT NULL,
    nombre      NVARCHAR(60)      NOT NULL,
    CONSTRAINT PK_Provincias        PRIMARY KEY (idProvincia),
    CONSTRAINT UQ_Provincias_nombre UNIQUE (nombre)
);
GO

CREATE TABLE dbo.Localidades (
    idLocalidad  INT IDENTITY(1,1) NOT NULL,
    nombre       NVARCHAR(100)     NOT NULL,
    codigoPostal NVARCHAR(10)      NULL,
    idProvincia  INT               NOT NULL,
    CONSTRAINT PK_Localidades           PRIMARY KEY (idLocalidad),
    CONSTRAINT FK_Localidades_Provincias FOREIGN KEY (idProvincia) REFERENCES dbo.Provincias (idProvincia),
    CONSTRAINT UQ_Localidades_nombre_provincia UNIQUE (nombre, idProvincia)
);
GO

CREATE TABLE dbo.Categorias (
    idCategoria INT IDENTITY(1,1) NOT NULL,
    nombre      NVARCHAR(80)      NOT NULL,
    descripcion NVARCHAR(200)     NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Categorias_activo DEFAULT (1),
    CONSTRAINT PK_Categorias        PRIMARY KEY (idCategoria),
    CONSTRAINT UQ_Categorias_nombre UNIQUE (nombre)
);
GO

/* Unidades de venta: en un corralón se vende por bolsa, m3, kg, metro, unidad... */
CREATE TABLE dbo.UnidadesMedida (
    idUnidadMedida   INT IDENTITY(1,1) NOT NULL,
    nombre           NVARCHAR(40)      NOT NULL,
    abreviatura      NVARCHAR(10)      NOT NULL,
    permiteDecimales BIT               NOT NULL CONSTRAINT DF_UnidadesMedida_decimales DEFAULT (0),
    CONSTRAINT PK_UnidadesMedida             PRIMARY KEY (idUnidadMedida),
    CONSTRAINT UQ_UnidadesMedida_nombre      UNIQUE (nombre),
    CONSTRAINT UQ_UnidadesMedida_abreviatura UNIQUE (abreviatura)
);
GO

CREATE TABLE dbo.Proveedores (
    idProveedor INT IDENTITY(1,1) NOT NULL,
    razonSocial NVARCHAR(150)     NOT NULL,
    cuit        NVARCHAR(13)      NOT NULL,
    telefono    NVARCHAR(30)      NULL,
    email       NVARCHAR(150)     NULL,
    domicilio   NVARCHAR(200)     NULL,
    idLocalidad INT               NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Proveedores_activo DEFAULT (1),
    CONSTRAINT PK_Proveedores           PRIMARY KEY (idProveedor),
    CONSTRAINT UQ_Proveedores_cuit      UNIQUE (cuit),
    CONSTRAINT FK_Proveedores_Localidades FOREIGN KEY (idLocalidad) REFERENCES dbo.Localidades (idLocalidad)
);
GO

/* =====================================================================
   3. DATOS MAESTROS
   ===================================================================== */

CREATE TABLE dbo.Clientes (
    idCliente       INT IDENTITY(1,1) NOT NULL,
    nombreCompleto  NVARCHAR(150)     NOT NULL,   -- "Apellido y nombre" o razón social
    tipoDocumento   VARCHAR(4)        NOT NULL,
    nroDocumento    VARCHAR(13)       NOT NULL,
    idCondicionIva  INT               NOT NULL,
    telefono        NVARCHAR(30)      NULL,
    email           NVARCHAR(150)     NULL,
    domicilio       NVARCHAR(200)     NULL,
    idLocalidad     INT               NULL,
    activo          BIT               NOT NULL CONSTRAINT DF_Clientes_activo DEFAULT (1),
    fechaAlta       DATETIME2(0)      NOT NULL CONSTRAINT DF_Clientes_fechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Clientes               PRIMARY KEY (idCliente),
    CONSTRAINT UQ_Clientes_documento     UNIQUE (tipoDocumento, nroDocumento),
    CONSTRAINT CK_Clientes_tipoDocumento CHECK (tipoDocumento IN ('DNI', 'CUIT', 'CUIL')),
    CONSTRAINT FK_Clientes_CondicionesIva  FOREIGN KEY (idCondicionIva) REFERENCES dbo.CondicionesIva (idCondicionIva),
    CONSTRAINT FK_Clientes_Localidades     FOREIGN KEY (idLocalidad)    REFERENCES dbo.Localidades (idLocalidad)
);
GO

CREATE TABLE dbo.Articulos (
    idArticulo     INT IDENTITY(1,1) NOT NULL,
    codigo         NVARCHAR(30)      NOT NULL,
    nombre         NVARCHAR(150)     NOT NULL,
    descripcion    NVARCHAR(300)     NULL,
    idCategoria    INT               NOT NULL,
    idUnidadMedida INT               NOT NULL,
    idProveedor    INT               NULL,
    precioUnitario DECIMAL(12,2)     NOT NULL,
    stockActual    DECIMAL(12,3)     NOT NULL CONSTRAINT DF_Articulos_stockActual DEFAULT (0),
    stockMinimo    DECIMAL(12,3)     NOT NULL CONSTRAINT DF_Articulos_stockMinimo DEFAULT (0),
    activo         BIT               NOT NULL CONSTRAINT DF_Articulos_activo DEFAULT (1),
    fechaAlta      DATETIME2(0)      NOT NULL CONSTRAINT DF_Articulos_fechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Articulos              PRIMARY KEY (idArticulo),
    CONSTRAINT UQ_Articulos_codigo       UNIQUE (codigo),
    CONSTRAINT CK_Articulos_precio       CHECK (precioUnitario >= 0),
    CONSTRAINT CK_Articulos_stockActual  CHECK (stockActual >= 0),      -- red de seguridad: jamás stock negativo
    CONSTRAINT CK_Articulos_stockMinimo  CHECK (stockMinimo >= 0),
    CONSTRAINT FK_Articulos_Categorias    FOREIGN KEY (idCategoria)    REFERENCES dbo.Categorias (idCategoria),
    CONSTRAINT FK_Articulos_UnidadesMedida FOREIGN KEY (idUnidadMedida) REFERENCES dbo.UnidadesMedida (idUnidadMedida),
    CONSTRAINT FK_Articulos_Proveedores    FOREIGN KEY (idProveedor)    REFERENCES dbo.Proveedores (idProveedor)
);
GO

/* =====================================================================
   4. TRANSACCIÓN: FACTURA + DETALLE
   ===================================================================== */

/* Numeración correlativa generada por la base (evita el típico MAX()+1 con concurrencia) */
CREATE SEQUENCE dbo.seq_NroFactura AS INT START WITH 1 INCREMENT BY 1;
GO

CREATE TABLE dbo.Facturas (
    idFactura         INT IDENTITY(1,1) NOT NULL,
    nroFactura        INT               NOT NULL CONSTRAINT DF_Facturas_nroFactura DEFAULT (NEXT VALUE FOR dbo.seq_NroFactura),
    fecha             DATETIME2(0)      NOT NULL CONSTRAINT DF_Facturas_fecha DEFAULT (SYSDATETIME()),
    idCliente         INT               NOT NULL,
    idFormaPago       INT               NOT NULL,
    idUsuario         INT               NOT NULL,   -- vendedor que emitió la factura
    observaciones    NVARCHAR(300)     NULL,
    estado            VARCHAR(10)       NOT NULL CONSTRAINT DF_Facturas_estado DEFAULT ('Emitida'),
    fechaAnulacion    DATETIME2(0)      NULL,
    motivoAnulacion   NVARCHAR(200)     NULL,
    CONSTRAINT PK_Facturas            PRIMARY KEY (idFactura),
    CONSTRAINT UQ_Facturas_nro        UNIQUE (nroFactura),
    CONSTRAINT CK_Facturas_estado     CHECK (estado IN ('Emitida', 'Anulada')),
    CONSTRAINT CK_Facturas_anulacion  CHECK (
        (estado = 'Emitida' AND fechaAnulacion IS NULL AND motivoAnulacion IS NULL) OR
        (estado = 'Anulada' AND fechaAnulacion IS NOT NULL AND motivoAnulacion IS NOT NULL)),
    CONSTRAINT FK_Facturas_Clientes    FOREIGN KEY (idCliente)   REFERENCES dbo.Clientes (idCliente),
    CONSTRAINT FK_Facturas_FormasPago  FOREIGN KEY (idFormaPago) REFERENCES dbo.FormasPago (idFormaPago),
    CONSTRAINT FK_Facturas_Usuarios    FOREIGN KEY (idUsuario)   REFERENCES dbo.Usuarios (idUsuario)
);
GO

CREATE TABLE dbo.DetallesFactura (
    idDetalle      INT IDENTITY(1,1) NOT NULL,
    idFactura      INT               NOT NULL,
    idArticulo     INT               NOT NULL,
    cantidad       DECIMAL(12,3)     NOT NULL,
    precioUnitario DECIMAL(12,2)     NOT NULL,   -- precio al momento de la venta (no cambia si el artículo se reprecia)
    CONSTRAINT PK_DetallesFactura          PRIMARY KEY (idDetalle),
    -- Regla del enunciado: un artículo aparece una sola vez por factura (se incrementa la cantidad)
    CONSTRAINT UQ_DetallesFactura_articulo UNIQUE (idFactura, idArticulo),
    CONSTRAINT CK_DetallesFactura_cantidad CHECK (cantidad > 0),
    CONSTRAINT CK_DetallesFactura_precio   CHECK (precioUnitario >= 0),
    CONSTRAINT FK_DetallesFactura_Facturas  FOREIGN KEY (idFactura)  REFERENCES dbo.Facturas (idFactura) ON DELETE CASCADE,
    CONSTRAINT FK_DetallesFactura_Articulos FOREIGN KEY (idArticulo) REFERENCES dbo.Articulos (idArticulo)
);
GO

/* =====================================================================
   5. STOCK: HISTORIAL DE MOVIMIENTOS (trazabilidad)
   Articulos.stockActual es el saldo; cada cambio queda registrado acá.
   ===================================================================== */

CREATE TABLE dbo.MovimientosStock (
    idMovimiento INT IDENTITY(1,1) NOT NULL,
    idArticulo   INT               NOT NULL,
    fecha        DATETIME2(0)      NOT NULL CONSTRAINT DF_MovStock_fecha DEFAULT (SYSDATETIME()),
    tipo         VARCHAR(7)        NOT NULL,   -- Entrada suma / Salida resta
    motivo       VARCHAR(12)       NOT NULL,
    cantidad     DECIMAL(12,3)     NOT NULL,   -- siempre positiva; el signo lo da "tipo"
    idFactura    INT               NULL,       -- si el movimiento vino de una venta o anulación
    idUsuario    INT               NOT NULL,
    observaciones NVARCHAR(200)    NULL,
    CONSTRAINT PK_MovimientosStock         PRIMARY KEY (idMovimiento),
    CONSTRAINT CK_MovStock_tipo           CHECK (tipo IN ('Entrada', 'Salida')),
    CONSTRAINT CK_MovStock_motivo         CHECK (motivo IN ('Compra', 'Venta', 'Anulacion', 'Ajuste', 'Devolucion')),
    CONSTRAINT CK_MovStock_cantidad       CHECK (cantidad > 0),
    CONSTRAINT FK_MovStock_Articulos       FOREIGN KEY (idArticulo) REFERENCES dbo.Articulos (idArticulo),
    CONSTRAINT FK_MovStock_Facturas        FOREIGN KEY (idFactura)  REFERENCES dbo.Facturas (idFactura),
    CONSTRAINT FK_MovStock_Usuarios        FOREIGN KEY (idUsuario)  REFERENCES dbo.Usuarios (idUsuario)
);
GO

/* =====================================================================
   6. REPARTOS
   Repartos = un viaje (camión + chofer + fecha) que lleva varias entregas.
   DetallesReparto = cada factura que se entrega dentro de ese viaje.
   ===================================================================== */

CREATE TABLE dbo.Vehiculos (
    idVehiculo  INT IDENTITY(1,1) NOT NULL,
    patente     NVARCHAR(10)      NOT NULL,
    marca       NVARCHAR(50)      NOT NULL,
    modelo      NVARCHAR(50)      NULL,
    capacidadKg DECIMAL(10,2)     NOT NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Vehiculos_activo DEFAULT (1),
    CONSTRAINT PK_Vehiculos           PRIMARY KEY (idVehiculo),
    CONSTRAINT UQ_Vehiculos_patente   UNIQUE (patente),
    CONSTRAINT CK_Vehiculos_capacidad CHECK (capacidadKg > 0)
);
GO

CREATE TABLE dbo.Choferes (
    idChofer           INT IDENTITY(1,1) NOT NULL,
    nombreCompleto     NVARCHAR(150)     NOT NULL,
    dni                VARCHAR(10)       NOT NULL,
    telefono           NVARCHAR(30)      NULL,
    activo             BIT               NOT NULL CONSTRAINT DF_Choferes_activo DEFAULT (1),
    CONSTRAINT PK_Choferes     PRIMARY KEY (idChofer),
    CONSTRAINT UQ_Choferes_dni UNIQUE (dni)
);
GO

CREATE TABLE dbo.Repartos (
    idReparto        INT IDENTITY(1,1) NOT NULL,
    fechaProgramada  DATE              NOT NULL,
    idVehiculo       INT               NOT NULL,
    idChofer         INT               NOT NULL,
    idUsuario        INT               NOT NULL,   -- quien programó el reparto
    estado           VARCHAR(12)       NOT NULL CONSTRAINT DF_Repartos_estado DEFAULT ('Programado'),
    horaSalida       DATETIME2(0)      NULL,
    horaRegreso      DATETIME2(0)      NULL,
    observaciones    NVARCHAR(300)     NULL,
    CONSTRAINT PK_Repartos          PRIMARY KEY (idReparto),
    CONSTRAINT CK_Repartos_estado   CHECK (estado IN ('Programado', 'EnCurso', 'Finalizado', 'Cancelado')),
    CONSTRAINT CK_Repartos_horas    CHECK (horaRegreso IS NULL OR (horaSalida IS NOT NULL AND horaRegreso >= horaSalida)),
    CONSTRAINT FK_Repartos_Vehiculos FOREIGN KEY (idVehiculo) REFERENCES dbo.Vehiculos (idVehiculo),
    CONSTRAINT FK_Repartos_Choferes   FOREIGN KEY (idChofer)   REFERENCES dbo.Choferes (idChofer),
    CONSTRAINT FK_Repartos_Usuarios  FOREIGN KEY (idUsuario)  REFERENCES dbo.Usuarios (idUsuario)
);
GO

CREATE TABLE dbo.DetallesReparto (
    idDetalleReparto  INT IDENTITY(1,1) NOT NULL,
    idReparto         INT               NOT NULL,
    idFactura         INT               NOT NULL,
    ordenEntrega      SMALLINT          NOT NULL,   -- orden de la parada dentro del recorrido
    domicilioEntrega  NVARCHAR(200)     NOT NULL,
    idLocalidad       INT               NOT NULL,
    telefonoContacto  NVARCHAR(30)      NULL,
    estado            VARCHAR(12)       NOT NULL CONSTRAINT DF_DetallesReparto_estado DEFAULT ('Pendiente'),
    fechaEntrega      DATETIME2(0)      NULL,
    observaciones     NVARCHAR(300)     NULL,       -- ej: motivo si no se pudo entregar
    CONSTRAINT PK_DetallesReparto         PRIMARY KEY (idDetalleReparto),
    CONSTRAINT UQ_DetallesReparto_factura UNIQUE (idReparto, idFactura),
    CONSTRAINT UQ_DetallesReparto_orden   UNIQUE (idReparto, ordenEntrega),
    CONSTRAINT CK_DetallesReparto_orden   CHECK (ordenEntrega > 0),
    CONSTRAINT CK_DetallesReparto_estado  CHECK (estado IN ('Pendiente', 'Entregada', 'NoEntregada')),
    CONSTRAINT CK_DetallesReparto_fecha   CHECK ((estado = 'Entregada' AND fechaEntrega IS NOT NULL)
                                              OR (estado <> 'Entregada' AND fechaEntrega IS NULL)),
    CONSTRAINT FK_DetallesReparto_Repartos   FOREIGN KEY (idReparto)   REFERENCES dbo.Repartos (idReparto) ON DELETE CASCADE,
    CONSTRAINT FK_DetallesReparto_Facturas   FOREIGN KEY (idFactura)   REFERENCES dbo.Facturas (idFactura),
    CONSTRAINT FK_DetallesReparto_Localidades FOREIGN KEY (idLocalidad) REFERENCES dbo.Localidades (idLocalidad)
);
GO

/* =====================================================================
   7. ÍNDICES sobre claves foráneas y columnas de búsqueda frecuente
   ===================================================================== */

CREATE INDEX IX_Usuarios_idRol            ON dbo.Usuarios (idRol);
CREATE INDEX IX_Localidades_idProvincia    ON dbo.Localidades (idProvincia);
CREATE INDEX IX_Clientes_nombreCompleto   ON dbo.Clientes (nombreCompleto);
CREATE INDEX IX_Clientes_idCondicionIva   ON dbo.Clientes (idCondicionIva);
CREATE INDEX IX_Clientes_idLocalidad      ON dbo.Clientes (idLocalidad);
CREATE INDEX IX_Articulos_nombre          ON dbo.Articulos (nombre);
CREATE INDEX IX_Articulos_idCategoria     ON dbo.Articulos (idCategoria);
CREATE INDEX IX_Articulos_idUnidadMedida  ON dbo.Articulos (idUnidadMedida);
CREATE INDEX IX_Articulos_idProveedor     ON dbo.Articulos (idProveedor);
CREATE INDEX IX_Facturas_fecha            ON dbo.Facturas (fecha);
CREATE INDEX IX_Facturas_idCliente        ON dbo.Facturas (idCliente);
CREATE INDEX IX_Facturas_idFormaPago      ON dbo.Facturas (idFormaPago);
CREATE INDEX IX_Facturas_idUsuario        ON dbo.Facturas (idUsuario);
CREATE INDEX IX_DetallesFactura_idArticulo ON dbo.DetallesFactura (idArticulo);
CREATE INDEX IX_MovStock_idArticulo      ON dbo.MovimientosStock (idArticulo, fecha);
CREATE INDEX IX_MovStock_idFactura       ON dbo.MovimientosStock (idFactura);
CREATE INDEX IX_Repartos_fecha            ON dbo.Repartos (fechaProgramada);
CREATE INDEX IX_Repartos_idVehiculo       ON dbo.Repartos (idVehiculo);
CREATE INDEX IX_Repartos_idChofer         ON dbo.Repartos (idChofer);
CREATE INDEX IX_Repartos_idUsuario        ON dbo.Repartos (idUsuario);
CREATE INDEX IX_DetallesReparto_idFactura ON dbo.DetallesReparto (idFactura);
CREATE INDEX IX_DetallesReparto_idLocalidad ON dbo.DetallesReparto (idLocalidad);
GO

/* =====================================================================
   8. DATOS INICIALES
   ===================================================================== */

INSERT INTO dbo.Roles (nombre, descripcion) VALUES
    (N'Administrador', N'Acceso total, incluye tablas de soporte'),
    (N'Vendedor',      N'Registra facturas y consulta stock');

/* IMPORTANTE: el hash de abajo es un MARCADOR. Reemplazalo por un hash real generado
   desde C# (por ejemplo con BCrypt o PBKDF2) antes de probar el login. */
INSERT INTO dbo.Usuarios (nombreUsuario, passwordHash, nombreCompleto, email, idRol) VALUES
    (N'admin', N'REEMPLAZAR_POR_HASH_REAL', N'Administrador del sistema', N'admin@corralon.com',
        (SELECT idRol FROM dbo.Roles WHERE nombre = N'Administrador'));

INSERT INTO dbo.FormasPago (nombre, descripcion) VALUES
    (N'Efectivo',          N'Pago en el mostrador'),
    (N'Transferencia',     N'Transferencia bancaria'),
    (N'Tarjeta de débito', NULL),
    (N'Tarjeta de crédito',NULL),
    (N'Cheque',            N'Cheque propio o de terceros'),
    (N'Cuenta corriente',  N'Pago a plazo para clientes habituales');

INSERT INTO dbo.CondicionesIva (nombre) VALUES
    (N'Consumidor Final'),
    (N'Responsable Inscripto'),
    (N'Monotributista'),
    (N'Exento');

INSERT INTO dbo.Provincias (nombre) VALUES (N'Córdoba');

INSERT INTO dbo.Localidades (nombre, codigoPostal, idProvincia)
SELECT v.nombre, v.cp, p.idProvincia
FROM (VALUES (N'Córdoba', N'5000'),
             (N'Villa María', N'5900'),
             (N'Río Cuarto', N'5800'),
             (N'San Francisco', N'2400'),
             (N'Villa Carlos Paz', N'5152')) AS v(nombre, cp)
CROSS JOIN dbo.Provincias p
WHERE p.nombre = N'Córdoba';

INSERT INTO dbo.UnidadesMedida (nombre, abreviatura, permiteDecimales) VALUES
    (N'Unidad',      N'u',   0),
    (N'Bolsa',       N'bol', 0),
    (N'Kilogramo',   N'kg',  1),
    (N'Metro',       N'm',   1),
    (N'Metro cúbico',N'm3',  1),
    (N'Litro',       N'l',   1),
    (N'Lata',        N'lta', 0);

INSERT INTO dbo.Categorias (nombre, descripcion) VALUES
    (N'Cementos y cales',      N'Cemento, cal, adhesivos cementicios'),
    (N'Áridos',                N'Arena, piedra, ripio'),
    (N'Ladrillos y bloques',   N'Ladrillos huecos, comunes y bloques'),
    (N'Hierros y mallas',      N'Hierro de construcción, mallas, alambres'),
    (N'Pinturas',              N'Látex, esmaltes, impermeabilizantes'),
    (N'Sanitarios y plomería', N'Caños, accesorios, tanques'),
    (N'Electricidad',          N'Cables, térmicas, tomas'),
    (N'Techos y aislantes',    N'Chapas, membranas, aislantes');

INSERT INTO dbo.Proveedores (razonSocial, cuit, telefono, email) VALUES
    (N'Cementos del Centro S.A.',      N'30-71234567-8', N'351-4000001', N'ventas@cementoscentro.com'),
    (N'Aceros Mediterráneo S.R.L.',    N'30-70987654-3', N'351-4000002', N'pedidos@acerosmed.com'),
    (N'Cerámica Cordobesa S.A.',       N'30-70555444-1', N'351-4000003', N'comercial@ceramicacba.com');

INSERT INTO dbo.Articulos (codigo, nombre, idCategoria, idUnidadMedida, idProveedor, precioUnitario, stockActual, stockMinimo)
SELECT v.codigo, v.nombre, c.idCategoria, u.idUnidadMedida, p.idProveedor, v.precio, v.stock, v.minimo
FROM (VALUES
    (N'CEM-001', N'Cemento Portland 50 kg',        N'Cementos y cales',    N'Bolsa',        N'Cementos del Centro S.A.',   9800.00, 200.000, 40.000),
    (N'CAL-001', N'Cal hidráulica 25 kg',          N'Cementos y cales',    N'Bolsa',        N'Cementos del Centro S.A.',   5200.00, 120.000, 30.000),
    (N'ARI-001', N'Arena fina',                    N'Áridos',              N'Metro cúbico', NULL,                         28000.00,  40.000, 10.000),
    (N'ARI-002', N'Piedra partida 6-20',           N'Áridos',              N'Metro cúbico', NULL,                         35000.00,  30.000, 10.000),
    (N'LAD-001', N'Ladrillo hueco 12x18x33',       N'Ladrillos y bloques', N'Unidad',       N'Cerámica Cordobesa S.A.',      620.00, 5000.000, 1000.000),
    (N'HIE-001', N'Hierro construcción 8 mm x 12 m',N'Hierros y mallas',   N'Unidad',       N'Aceros Mediterráneo S.R.L.',  9500.00, 150.000, 40.000),
    (N'HIE-002', N'Malla sima 15x15 4,2 mm',       N'Hierros y mallas',    N'Unidad',       N'Aceros Mediterráneo S.R.L.', 48000.00,  25.000, 8.000),
    (N'PIN-001', N'Látex interior blanco 20 l',    N'Pinturas',            N'Lata',         NULL,                         62000.00,  18.000, 5.000),
    (N'ELE-001', N'Cable unipolar 2,5 mm',         N'Electricidad',        N'Metro',        NULL,                           950.00, 600.000, 100.000),
    (N'SAN-001', N'Caño PVC 110 mm x 4 m',         N'Sanitarios y plomería',N'Unidad',      NULL,                         18500.00,  35.000, 10.000)
) AS v(codigo, nombre, categoria, unidad, proveedor, precio, stock, minimo)
JOIN dbo.Categorias c     ON c.nombre = v.categoria
JOIN dbo.UnidadesMedida u  ON u.nombre = v.unidad
LEFT JOIN dbo.Proveedores p ON p.razonSocial = v.proveedor;

INSERT INTO dbo.Clientes (nombreCompleto, tipoDocumento, nroDocumento, idCondicionIva, telefono, idLocalidad)
SELECT v.nombre, v.tipoDoc, v.nroDoc, ci.idCondicionIva, v.tel, l.idLocalidad
FROM (VALUES
    (N'Consumidor Final',                   'DNI',  '0',            N'Consumidor Final',     NULL,           NULL),
    (N'Constructora Del Sur S.R.L.',        'CUIT', '30-71111222-5',N'Responsable Inscripto',N'353-4111111', N'Villa María'),
    (N'Pérez, Juan Carlos',                 'DNI',  '25123456',     N'Monotributista',       N'351-5222222', N'Córdoba')
) AS v(nombre, tipoDoc, nroDoc, condicion, tel, localidad)
JOIN dbo.CondicionesIva ci ON ci.nombre = v.condicion
LEFT JOIN dbo.Localidades l ON l.nombre = v.localidad;

INSERT INTO dbo.Vehiculos (patente, marca, modelo, capacidadKg) VALUES
    (N'AB123CD', N'Mercedes-Benz', N'Camión 1620',   12000.00),
    (N'AC456EF', N'Ford',          N'Cargo 1722',     8000.00),
    (N'AA789GH', N'Volkswagen',    N'Delivery 9.170', 3500.00);

INSERT INTO dbo.Choferes (nombreCompleto, dni, telefono) VALUES
    (N'Gómez, Ramón',    '22345678', N'353-4333333'),
    (N'Ledesma, Carlos', '27456789', N'353-4444444');
GO
