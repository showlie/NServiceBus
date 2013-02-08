﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_a_message_that_is_mapped_to_an_existing_saga_instance : NServiceBusIntegrationTest
    {
        static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();

        [Test]
        public void Should_hydrate_and_invoke_the_existing_instance()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<SagaEndpoint>()
                    .Done(c => c.SecondMessageReceived)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                    {
                        Assert.AreEqual(c.FirstSagaInstance, c.SecondSagaInstance, "The same saga instance should be invoked invoked for both messages");
                    })

                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

            public Guid FirstSagaInstance { get; set; }
            public Guid SecondSagaInstance { get; set; }
        }

        public class SagaEndpoint : EndpointBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .When(bus =>
                        {
                            bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn });
                            bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn, SecondMessage = true });
                        });
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;

                    if (message.SecondMessage)
                    {
                        Context.SecondSagaInstance = Data.Id;
                        Context.SecondMessageReceived = true;
                    }
                    else
                    {
                        Context.FirstSagaInstance = Data.Id;
                    }
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSagaMessage>(m=>m.SomeId)
                        .ToSaga(s=>s.SomeId);
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
                public Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}