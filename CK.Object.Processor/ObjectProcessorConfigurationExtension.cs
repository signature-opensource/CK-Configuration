using CK.Core;
using CK.Object.Processor;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public static class ObjectProcessorConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous transform that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncProcessorConfiguration.CreateAsyncProcessor(IActivityMonitor, IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured processor or null for the void processor.</returns>
        public static Func<object, ValueTask<object?>>? CreateAsyncProcessor( this ObjectAsyncProcessorConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateAsyncProcessor( monitor, EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="IObjectProcessorHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncProcessorConfiguration.CreateAsyncHook(IActivityMonitor, ProcessorHookContext, IServiceProvider)"/>
        /// is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A configured wrapper bound to the hook context or null for the void processor.</returns>
        public static IObjectProcessorHook? CreateAsyncHook( this ObjectAsyncProcessorConfiguration @this, IActivityMonitor monitor, ProcessorHookContext context )
        {
            return @this.CreateAsyncHook( monitor, context, EmptyServiceProvider.Instance );
        }


        /// <summary>
        /// Creates a synchronous Processor that doesn't require any external service to do its job.
        /// <see cref="ObjectProcessorConfiguration.CreateProcessor(IActivityMonitor, IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object Processor or null for the void processor.</returns>
        public static Func<object, object>? CreateProcessor( this ObjectProcessorConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateProcessor( monitor, EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="ObjectProcessorHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectProcessorConfiguration.CreateHook(IActivityMonitor, ProcessorHookContext, IServiceProvider)"/> is
        /// called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for the void processor.</returns>
        public static ObjectProcessorHook? CreateHook( this ObjectProcessorConfiguration @this, IActivityMonitor monitor, ProcessorHookContext hook )
        {
            return @this.CreateHook( monitor, hook, EmptyServiceProvider.Instance );
        }

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }
    }
}
