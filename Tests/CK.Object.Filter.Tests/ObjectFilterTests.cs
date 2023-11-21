using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Collections;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Filter.Tests
{
    [TestFixture]
    public class ObjectFilterTests
    {
        [Test]
        public void empty_configuration_fails()
        {
            var config = new MutableConfigurationSection( "Root" );

            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectFilterConfiguration.AddResolver( builder );
            using( TestHelper.Monitor.CollectTexts( out var logs ) )
            {
                var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config );
                fC.Should().BeNull();
                logs.Should().Contain( "Configuration 'Root' must have children to be considered a default 'GroupFilterConfiguration'." );
            }
        }

        [TestCase( true )]
        [TestCase( false )]
        public void type_can_be_true_or_false( bool always )
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Condition"] = always.ToString();
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectFilterConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );
            var f = fC.CreatePredicate( TestHelper.Monitor );
            f( this ).Should().Be( always );
        }

        [TestCase( "All" )]
        [TestCase( "Any" )]
        public void type_can_be_All_or_Any( string t )
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Condition"] = t;
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectFilterConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );
            var f = fC.CreatePredicate( TestHelper.Monitor );
            // "All" on empty Filters defaults to true, "Any" defaults to false.
            f( this ).Should().Be( t == "All" );
        }

        [Test]
        public void default_group_is_All()
        {
            var config = new MutableConfigurationSection( "Root" );
            config["Conditions:0:Type"] = "true";
            var builder = new PolymorphicConfigurationTypeBuilder();
            // Relaces default "Filters" by "Conditions".
            ObjectFilterConfiguration.AddResolver( builder, compositeItemsFieldName: "Conditions" );

            var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            fC.Should().BeAssignableTo<GroupFilterConfiguration>();
            ((GroupFilterConfiguration)fC).Filters.Should().HaveCount( 1 );
            ((GroupFilterConfiguration)fC).All.Should().BeTrue();
            ((GroupFilterConfiguration)fC).Any.Should().BeFalse();
            ((GroupFilterConfiguration)fC).AtLeast.Should().Be( 0 );
            ((GroupFilterConfiguration)fC).Filters[0].Should().BeAssignableTo<AlwaysTrueFilterConfiguration>();

            var f = fC.CreatePredicate( TestHelper.Monitor );
            f( this ).Should().BeTrue();
        }

        [Test]
        public void complex_json_tree()
        {
            var config = new MutableConfigurationSection( "Root" );
            config.AddJson( """
                {
                    // No type resolved to "All" ("And" connector).
                    "Filters": [
                        {
                            "Filters": [
                                {
                                    // This is the same as below.
                                    "Type": true,
                                },
                                {
                                    // An intrinsic AlwaysTrue object filter.
                                    // A "AlwaysFalse" is also available.
                                    "Type": "AlwaysTrue",
                                },
                                {
                                    // This (stupid) filter is implemented in this assembly.
                                    "Assemblies": {"CK.Object.Filter.Tests": "Tests"},
                                    "Type": "EnumerableMaxCount, Tests",
                                    "MaxCount": 5
                                }
                            ]
                        },
                        {
                            // "Any" is the "Or" connector.
                            "Type": "Any",
                            "Assemblies": {"CK.Object.Filter.Tests": "P"},
                            "Filters": [
                                {
                                    "Type": "StringContains, P",
                                    "Content": "A"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "B"
                                },
                            ]
                        },
                        {
                            // "Group" with AtLeast.
                            "Type": "Group",
                            "AtLeast": 2,
                            "Assemblies": {"CK.Object.Filter.Tests": "P"},
                            "Filters": [
                                {
                                    "Type": "StringContains, P",
                                    "Content": "x"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "y"
                                },
                                {
                                    "Type": "StringContains, P",
                                    "Content": "z"
                                },
                            ]
                        },
                    ]
                }
                """ );
            var builder = new PolymorphicConfigurationTypeBuilder();
            ObjectFilterConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectFilterConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var f = fC.CreatePredicate( TestHelper.Monitor );
            f( 0 ).Should().Be( false );
            f( "Ax" ).Should().Be( false );
            f( "Axy" ).Should().Be( true );
            f( "Bzy" ).Should().Be( true );
            f( "Bzy but too long" ).Should().Be( false );
        }
    }
}
