﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Rocket.API.DependencyInjection;
using Rocket.API.Scheduler;
using Rocket.Core.Plugins;
using Rocket.Core.Eventing;

namespace Rocket.Tests
{
    public class TestPlugin : PluginBase
    {
        public override IEnumerable<string> Capabilities => new List<string>() { "TESTING" };

        public override string Name => "Test Plugin";

        public TestPlugin(IDependencyContainer container) : base(container)
        {
            Logger.Info("Constructing TestPlugin (From plugin)");

        }

        class TestEvent : Event
        {
            public bool Value { get; set; }

            public TestEvent() : base(ExecutionTargetContext.Sync)
            {

            }
        }

        public Task<bool> TestEventing()
        {
            var promise = new TaskCompletionSource<bool>();

            Subscribe<TestEvent>((arguments) =>
            {
                promise.SetResult(arguments.Value);
            });

            Emit(new TestEvent { Value = true});

            return promise.Task;
        }

        protected override void OnLoad()
        {
            Logger.Info("Hello World (From plugin)");
        }

        protected override void OnUnload()
        {
            Logger.Info("Bye World (From plugin)");
        }
    }
}
