namespace Corralon.Logic.Exceptions
{
    /// <summary>El registro pedido no existe (la API responde 404).</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Se violó una regla de negocio (la API responde 400).</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string mensaje) : base(mensaje) { }
    }
}
