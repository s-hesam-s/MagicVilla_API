using System.Linq.Expressions;
using System.Reflection;
using Asp.Versioning;
using MagicVilla_VillaAPI;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Moq;

var root = PreviewSetup.Root("MagicVilla_VillaAPI"); var builder = PreviewSetup.Builder(root, 8843);
var inner = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" }); inner.Configuration.Sources.Clear(); inner.Logging.ClearProviders(); inner.WebHost.UseTestServer();
var rows = new List<Villa> { new() { Id = 1, Name = "Sample Garden Villa", Details = "Fixture response from the existing controller", Rate = 180, Occupancy = 4, Sqft = 650 } };
var repo = new Mock<IVillaRepository>(MockBehavior.Strict);
repo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Villa, bool>>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(rows);
repo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Villa, bool>>>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync((Expression<Func<Villa, bool>> filter, bool tracked, string includes) => { if (filter.Compile()(new Villa { Id = 99 })) throw new InvalidOperationException("Fixture repository failure"); return rows.SingleOrDefault(filter.Compile())!; });
inner.Services.AddSingleton(repo.Object); inner.Services.AddSingleton<MappingConfig>();
inner.Services.AddControllers(o => o.CacheProfiles.Add("Default30", new CacheProfile { Duration = 30 })).AddApplicationPart(typeof(MagicVilla_VillaAPI.Controllers.V1.VillaAPIController).Assembly).ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new PreviewControllers()));
inner.Services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ApiVersionReader = new UrlSegmentApiVersionReader(); }).AddMvc();
await using var fixture = inner.Build(); fixture.Use(async (c, next) => { if (!HttpMethods.IsGet(c.Request.Method)) { c.Response.StatusCode = 405; return; } await next(); }); fixture.MapControllers(); await fixture.StartAsync();
var client = fixture.GetTestClient(); var evidence = new List<object>();
foreach (var id in new[] { 1, 0, 55, 99 }) { var path = $"/api/v1/VillaAPI/{id}"; using var response = await client.GetAsync(path); evidence.Add(new { method = "GET", path, status = (int)response.StatusCode, body = await response.Content.ReadAsStringAsync() }); }
var app = builder.Build(); PreviewSetup.Guard(app); app.MapGet("/evidence.json", () => evidence); app.MapGet("/", () => Results.Content(EvidencePage.Html("MagicVilla API · response boundaries", "The original versioned Villa controller runs in an in-process HTTP host with a strict sample repository. Valid, invalid, missing and repository-failure cases are observed without SQL or the original host.", evidence), "text/html")); await app.RunAsync();

public sealed class PreviewControllers : Microsoft.AspNetCore.Mvc.ApplicationParts.IApplicationFeatureProvider<Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature>
{ public void PopulateFeature(IEnumerable<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart> parts, Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature feature) { feature.Controllers.Clear(); feature.Controllers.Add(typeof(MagicVilla_VillaAPI.Controllers.V1.VillaAPIController).GetTypeInfo()); } }
