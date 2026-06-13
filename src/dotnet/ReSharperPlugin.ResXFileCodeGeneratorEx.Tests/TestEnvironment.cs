using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.ResXFileCodeGeneratorEx.Tests
{
    [ZoneDefinition]
    public class ResXFileCodeGeneratorExTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IResXFileCodeGeneratorExZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<ResXFileCodeGeneratorExTestEnvironmentZone> { }

    [SetUpFixture]
    public class ResXFileCodeGeneratorExTestsAssembly : ExtensionTestEnvironmentAssembly<ResXFileCodeGeneratorExTestEnvironmentZone> { }
}
