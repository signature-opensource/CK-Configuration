using CK.Core;

namespace StrategyPlugin
{
    /// <summary>
    /// An example of the base abstraction of a configured component instance.
    /// Note that this can perfectly be an abstract class.
    /// </summary>
    public interface IStrategy
    {
        /// <summary>
        /// Stupid method, just for test. Any number of methods, properties or events can
        /// be defined in the base abstraction.
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        int DoSomething( IActivityMonitor monitor, int payload );
    }
}
