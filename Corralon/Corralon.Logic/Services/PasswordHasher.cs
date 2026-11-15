using System.Security.Cryptography;

namespace Corralon.Logic.Services
{
    /// <summary>
    /// Guarda las contraseñas con PBKDF2 (nunca en texto plano).
    /// Formato almacenado: PBKDF2$iteraciones$salt(base64)$hash(base64)
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iteraciones = 100000;
        private const int TamanoSalt = 16;
        private const int TamanoHash = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(TamanoSalt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
            return $"PBKDF2${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verificar(string password, string almacenado)
        {
            if (string.IsNullOrEmpty(almacenado))
            {
                return false;
            }

            var partes = almacenado.Split('$');
            if (partes.Length != 4 || partes[0] != "PBKDF2" || !int.TryParse(partes[1], out var iteraciones))
            {
                return false;   // por ejemplo, el marcador REEMPLAZAR_POR_HASH_REAL del script
            }

            try
            {
                var salt = Convert.FromBase64String(partes[2]);
                var esperado = Convert.FromBase64String(partes[3]);
                var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
                return CryptographicOperations.FixedTimeEquals(calculado, esperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
