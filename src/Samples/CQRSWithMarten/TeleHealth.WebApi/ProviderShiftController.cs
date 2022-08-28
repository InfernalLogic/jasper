using Jasper;
using Jasper.Attributes;
using Jasper.Persistence.Marten;
using Marten;
using Marten.AspNetCore;
using Marten.Exceptions;
using Marten.Schema;
using Microsoft.AspNetCore.Mvc;
using TeleHealth.Common;

namespace TeleHealth.WebApi;

public record CompleteCharting(
    Guid ShiftId,
    int Version
);

public class ProviderShiftController : ControllerBase
{
    [HttpGet("/shift/{shiftId}")]
    public Task GetProviderShift(Guid shiftId, [FromServices] IQuerySession session)
    {
        return session.Json.WriteById<ProviderShift>(shiftId, HttpContext);
    }

    public Task CompleteCharting(
        [FromBody] CompleteCharting charting,
        [FromServices] IDocumentSession session)
    {
        return session
            .Events
            .WriteToAggregate<ProviderShift>(charting.ShiftId, charting.Version, stream =>
        {
            if (stream.Aggregate.Status != ProviderStatus.Charting)
            {
                throw new Exception("The shift is not currently charting");
            }

            var finished = new ChartingFinished();
            stream.AppendOne(finished);
        });
    }
}

// This is auto-discovered by Jasper
public class CompleteChartingHandler
{
    // Decider pattern
    [MartenCommandWorkflow(AggregateLoadStyle.Exclusive)] // this opts into some Jasper middlware
    public ChartingFinished Handle(CompleteCharting charting, ProviderShift shift)
    {
        if (shift.Status != ProviderStatus.Charting)
        {
            throw new Exception("The shift is not currently charting");
        }

        return new ChartingFinished();
    }
}