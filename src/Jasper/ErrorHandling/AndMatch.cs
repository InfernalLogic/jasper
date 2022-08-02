using System;
using System.Collections.Generic;
using System.Linq;
using Baseline;

namespace Jasper.ErrorHandling;

public class AndMatch : IExceptionMatch
{
    public readonly List<IExceptionMatch> Inners = new();

    public AndMatch(params IExceptionMatch[] matches)
    {
        Inners.AddRange(matches);
    }

    public bool Matches(Exception ex)
    {
        return Inners.All(x => x.Matches(ex));
    }

    public string Description => Inners.Select(x => ExceptionMatchExtensions.Formatted(x)).Join(" and ");

}
