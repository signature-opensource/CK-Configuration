using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using StrategyPlugin;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests
{

    [TestFixture]
    public class ConfigurationWithPlaceholderTests
    {
        [Test]
        public void Configuration_with_placeholders_and_replacements()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                """
                {
                    "DefaultAssembly": "ConsumerB.Plugins",
                    "Strategies":
                    [
                        {
                            "Type": "Placeholder"
                        },
                        {
                            "Type": "ESimple"
                        },
                        {
                            "Type": "Placeholder"
                        },
                        {
                            "Type": "ExtensibleComposite",
                            "Strategies": [
                                {
                                    "Type": "ESimple"
                                },
                                {
                                    "Type": "Placeholder"
                                },
                                {
                                    "Type": "ESimple"
                                }
                            ]
                        },
                        {
                            "Type": "Placeholder"
                        }
                    ]
                }
                """ );
            var builder = new PolymorphicConfigurationTypeBuilder();
            ExtensibleStrategyConfiguration.AddResolver( builder );
            var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );
            sC = CheckNotNullAndRun( sC, 3 );

            var setFirst = new MutableConfigurationSection( "Root:Strategies:0", "<Dynamic>" );
            setFirst["Type"] = "ESimple";

            sC.TrySetPlaceholder( TestHelper.Monitor, setFirst, out var sC2 ).Should().BeTrue();
            sC2 = CheckNotNullAndRun( sC2, 4 );

            var setInComposite = new MutableConfigurationSection( "Root:Strategies:3:Strategies:1", "<Dynamic>" );
            setInComposite["Type"] = "ESimple";
            sC2.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var sC3 ).Should().BeTrue();
            CheckNotNullAndRun( sC3, 5 );

            // From sC.
            sC.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var sC2bis ).Should().BeTrue();
            CheckNotNullAndRun( sC2bis, 4 );

            static ExtensibleStrategyConfiguration CheckNotNullAndRun( ExtensibleStrategyConfiguration? sC, int expected )
            {
                Throw.DebugAssert( sC != null );
                var s = sC.CreateStrategy( TestHelper.Monitor );
                Throw.DebugAssert( s != null );
                s.DoSomething( TestHelper.Monitor, 0 ).Should().Be( expected );
                return sC;
            }
        }
    }
}
