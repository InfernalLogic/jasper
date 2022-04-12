using System.Diagnostics;
using Jasper.Attributes;
using Jasper.Configuration;
using Lamar;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;
using Microsoft.Extensions.Logging;
using ILogger = Castle.Core.Logging.ILogger;

namespace DocumentationSamples
{
    public class Middleware
    {
        public static void Stopwatch(ILogger logger)
        {
            #region sample_stopwatch_concept
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                // execute the HTTP request
                // or message
            }
            finally
            {
                stopwatch.Stop();
                logger.Info("Ran something in " + stopwatch.ElapsedMilliseconds);
            }
            #endregion
        }
    }


    #region sample_StopwatchFrame
    public class StopwatchFrame : SyncFrame
    {
        private readonly IChain _chain;
        private Variable _logger;
        private readonly Variable _stopwatch;

        public StopwatchFrame(IChain chain)
        {
            _chain = chain;

            // This frame creates a Stopwatch, so we
            // expose that fact to the rest of the generated method
            // just in case someone else wants that
            _stopwatch = new Variable(typeof(Stopwatch), "stopwatch", this);
        }


        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.Write($"var stopwatch = new {typeof(Stopwatch).FullNameInCode()}();");
            writer.Write($"stopwatch.Start();");

            writer.Write("BLOCK:try");
            Next?.GenerateCode(method, writer);
            writer.FinishBlock();

            // Write a finally block where you record the stopwatch
            writer.Write("BLOCK:finally");

            writer.Write($"stopwatch.Stop();");
            writer.Write($"{_logger.Usage}.Log(Microsoft.Extensions.Logging.LogLevel.Information, \"{_chain.Description} ran in \" + {_stopwatch.Usage}.{nameof(Stopwatch.ElapsedMilliseconds)});)");

            writer.FinishBlock();
        }

        public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
        {
            // This in effect turns into "I need ILogger<IChain> injected into the
            // compiled class"
            _logger = chain.FindVariable(typeof(ILogger<IChain>));
            yield return _logger;
        }
    }
    #endregion

    #region sample_StopwatchAttribute
    public class StopwatchAttribute : ModifyChainAttribute
    {
        public override void Modify(IChain chain, GenerationRules rules, IContainer container)
        {
            chain.Middleware.Add(new StopwatchFrame(chain));
        }
    }
    #endregion

    #region sample_ClockedEndpoint
    public class ClockedEndpoint
    {
        [Stopwatch]
        public string get_clocked()
        {
            return "how fast";
        }
    }
    #endregion
}
