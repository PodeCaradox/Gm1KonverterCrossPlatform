using System;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// An expected problem in the user's workflow (e.g. images were not exported yet).
    /// The message is meant to be shown to the user.
    /// </summary>
    public sealed class WorkflowException : Exception
    {
        public WorkflowException(string message)
            : base(message)
        {
        }

        public WorkflowException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
