﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Saga;
    using NUnit.Framework;

    public class When_message_has_a_saga_id : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_start_a_new_saga_if_not_found()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                {
                    var message = new MessageWithSagaId();

                    bus.SetMessageHeader(message, Headers.SagaId, Guid.NewGuid().ToString());
                    bus.SetMessageHeader(message, Headers.SagaType, typeof(MySaga).AssemblyQualifiedName);

                    bus.SendLocal(message);
                }))
                .Done(c => c.NotFoundHandlerCalled)
                .Run(TimeSpan.FromSeconds(15));

            Assert.True(context.NotFoundHandlerCalled);
            Assert.False(context.MessageHandlerCalled);
            Assert.False(context.TimeoutHandlerCalled);
        }

        class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<MessageWithSagaId>,
            IHandleTimeouts<MessageWithSagaId>,
            IHandleSagaNotFound
        {
            public Context Context { get; set; }

            public class SagaData : ContainSagaData
            {
            }

            public void Handle(MessageWithSagaId message)
            {
                Context.MessageHandlerCalled = true;
            }

            public void Handle(object message)
            {
                Context.NotFoundHandlerCalled = true;
            }

            public void Timeout(MessageWithSagaId state)
            {
                Context.TimeoutHandlerCalled = true;
            }
        }

        class Context : ScenarioContext
        {
            public bool NotFoundHandlerCalled { get; set; }
            public bool MessageHandlerCalled { get; set; }
            public bool TimeoutHandlerCalled { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MessageWithSagaId : IMessage
        {
        }
    }
}