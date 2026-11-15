/* =====================================================================
   Usuarios iniciales para poder probar el login (ejecutar DESPUÉS de corralon_db.sql)

   admin     / Admin123!      (rol Administrador)
   vendedor  / Vendedor123!   (rol Vendedor)

   Las contraseñas están guardadas como hash PBKDF2 (formato PBKDF2$iteraciones$salt$hash),
   el mismo que genera y verifica la clase PasswordHasher de Corralon.Logic.
   CAMBIARLAS antes de mostrar o publicar el sistema.
   ===================================================================== */

USE CorralonDB;
GO

UPDATE dbo.Usuarios
SET passwordHash = N'PBKDF2$100000$E+qKbMc5YODjVjVmobk4kA==$FmTd2SDKcSQJMimoFBrqFjJOTWhQ4O8/InBOL4E5V48='
WHERE nombreUsuario = N'admin';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE nombreUsuario = N'vendedor')
    INSERT INTO dbo.Usuarios (nombreUsuario, passwordHash, nombreCompleto, email, idRol)
    VALUES (N'vendedor', N'PBKDF2$100000$7CT+msl0qKChhhlfSBrJEA==$oMFyjsssGVmnMtajSxHNZfYJ6bcG+Sf7fwvT3i0+hxI=', N'Vendedor de prueba', N'vendedor@corralon.com',
            (SELECT idRol FROM dbo.Roles WHERE nombre = N'Vendedor'));
GO
