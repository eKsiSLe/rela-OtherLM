using Autofac;
using relaDevicePlugin;

namespace ExampleOtherLmConnector;

public sealed class ExampleOtherLmConnectorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ExampleOtherLmConnectorDevice>()
            .As<ILMDevice>()
            .SingleInstance();
    }
}
