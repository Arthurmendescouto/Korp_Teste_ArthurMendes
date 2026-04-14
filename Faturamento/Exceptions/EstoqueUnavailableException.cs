namespace Faturamento.Exceptions
{
    /// <summary>
    /// Falha de comunicação com o microsserviço de Estoque (rede, timeout ou host inacessível).
    /// </summary>
    public sealed class EstoqueUnavailableException : Exception
    {
        public EstoqueUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
