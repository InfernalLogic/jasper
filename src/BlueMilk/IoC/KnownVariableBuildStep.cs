﻿using System.Collections.Generic;
using BlueMilk.Codegen;

namespace BlueMilk.IoC
{
    public class KnownVariableBuildStep : BuildStep
    {
        public Variable Variable { get; }

        public KnownVariableBuildStep(Variable variable) : base(variable.VariableType, true, false)
        {
            Variable = variable;
        }

        public override IEnumerable<BuildStep> ReadDependencies(BuildStepPlanner planner)
        {
            yield break;
        }

        protected override Variable buildVariable()
        {
            return Variable;
        }
    }
}