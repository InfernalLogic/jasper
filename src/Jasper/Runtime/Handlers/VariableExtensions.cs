﻿using LamarCodeGeneration.Model;

namespace Jasper.Runtime.Handlers;

public static class VariableExtensions
{
    public static bool ShouldBeCascaded(this Variable variable)
    {
        return !variable.Properties.ContainsKey(HandlerChain.NotCascading);
    }

    public static void MarkAsNotCascaded(this Variable variable)
    {
        variable.Properties.Add(HandlerChain.NotCascading, true);
    }
}
