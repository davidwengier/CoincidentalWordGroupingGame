using CoincidentalWordGroupingGame;
using CoincidentalWordGroupingGame.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(PuzzleCatalog.LoadFromAssembly(typeof(Program).Assembly));

await builder.Build().RunAsync();
