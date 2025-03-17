using CK.Core;
using Shouldly;
using NUnit.Framework;
using StrategyPlugin;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests;


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
        var builder = new TypedConfigurationBuilder();
        ExtensibleStrategyConfiguration.AddResolver( builder );
        var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );
        sC = CheckNotNullAndRun( sC, 3 );

        var setFirst = new MutableConfigurationSection( "Root:Strategies:0", "<Dynamic>" );
        setFirst["Type"] = "ESimple";

        var sC2 = sC.TrySetPlaceholder( TestHelper.Monitor, setFirst, out var _ );
        sC2 = CheckNotNullAndRun( sC2, 4 );

        var setInComposite = new MutableConfigurationSection( "Root:Strategies:3:Strategies:1", "<Dynamic>" );
        setInComposite["Type"] = "ESimple";
        var sC3 = sC2.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var _ );
        CheckNotNullAndRun( sC3, 5 );

        // From sC.
        var sC2bis = sC.TrySetPlaceholder( TestHelper.Monitor, setInComposite, out var _ );
        CheckNotNullAndRun( sC2bis, 4 );

        static ExtensibleStrategyConfiguration CheckNotNullAndRun( ExtensibleStrategyConfiguration? sC, int expected )
        {
            Throw.DebugAssert( sC != null );
            var s = sC.CreateStrategy( TestHelper.Monitor );
            Throw.DebugAssert( s != null );
            s.DoSomething( TestHelper.Monitor, 0 ).ShouldBe( expected );
            return sC;
        }
    }
}
