﻿using System;
using Jasper.Configuration;

namespace Jasper.Http.Model
{
    /// <summary>
    /// Base class for custom attributes that modify route configurations
    /// </summary>
    public abstract class ModifyRouteAttribute : Attribute, IModifyChain<RouteChain>
    {
        public abstract void Modify(RouteChain chain, JasperGenerationRules rules);
    }
}
