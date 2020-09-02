﻿namespace MassTransit.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Shouldly;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class Publishing_a_message_on_the_bus :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_include_a_conversation_id()
        {
            await Bus.Publish(new PingMessage());

            ConsumeContext<PingMessage> context = await _handled;

            context.ConversationId.HasValue.ShouldBe(true);
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _handled = Handled<PingMessage>(configurator);
        }
    }


    [TestFixture]
    public class Sending_a_message :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_include_a_conversation_id()
        {
            await InputQueueSendEndpoint.Send(new PingMessage());

            ConsumeContext<PingMessage> context = await _handled;

            context.ConversationId.HasValue.ShouldBe(true);
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _handled = Handled<PingMessage>(configurator);
        }
    }


    [TestFixture]
    public class Publishing_from_a_handler :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_include_a_conversation_id()
        {
            Task<ConsumeContext<PongMessage>> responseHandled = ConnectPublishHandler<PongMessage>();

            await InputQueueSendEndpoint.Send(new PingMessage());

            ConsumeContext<PingMessage> context = await _handled;

            context.ConversationId.HasValue.ShouldBe(true);

            ConsumeContext<PongMessage> responseContext = await responseHandled;

            responseContext.ConversationId.HasValue.ShouldBe(true);

            responseContext.ConversationId.ShouldBe(context.ConversationId);
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _handled = Handler<PingMessage>(configurator, context => context.RespondAsync(new PongMessage(context.Message.CorrelationId)));
        }
    }


    [TestFixture]
    public class Starting_a_new_conversation :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_include_a_new_conversation_id()
        {
            Task<ConsumeContext<PongMessage>> responseHandled = ConnectPublishHandler<PongMessage>();

            var conversationId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage(), x => x.ConversationId = conversationId);

            ConsumeContext<PingMessage> context = await _handled;

            Assert.That(context.ConversationId.HasValue);

            ConsumeContext<PongMessage> responseContext = await responseHandled;

            Assert.That(responseContext.ConversationId.HasValue);

            Assert.That(responseContext.ConversationId.Value, Is.Not.EqualTo(conversationId));

            Assert.That(responseContext.Headers.Get<Guid>(MessageHeaders.InitiatingConversationId), Is.EqualTo(conversationId));
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _handled = Handler<PingMessage>(configurator, context =>
                context.RespondAsync(new PongMessage(context.Message.CorrelationId), x => x.StartNewConversation()));
        }
    }

    [TestFixture]
    public class Starting_a_new_conversation_from_a_new_message :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_not_capture_an_initiating_conversation_id()
        {
            var conversationId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage(), x => x.StartNewConversation(conversationId));

            ConsumeContext<PingMessage> context = await _handled;

            Assert.That(context.ConversationId.HasValue);

            Assert.That(context.ConversationId.Value, Is.EqualTo(conversationId));

            Assert.That(context.Headers.Get<Guid>(MessageHeaders.InitiatingConversationId), Is.Null);
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _handled = Handled<PingMessage>(configurator);
        }
    }
}
