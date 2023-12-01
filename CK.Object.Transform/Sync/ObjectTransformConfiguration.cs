using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Security;

namespace CK.Object.Transform
{
    /// <summary>
    /// Configuration base class for synchronous transform functions.
    /// </summary>
    public abstract partial class ObjectTransformConfiguration : IObjectTransformConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section.
        /// <para>
        /// The required signature constructor for specialized class is
        /// <c>( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )</c>.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration section.</param>
        protected ObjectTransformConfiguration( ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates a synchronous transform function.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public abstract Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Creates a <see cref="ObjectTransformHook"/> with this configuration and a function obtained by
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
        public virtual ObjectTransformHook? CreateHook( IActivityMonitor monitor, TransformHookContext hook, IServiceProvider services )
        {
            var p = CreateTransform( monitor, services );
            return p != null ? new ObjectTransformHook( hook, this, p ) : null;
        }

        /// <summary>
        /// Creates a synchronous transform function that doesn't require any external service to do its job.
        /// <see cref="CreateTransform(IActivityMonitor, IServiceProvider)"/> is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public Func<object, object>? CreateTransform( IActivityMonitor monitor ) => CreateTransform( monitor, EmptyServiceProvider.Instance );

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }

        /// <summary>
        /// Creates an <see cref="ObjectTransformHook"/> that doesn't require any external service to do its job.
        /// <see cref="CreateHook(IActivityMonitor, TransformHookContext, IServiceProvider)"/> is called with an
        /// empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="hook">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for an identity function.</returns>
        public ObjectTransformHook? CreateHook( IActivityMonitor monitor, TransformHookContext hook ) => CreateHook( monitor, hook, EmptyServiceProvider.Instance );

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous <see cref="ObjectTransformConfiguration"/>.
        /// <list type="bullet">
        /// <item>The transform functions must be in the "CK.Object.Transform" namespace.</item>
        /// <item>Their name must end with "TransformConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Transform" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        public static void AddResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Transforms" )
        {
            builder.AddResolver( new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectTransformConfiguration ),
                                             typeNamespace: "CK.Object.Transform",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Transform",
                                             defaultCompositeBaseType: typeof( SequenceTransformConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName ) );

        }

    }
}
