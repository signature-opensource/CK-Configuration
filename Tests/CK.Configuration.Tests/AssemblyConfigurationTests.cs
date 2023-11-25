using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using StrategyPlugin;
using static CK.Testing.MonitorTestHelper;

namespace CK.Configuration.Tests
{
    [TestFixture]
    public class AssemblyConfigurationTests
    {
        [TestCase( false )]
        [TestCase( true )]
        public void AssemblyConfiguration_lock_with_or_without_placeholder( bool usePlaceholder )
        {
            var raw = """
                      {
                          // Top1
                          "DefaultAssembly": "ConsumerB.Plugins",
                          "Type": "ExtensibleComposite",
                          "Strategies":
                          [
                              {
                                  // Top2
                                  "Type": "ExtensibleComposite",
                                  "Strategies": [
                                      {
                                          "Type": "ESimple"
                                      },
                                      {
                                          // Top3
                                          "Type": "Placeholder",
                                          // Bottom3
                                      },
                                      {
                                          "Type": "ESimple"
                                      }
                                  ],
                                  // Bottom2
                              }
                          ],
                          // Bottom1
                      }
                      """;

            foreach( var form in new[] { """ "Assemblies":"IsLocked", """,
                                         """ "Assemblies":"Locked", """,
                                         """ "Assemblies":"Lock", """,
                                         """ "Assemblies":{ "IsLocked": true }, """,
                                         """ "Assemblies":{ "Locked": true }, """,
                                         """ "Assemblies":{ "Lock": true }, """,
                                         """ "Assemblies":[ "Locked" ], """,
                                         """ "Assemblies":[ "Locked", "ConsumerA.Strategy" ], """} )
            {
                var ignoredPath = usePlaceholder
                                    ? "Assemblies is locked. Ignoring 'Root:Strategies:0:Strategies:1:<Dynamic>:Assemblies'."
                                    : "Assemblies is locked. Ignoring 'Root:Strategies:0:Strategies:1:Assemblies'.";
                Run( raw.Replace( "// Top1", form ),
                     usePlaceholder,
                     "Allowed assemblies are locked below 'Root'.",
                     ignoredPath,
                     form.Contains( "ConsumerA.Plugin" ) );
                Run( raw.Replace( "// Bottom1", form ),
                     usePlaceholder,
                     "Allowed assemblies are locked below 'Root'.",
                     ignoredPath,
                     form.Contains( "ConsumerA.Plugin" ) );

                Run( raw.Replace( "// Top2", form ),
                     usePlaceholder,
                     "Allowed assemblies are locked below 'Root:Strategies:0'.",
                     ignoredPath,
                     form.Contains( "ConsumerA.Plugin" ) );
                Run( raw.Replace( "// Bottom2", form ),
                     usePlaceholder,
                     "Allowed assemblies are locked below 'Root:Strategies:0'.",
                     ignoredPath,
                     form.Contains( "ConsumerA.Plugin" ) );

                // We cannot inject in Top3 when also injecting the "Assemblies": "Plugin, Disallowed"
                // Top3, above the placeholder, can only be tested with the placeholder replacement.
                if( usePlaceholder )
                {
                    Run( raw.Replace( "// Top3", form ),
                         usePlaceholder,
                         "Allowed assemblies are locked below 'Root:Strategies:0:Strategies:1'.",
                         ignoredPath,
                         form.Contains( "ConsumerA.Plugin" ) );

                    Run( raw.Replace( "// Bottom3", form ),
                         usePlaceholder,
                         "Allowed assemblies are locked below 'Root:Strategies:0:Strategies:1'.",
                         ignoredPath,
                         form.Contains( "ConsumerA.Plugin" ) );
                }

            }

            static void Run( string raw,
                             bool usePlaceholder,
                             string expectedInfo,
                             string expectedWarn,
                             bool withConsumerA )
            {
                if( !usePlaceholder )
                {
                    if( withConsumerA )
                    {
                        raw = raw.Replace( @"""Type"": ""Placeholder""", @"""Assemblies"": ""Plugin, Disallowed"", ""Type"": ""AnotherSimpleStrategy, ConsumerA.Strategy""" );
                    }
                    else
                    {
                        raw = raw.Replace( @"""Type"": ""Placeholder""", @"""Assemblies"": ""Plugin, Disallowed"", ""Type"": ""ESimple""" );
                    }
                }
                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var config = ImmutableConfigurationSection.CreateFromJson( "Root", raw );
                    var builder = new PolymorphicConfigurationTypeBuilder();
                    ExtensibleStrategyConfiguration.AddResolver( builder );
                    var sC = builder.Create<ExtensibleStrategyConfiguration>( TestHelper.Monitor, config );
                    Throw.DebugAssert( sC != null );
                    if( usePlaceholder )
                    {
                        var patch = new MutableConfigurationSection( "Root:Strategies:0:Strategies:1:<Dynamic>" );
                        patch["Assemblies"] = "Plugin, Disallowed";
                        patch["Type"] = "ESimple";
                        sC = sC.SetPlaceholder( TestHelper.Monitor, patch );
                        Throw.DebugAssert( sC != null );
                    }
                    logs.Should().Contain( expectedInfo );
                    logs.Should().Contain( expectedWarn );
                }
            }
        }
    }
}
