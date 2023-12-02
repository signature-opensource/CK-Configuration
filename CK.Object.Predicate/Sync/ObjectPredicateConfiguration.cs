using CK.Core;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Configuration base class for synchronous predicates.
    /// </summary>
    public abstract class ObjectPredicateConfiguration : ObjectAsyncPredicateConfiguration
    {
        static readonly object _initDefaultLock = new object();
        static PolymorphicConfigurationTypeBuilder.StandardTypeResolver? _defaultSyncResolver;

        /// <summary>
        /// Captures the configuration section's path (that must be valid, see <see cref="MutableConfigurationSection.IsValidPath(ReadOnlySpan{char})"/>).
        /// <para>
        /// The required signature constructor for specialized class is
        /// <c>( IActivityMonitor monitor, PolymorphicConfigurationTypeBuilder builder, ImmutableConfigurationSection configuration )</c>.
        /// </para>
        /// </summary>
        /// <param name="configurationPath">The configuration path.</param>
        protected ObjectPredicateConfiguration( string configurationPath )
            : base( configurationPath ) 
        {
        }

        /// <summary>
        /// Adapts this synchronous predicate thanks to a <see cref="ValueTask.FromResult{TResult}(TResult)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public sealed override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IActivityMonitor monitor, IServiceProvider services )
        {
            var p = CreatePredicate(monitor, services);
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }

        /// <summary>
        /// Creates a synchronous predicate.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured object predicate or null for an empty predicate.</returns>
        public abstract Func<object, bool>? CreatePredicate( IActivityMonitor monitor, IServiceProvider services );

        /// <summary>
        /// Definite relay to <see cref="CreateHook(IActivityMonitor, PredicateHookContext, IServiceProvider)"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A wrapper bound to the hook context or null for an empty predicate.</returns>
        public sealed override IObjectPredicateHook? CreateAsyncHook( IActivityMonitor monitor, PredicateHookContext context, IServiceProvider services )
        {
            return CreateHook( monitor, context, services );
        }

        /// <summary>
        /// Creates a <see cref="ObjectPredicateHook"/> with this configuration and a predicate obtained by
        /// calling <see cref="CreatePredicate(IActivityMonitor, IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this predicate relies on other predicates in order to hook all of them.
        /// Failing to do so will hide some predicates to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A wrapper bound to the hook context or null for an empty predicate.</returns>
        public virtual ObjectPredicateHook? CreateHook( IActivityMonitor monitor, PredicateHookContext context, IServiceProvider services )
        {
            var p = CreatePredicate( monitor, services );
            return p != null ? new ObjectPredicateHook( context, this, p ) : null;
        }

        /// <summary>
        /// Adds a <see cref="PolymorphicConfigurationTypeBuilder.TypeResolver"/> for synchronous predicates only.
        /// <list type="bullet">
        /// <item>The predicates must be in the "CK.Object.Predicate" namespace.</item>
        /// <item>Their name must end with "PredicateConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Predicate" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        /// <returns>The added resolver.</returns>
        public static PolymorphicConfigurationTypeBuilder.StandardTypeResolver AddSynchronousOnlyResolver( PolymorphicConfigurationTypeBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Predicates" )
        {
            var sync = !allowOtherNamespace && compositeItemsFieldName == "Predicates"
                        ? EnsureDefault()
                        : new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                                baseType: typeof( ObjectPredicateConfiguration ),
                                                typeNamespace: "CK.Object.Predicate",
                                                allowOtherNamespace: allowOtherNamespace,
                                                familyTypeNameSuffix: "Predicate",
                                                tryCreateFromTypeName: TryCreateFromTypeName,
                                                defaultCompositeBaseType: typeof( GroupPredicateConfiguration ),
                                                compositeItemsFieldName: compositeItemsFieldName );

            builder.AddResolver( sync );
            return sync;

            static PolymorphicConfigurationTypeBuilder.StandardTypeResolver EnsureDefault()
            {
                if( _defaultSyncResolver == null )
                {
                    lock( _initDefaultLock )
                    {
                        _defaultSyncResolver ??= new PolymorphicConfigurationTypeBuilder.StandardTypeResolver(
                                                            baseType: typeof( ObjectPredicateConfiguration ),
                                                            typeNamespace: "CK.Object.Predicate",
                                                            familyTypeNameSuffix: "Predicate",
                                                            tryCreateFromTypeName: TryCreateFromTypeName,
                                                            defaultCompositeBaseType: typeof( GroupPredicateConfiguration ),
                                                            compositeItemsFieldName: "Predicates" );
                    }
                }
                return _defaultSyncResolver;
            }
        }

    }
}
