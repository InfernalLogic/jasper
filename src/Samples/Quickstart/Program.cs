#region sample_Quickstart_Program

using Jasper;
using Quickstart;

var builder = WebApplication.CreateBuilder(args);

// For now, this is enough to integrate Jasper into
// your application, but there'll be *much* more
// options later of course :-)
builder.Host.UseJasper();

// Some in memory services for our application, the
// only thing that matters for now is that these are
// systems built by the application's IoC container
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<IssueRepository>();


var app = builder.Build();

// An endpoint to create a new issue
app.MapPost("/issues/create", (CreateIssue body, ICommandBus bus) => bus.InvokeAsync(body));

// An endpoint to assign an issue to an existing user
app.MapPost("/issues/assign", (AssignIssue body, ICommandBus bus) => bus.InvokeAsync(body));

app.Run();

#endregion
