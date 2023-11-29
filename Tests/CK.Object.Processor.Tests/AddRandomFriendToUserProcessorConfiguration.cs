using CK.Core;
using CK.Object.Processor.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CK.Object.Processor
{
    public sealed class AddRandomFriendToUserProcessorConfiguration : ObjectProcessorConfiguration
    {
        readonly int _minAge;

        public AddRandomFriendToUserProcessorConfiguration( IActivityMonitor monitor,
                                                            PolymorphicConfigurationTypeBuilder builder,
                                                            ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            _minAge = configuration.TryGetIntValue( monitor, "MinAge", 1, 99 ) ?? 0;
        }

        protected override Func<object, bool>? CreateIntrinsicCondition( IActivityMonitor monitor, IServiceProvider services )
        {
            return o => o is UserRecord u && u.Age >= _minAge;
        }

        protected override Func<object, object>? CreateIntrinsicTransform( IActivityMonitor monitor, IServiceProvider services )
        {
            var userServices = services.GetRequiredService<UserService>();
            return o =>
            {
                var u = ((UserRecord)o);
                u.Friends.Add( userServices.Users[Random.Shared.Next( userServices.Users.Count )] );
                return o;
            };
        }

    }
}
