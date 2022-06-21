using System;
using Baseline.Reflection;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;

namespace Jasper.Persistence.Sagas;

internal class CreateNewSagaFrame : SyncFrame
{
    public CreateNewSagaFrame(Type sagaType)
    {
        if (!sagaType.HasDefaultConstructor())
        {
            throw new ArgumentOutOfRangeException(nameof(sagaType),
                $"For now, Jasper requires that Saga types have a public, no-arg default constructor. Missing on {sagaType.FullNameInCode()}");
        }

        Saga = new Variable(sagaType, this);
    }

    public Variable Saga { get; }

    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write($"var {Saga.Usage} = new {Saga.VariableType.FullNameInCode()}();");
        Next?.GenerateCode(method, writer);
    }
}