using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;
using WeaponShop.Web.Controllers;

namespace WeaponShop.Tests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task Details_OrderOwnedByAnotherUser_ReturnsForbid()
    {
        var controller = CreateController(
            new TestOrderService
            {
                OrderById = new Order
                {
                    Id = 11,
                    UserId = "different-user"
                }
            },
            userId: "customer-1");

        var result = await controller.Details(11, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ViewInvoice_OwnOrder_ReturnsHtmlContent()
    {
        var order = new Order
        {
            Id = 12,
            UserId = "customer-1",
            OrderNumber = "WS-20260425-000012"
        };
        var controller = CreateController(
            new TestOrderService
            {
                OrderById = order
            },
            userId: "customer-1",
            invoiceDocumentService: new TestInvoiceDocumentService
            {
                Document = new InvoiceDocument
                {
                    HtmlContent = "<h1>Invoice</h1>",
                    PdfContent = [0x25, 0x50, 0x44, 0x46],
                    PdfFileName = "invoice.pdf"
                }
            });

        var result = await controller.ViewInvoice(12, CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", content.ContentType);
        Assert.Equal("<h1>Invoice</h1>", content.Content);
    }

    [Fact]
    public async Task DownloadInvoice_OwnOrder_ReturnsPdfFile()
    {
        var order = new Order
        {
            Id = 13,
            UserId = "customer-1",
            OrderNumber = "WS-20260425-000013"
        };
        var controller = CreateController(
            new TestOrderService
            {
                OrderById = order
            },
            userId: "customer-1",
            invoiceDocumentService: new TestInvoiceDocumentService
            {
                Document = new InvoiceDocument
                {
                    HtmlContent = "<h1>Invoice</h1>",
                    PdfContent = [1, 2, 3],
                    PdfFileName = "invoice-13.pdf"
                }
            });

        var result = await controller.DownloadInvoice(13, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("invoice-13.pdf", file.FileDownloadName);
        Assert.Equal([1, 2, 3], file.FileContents);
    }

    private static OrdersController CreateController(
        TestOrderService orderService,
        string userId,
        IInvoiceDocumentService? invoiceDocumentService = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Customer")
            ], "Test"))
        };

        return new OrdersController(orderService, invoiceDocumentService ?? new TestInvoiceDocumentService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }
}
