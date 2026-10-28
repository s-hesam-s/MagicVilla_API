using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using MagicVilla_VillaAPI;
using MagicVilla_VillaAPI.Controllers.V1;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Authorization.Tests;

public class ErrorResponseTests
{
    [Fact]
    public async Task VillaFailureReturnsGeneric500WithoutInternalDetails()
    {
        var repository = new Mock<IVillaRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Villa,bool>>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("fixture-private-database-and-stack-detail"));
        var controller = new VillaAPIController(repository.Object, new MappingConfig());
        var response = await controller.GetVillas(null, null);
        assertGeneric(response.Result);
    }

    [Fact]
    public async Task VillaNumberFailureReturnsGeneric500WithoutInternalDetails()
    {
        var repository = new Mock<IVillaNumberRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<VillaNumber,bool>>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("fixture-private-database-and-stack-detail"));
        var controller = new VillaNumberAPIController(repository.Object, new Mock<IVillaRepository>().Object, new MappingConfig());
        var response = await controller.GetVillaNumbers();
        assertGeneric(response.Result);
    }

    private static void assertGeneric(IActionResult result)
    {
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        var body = Assert.IsType<APIResponse>(error.Value);
        Assert.False(body.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, body.StatusCode);
        Assert.Equal("The request could not be completed.", Assert.Single(body.ErrorMessages));
        Assert.DoesNotContain("fixture-private", JsonSerializer.Serialize(body));
        Assert.DoesNotContain("StackTrace", JsonSerializer.Serialize(body));
    }
}
