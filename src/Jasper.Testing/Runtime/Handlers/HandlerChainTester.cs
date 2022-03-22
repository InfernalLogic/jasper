﻿using System.Linq;
using Jasper.Attributes;
using Jasper.Runtime.Handlers;
using Lamar;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using Shouldly;
using TestMessages;
using Xunit;

namespace Jasper.Testing.Runtime.Handlers
{
    public class HandlerChainTester
    {
        public class Target
        {
            [Middleware(typeof(FakeMiddleware1), typeof(FakeMiddleware2))]
            public void Go(Message1 message)
            {
            }

            public static void GoStatic(Message2 message)
            {
            }
        }

        public class FakeMiddleware1 : Frame
        {
            public FakeMiddleware1() : base(false)
            {
            }

            public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
            {
            }
        }

        public class FakeMiddleware2 : Frame
        {
            public FakeMiddleware2() : base(false)
            {
            }

            public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
            {
            }
        }

        [Fact]
        public void apply_generic_middleware()
        {
            var chain = HandlerChain.For<Target>(x => x.Go(null), null);
            var frames = chain.DetermineFrames(new GenerationRules(), Container.Empty());

            chain.Middleware.Any(x => x is FakeMiddleware1).ShouldBeTrue();
            chain.Middleware.Any(x => x is FakeMiddleware2).ShouldBeTrue();
        }

        [Fact]
        public void create_by_method()
        {
            var chain = HandlerChain.For<Target>(x => x.Go(null), null);
            chain.MessageType.ShouldBe(typeof(Message1));

            var methodCall = chain.Handlers.Single();
            methodCall.HandlerType.ShouldBe(typeof(Target));
            methodCall.Method.Name.ShouldBe(nameof(Target.Go));
        }

        [Fact]
        public void create_by_static_method()
        {
            var chain = HandlerChain.For<Target>(nameof(Target.GoStatic), null);

            chain.MessageType.ShouldBe(typeof(Message2));

            var methodCall = chain.Handlers.Single();
            methodCall.HandlerType.ShouldBe(typeof(Target));
            methodCall.Method.Name.ShouldBe(nameof(Target.GoStatic));
        }

        [Fact]
        public void default_number_of_max_attempts_is_null()
        {
            var chain = HandlerChain.For<Target>(nameof(Target.GoStatic), null);
            chain.Retries.MaximumAttempts.HasValue.ShouldBeFalse();
        }


    }
}
