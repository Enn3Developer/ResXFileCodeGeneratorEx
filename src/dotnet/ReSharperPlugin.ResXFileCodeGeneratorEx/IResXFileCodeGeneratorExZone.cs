using JetBrains.Application.BuildScript.Application.Zones;
#if RIDER
using JetBrains.RdBackend.Common.Env;
#endif

namespace ReSharperPlugin.ResXFileCodeGeneratorEx
{
    [ZoneDefinition]
    public interface IResXFileCodeGeneratorExZone : IZone
#if RIDER
        , IRequire<IResharperHostCorePlatformZone>
#endif
    {
    }
}
