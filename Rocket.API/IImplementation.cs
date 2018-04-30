﻿using Rocket.API.Commands;
using Rocket.API.Configuration;
using Rocket.API.Eventing;

namespace Rocket.API
{
    public interface IImplementation : IEventEmitter, IConfigurationContext
    {
        string InstanceId { get; }
        void Init(IRuntime runtime);
        void Shutdown();
        void Reload();

        IConsoleCommandCaller GetConsoleCaller();
    }
}