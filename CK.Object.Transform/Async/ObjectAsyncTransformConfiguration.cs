using CK.Core;
using System;
using System.Security;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Configuration base class for asynchronous transform functions.
    /// </summary>
    public abstract partial class ObjectAsyncTransformConfiguration : IObjectTransformConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section.
        /// <para>
        /// The required signature constructor for specialized class is
        /// <c>( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )</c>.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration serction.</param>
        protected ObjectAsyncTransformConfiguration( ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates an asynchronous transform function that requires external services to do its job.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public abstract Func<object, ValueTask<object>>? CreateTransform( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Creates a <see cref="ObjectAsyncTransformHook"/> with this configuration and a function obtained by
        /// calling <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this transform function relies on other transform functions in order to hook all of them.
        /// Failing to do so will hide some transform functions to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A wrapper bound to the hook context or null for an identity function.</returns>
        public virtual ObjectAsyncTransformHook? CreateHook( IActivityMonitor monitor, TransformHookContext hook, IServiceProvider services )
        {
            var p = CreateTransform( monitor, services );
            return p != null ? new ObjectAsyncTransformHook( hook, this, p ) : null;
        }

        /// <summary>
        /// Creates an asynchronous transform function that doesn't require any external service to do its job.
        /// <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public Func<object, ValueTask<object>>? CreateTransform( IActivityMonitor monitor ) => CreateTransform( monitor, ObjectTransformConfiguration.EmptyServiceProvider.Instance );

        /// <summary>
        /// Creates an <see cref="ObjectAsyncTransformHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, TransformHookContext, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for an identity function.</returns>
        public ObjectAsyncTransformHook? CreateHook( IActivityMonitor monitor, TransformHookContext hook ) => CreateHook( monitor, hook, ObjectTransformConfiguration.EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for asynchronous <see cref="ObjectAsyncTransformConfiguration"/>.
        /// <list type="bullet">
        /// <item>The transform functions must be in the "CK.Object.Transform" namespace (same as the synchronous <see cref="ObjectTransformConfiguration"/>).</item>
        /// <item>Their name must end with "AsyncTransformConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Transform" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder,
                                        bool allowOtherNamespace = false,
                                        string compositeItemsFieldName = "Transforms" )
        {
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectAsyncTransformConfiguration ),
                                             typeNamespace: "CK.Object.Transform",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "AsyncTransform",
                                             compositeBaseType: typeof( SequenceAsyncTransformConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName ) );
        }

    }
}
