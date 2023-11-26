namespace CK.Object.Transform
{
    /// <summary>
    /// Generalizes <see cref="ObjectTransformHook"/> and <see cref="ObjectAsyncTransformHook"/>.
    /// </summary>
    public interface IObjectTransformHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectTransformConfiguration Configuration { get; }
    }

}
