namespace CK.Object.Filter
{
    /// <summary>
    /// Generalizes <see cref="ObjectFilterHook"/> and <see cref="ObjectAsyncFilterHook"/>.
    /// </summary>
    public interface IObjectFilterHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectFilterConfiguration Configuration { get; }
    }

}
