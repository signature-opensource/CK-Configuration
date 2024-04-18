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
        private const string WithAQNName = "With AQN type name";
        private const string WithTypeNameOnly = "With type name only";

        [TestCase( WithAQNName )]
        [TestCase( WithTypeNameOnly )]
        public void type_name_only_lookup_use_the_DefaultAssembly( string nameKind )
        {
            var m = new MutableConfigurationSection( "A" );
            m["DefaultAssembly"] = "StrategyPlugin";
            var c = new ImmutableConfigurationSection( m );
            var a = AssemblyConfiguration.Create( TestHelper.Monitor, c );
            Throw.DebugAssert( a != null );

            var typeName = nameKind == WithAQNName
                            ? typeof( CompositeStrategy ).AssemblyQualifiedName!
                            : "CompositeStrategy";
            var t = a.TryResolveType( TestHelper.Monitor,
                                      typeName,
                                      typeNamespace: "StrategyPlugin",
                                      // Below are the default values (here for documentation purposes):
                                      isOptional: false,
                                      fallbackAssembly: null,
                                      allowOtherNamespace: false,
                                      familyTypeNameSuffix: null,
                                      typeNameSuffix: "Configuration",
                                      errorPrefix: null,
                                      errorSuffix: null );
            t.Should().NotBeNull();
        }

        [TestCase( "No DefaultAssembly", WithAQNName )]
        [TestCase( "No DefaultAssembly", WithTypeNameOnly )]
        [TestCase( "DefaultAssembly is another one", WithAQNName )]
        [TestCase( "DefaultAssembly is another one", WithTypeNameOnly )]
        public void type_name_lookup_can_use_the_fallbackDefaultAssembly_even_when( string cause, string nameKind )
        {
            var m = new MutableConfigurationSection( "A" );
            if( cause == "DefaultAssembly is another one" )
            {
                m["DefaultAssembly"] = "ConsumerB.Plugins";
            }

            var typeName = nameKind == WithAQNName
                            ? typeof( CompositeStrategy ).AssemblyQualifiedName!
                            : "CompositeStrategy";

            var c = new ImmutableConfigurationSection( m );
            var a = AssemblyConfiguration.Create( TestHelper.Monitor, c );
            Throw.DebugAssert( a != null );
            var t = a.TryResolveType( TestHelper.Monitor, typeName, typeNamespace: "StrategyPlugin", fallbackAssembly: typeof(IStrategy).Assembly );
            t.Should().NotBeNull();
        }

        [TestCase( WithAQNName )]
        [TestCase( WithTypeNameOnly )]
        public void type_name_lookup_can_use_the_alias_or_the_assembly_name( string nameKind )
        {
            var m = new MutableConfigurationSection( "A" );
            m["Assemblies:0:Assembly"] = "StrategyPlugin";
            m["Assemblies:0:Alias"] = "Strat";

            var typeName = nameKind == WithAQNName
                            ? typeof( CompositeStrategy ).AssemblyQualifiedName!
                            : "CompositeStrategy, Strat";

            var c = new ImmutableConfigurationSection( m );
            var a = AssemblyConfiguration.Create( TestHelper.Monitor, c );
            Throw.DebugAssert( a != null );
            var t = a.TryResolveType( TestHelper.Monitor, typeName, typeNamespace: "StrategyPlugin", fallbackAssembly: typeof(IStrategy).Assembly );
            t.Should().NotBeNull();
        }

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
                                         """ "Assemblies":[ "Locked", "ConsumerA.Strategy" ], """,
                                         """ "Assemblies":[ "IsLocked", "ConsumerA.Strategy" ], """,
                                         """ "Assemblies":[ "Lock", "ConsumerA.Strategy" ], """} )
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
                    var builder = new TypedConfigurationBuilder();
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
