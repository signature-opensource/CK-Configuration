using CK.Core;
using System;

namespace CK.Object.Transform
{
    public sealed class AddSuffixTransformConfiguration : ObjectTransformConfiguration
    {
        readonly string _suffix;

        public AddSuffixTransformConfiguration( IActivityMonitor monitor,
                                                PolymorphicConfigurationTypeBuilder builder,
                                                ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
            _suffix = configuration["Suffix"] ?? "";
        }

        public override Func<object, object>? CreateTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            return Transform;
        }

        object Transform( object o )
        {
            if( o is not string s )
            {
                throw new ArgumentException( $"String expected, got '{o.GetType().ToCSharpName()}'." );
            }
            return s + _suffix;
        }
    }
}
