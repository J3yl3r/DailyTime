namespace dailyTimeApi.Exceptions
{
    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }
        public ValidationException(string message) : base(message)
        {
            Errors = new[] { message };
        }
        public ValidationException(IEnumerable<string> errors)
            : base(string.Join("; ", errors))
        {
            Errors = errors.ToList();
        }
    }
}
