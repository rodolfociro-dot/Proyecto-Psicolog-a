-- =====================================================
-- Plataforma de gestión para psicología (varios profesionales)
-- Valores monetarios en pesos colombianos (COP), sin decimales.
-- =====================================================
CREATE DATABASE PsicologiaDB;
GO
USE PsicologiaDB;
GO

-- Roles: Administrador, Profesional, Asistente
CREATE TABLE Rol (
    IdRol INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(30) NOT NULL UNIQUE
);

-- Todas las personas que entran al sistema (la contraseña se guarda cifrada)
CREATE TABLE Usuario (
    IdUsuario INT IDENTITY PRIMARY KEY,
    IdRol INT NOT NULL REFERENCES Rol(IdRol),
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    ContrasenaHash NVARCHAR(255) NOT NULL,
    Telefono NVARCHAR(20) NULL,
    Especialidad NVARCHAR(100) NULL,          -- solo profesionales
    RegistroProfesional NVARCHAR(50) NULL,    -- solo profesionales
    ValorSesion DECIMAL(14,0) NULL CHECK (ValorSesion >= 0),  -- precio sugerido por sesión
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- Las personas atendidas (cada una pertenece a un profesional)
CREATE TABLE Consultante (
    IdConsultante INT IDENTITY PRIMARY KEY,
    TipoDocumento NVARCHAR(10) NOT NULL,
    Documento NVARCHAR(20) NOT NULL,
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    FechaNacimiento DATE NULL,
    Telefono NVARCHAR(20) NULL,
    Email NVARCHAR(150) NULL,
    ContactoEmergenciaNombre NVARCHAR(100) NULL,
    ContactoEmergenciaTelefono NVARCHAR(20) NULL,
    MotivoConsulta NVARCHAR(500) NULL,
    IdProfesional INT NOT NULL REFERENCES Usuario(IdUsuario),
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Consultante_Documento UNIQUE (TipoDocumento, Documento)
);

-- Agenda
CREATE TABLE Cita (
    IdCita INT IDENTITY PRIMARY KEY,
    IdConsultante INT NOT NULL REFERENCES Consultante(IdConsultante),
    IdProfesional INT NOT NULL REFERENCES Usuario(IdUsuario),
    FechaInicio DATETIME2 NOT NULL,
    FechaFin DATETIME2 NOT NULL,
    Modalidad NVARCHAR(15) NOT NULL DEFAULT N'Presencial'
        CHECK (Modalidad IN (N'Presencial', N'Virtual')),
    Estado NVARCHAR(15) NOT NULL DEFAULT N'Programada'
        CHECK (Estado IN (N'Programada', N'Atendida', N'Cancelada', N'Inasistencia')),
    Valor DECIMAL(14,0) NOT NULL DEFAULT 0 CHECK (Valor >= 0),
    EnlaceVirtual NVARCHAR(300) NULL,
    NotaAdministrativa NVARCHAR(300) NULL,    -- nota NO clínica (ej. "llega tarde")
    IdCreadaPor INT NOT NULL REFERENCES Usuario(IdUsuario),
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Cita_Fechas CHECK (FechaFin > FechaInicio)
);
CREATE INDEX IX_Cita_Profesional_Fecha ON Cita (IdProfesional, FechaInicio);

-- Contenido clínico: tabla aparte para poder restringir quién la ve
CREATE TABLE NotaSesion (
    IdNota INT IDENTITY PRIMARY KEY,
    IdCita INT NOT NULL UNIQUE REFERENCES Cita(IdCita),
    IdConsultante INT NOT NULL REFERENCES Consultante(IdConsultante),
    IdProfesional INT NOT NULL REFERENCES Usuario(IdUsuario),
    Motivo NVARCHAR(500) NULL,
    Desarrollo NVARCHAR(MAX) NULL,
    PlanSeguimiento NVARCHAR(MAX) NULL,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaModificacion DATETIME2 NULL
);

-- Finanzas
CREATE TABLE Pago (
    IdPago INT IDENTITY PRIMARY KEY,
    IdConsultante INT NOT NULL REFERENCES Consultante(IdConsultante),
    IdCita INT NULL REFERENCES Cita(IdCita),
    Monto DECIMAL(14,0) NOT NULL CHECK (Monto > 0),
    MetodoPago NVARCHAR(20) NOT NULL
        CHECK (MetodoPago IN (N'Efectivo', N'Transferencia', N'Tarjeta', N'Otro')),
    FechaPago DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Referencia NVARCHAR(100) NULL,
    IdRegistradoPor INT NOT NULL REFERENCES Usuario(IdUsuario)
);

-- Auditoría: quién vio o modificó información sensible
CREATE TABLE RegistroAcceso (
    IdRegistro BIGINT IDENTITY PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuario(IdUsuario),
    Accion NVARCHAR(30) NOT NULL,        -- Ver, Crear, Modificar, Exportar
    Entidad NVARCHAR(30) NOT NULL,       -- NotaSesion, Consultante, ...
    IdEntidad INT NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

INSERT INTO Rol (Nombre) VALUES (N'Administrador'), (N'Profesional'), (N'Asistente');
GO