using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.Controllers;
using WeaponShop.Web.ViewModels.Account;

namespace WeaponShop.Tests.Controllers;

public class AccountControllerTests
{
    [Fact]
    public async Task Login_AdminUser_RedirectsToAdminOrders()
    {
        var user = new ApplicationUser
        {
            Id = "admin-1",
            UserName = "admin@weaponshop.local",
            Email = "admin@weaponshop.local"
        };

        var userManager = new TestUserManager(new[] { user })
        {
            PasswordCheck = (_, password) => password == "Admin123?"
        };
        var principalFactory = new TestClaimsPrincipalFactory(_ => ["Admin"]);
        var controller = CreateController(
            userManager,
            principalFactory,
            Path.GetTempPath());

        var result = await controller.Login(new LoginViewModel
        {
            Email = user.Email!,
            Password = "Admin123?"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Orders", redirect.ControllerName);
        Assert.Equal("Admin", redirect.RouteValues?["area"]);
    }

    [Fact]
    public async Task Documents_InvalidFile_DoesNotConsumeUploadSlot()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-1",
                UserName = "customer@weaponshop.local",
                Email = "customer@weaponshop.local",
                DocumentsUploadCount = 1
            };

            var userManager = new TestUserManager(new[] { user });
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Documents(new DocumentsViewModel
            {
                IdCardFile = CreateFormFile("idcard.exe", "invalid")
            }, CancellationToken.None);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Documents", view.ViewName);
            Assert.Equal(1, user.DocumentsUploadCount);
            Assert.Equal(0, userManager.UpdateCallCount);
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "UserDocuments", user.Id)));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Documents_UpdateFailure_DeletesSavedFilesAndRestoresUserState()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-2",
                UserName = "customer2@weaponshop.local",
                Email = "customer2@weaponshop.local",
                DocumentsUploadCount = 1
            };

            var userManager = new TestUserManager(new[] { user })
            {
                UpdateResult = IdentityResult.Failed(new IdentityError { Description = "Update failed." })
            };
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Documents(new DocumentsViewModel
            {
                IdCardFile = CreatePdfFormFile("idcard.pdf")
            }, CancellationToken.None);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Documents", view.ViewName);
            Assert.Equal(1, user.DocumentsUploadCount);
            Assert.Null(user.IdCardFileName);
            Assert.Equal(1, userManager.UpdateCallCount);

            var userDirectory = Path.Combine(tempRoot, "UserDocuments", user.Id);
            Assert.False(Directory.Exists(userDirectory) && Directory.EnumerateFiles(userDirectory).Any());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Documents_RemoveIdCard_DeletesStoredFileAndClearsDependentFlags()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-remove-id",
                UserName = "removeid@weaponshop.local",
                Email = "removeid@weaponshop.local",
                IdCardFileName = "idcard-existing.pdf",
                IdCardIssuedInCzechRepublic = true,
                FirearmsLicenseRecorded = true,
                DocumentsUploadCount = 2
            };

            var userDirectory = Path.Combine(tempRoot, "UserDocuments", user.Id);
            Directory.CreateDirectory(userDirectory);
            await File.WriteAllBytesAsync(Path.Combine(userDirectory, user.IdCardFileName), [1, 2, 3]);

            var userManager = new TestUserManager(new[] { user });
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Documents(new DocumentsViewModel
            {
                RemoveIdCard = true,
                IdCardIssuedInCzechRepublic = true,
                FirearmsLicenseRecorded = true
            }, CancellationToken.None);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Documents", redirect.ActionName);
            Assert.Null(user.IdCardFileName);
            Assert.False(user.IdCardIssuedInCzechRepublic);
            Assert.False(user.FirearmsLicenseRecorded);
            Assert.Equal(2, user.DocumentsUploadCount);
            Assert.Equal(1, userManager.UpdateCallCount);
            Assert.False(File.Exists(Path.Combine(userDirectory, "idcard-existing.pdf")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Documents_RemovePurchasePermit_DeletesStoredFileWithoutConsumingUploadSlot()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-remove-permit",
                UserName = "removepermit@weaponshop.local",
                Email = "removepermit@weaponshop.local",
                PurchasePermitFileName = "permit-existing.pdf",
                DocumentsUploadCount = 1
            };

            var userDirectory = Path.Combine(tempRoot, "UserDocuments", user.Id);
            Directory.CreateDirectory(userDirectory);
            await File.WriteAllBytesAsync(Path.Combine(userDirectory, user.PurchasePermitFileName), [1, 2, 3]);

            var userManager = new TestUserManager(new[] { user });
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Documents(new DocumentsViewModel
            {
                RemovePurchasePermit = true
            }, CancellationToken.None);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Documents", redirect.ActionName);
            Assert.Null(user.PurchasePermitFileName);
            Assert.Equal(1, user.DocumentsUploadCount);
            Assert.Equal(1, userManager.UpdateCallCount);
            Assert.False(File.Exists(Path.Combine(userDirectory, "permit-existing.pdf")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Register_UnderageUser_ReturnsViewAndDoesNotCreateAccount()
    {
        var userManager = new TestUserManager([]);
        var controller = CreateController(
            userManager,
            new TestClaimsPrincipalFactory(_ => Array.Empty<string>()),
            CreateTempDirectory());

        var result = await controller.Register(new RegisterViewModel
        {
            FirstName = "Young",
            LastName = "User",
            Email = "young@weaponshop.local",
            Password = "Password123?",
            ConfirmPassword = "Password123?",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17)
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
        Assert.Equal(0, userManager.CreateCallCount);
        Assert.Contains(controller.ModelState[nameof(RegisterViewModel.DateOfBirth)]!.Errors, error =>
            error.ErrorMessage.Contains("starší 18 let", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Profile_ValidUpdate_UpdatesCustomerAccountAndChangesPassword()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-3",
                UserName = "old@weaponshop.local",
                Email = "old@weaponshop.local",
                FirstName = "Old",
                LastName = "Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25)
            };

            var userManager = new TestUserManager(new[] { user })
            {
                PasswordCheck = (_, password) => password == "OldPass123?"
            };
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Profile(new ProfileViewModel
            {
                FirstName = "Jan",
                LastName = "Novak",
                Email = "jan.novak@weaponshop.local",
                DateOfBirth = user.DateOfBirth,
                CurrentPassword = "OldPass123?",
                NewPassword = "NewPass123?",
                ConfirmNewPassword = "NewPass123?"
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirect.ActionName);
            Assert.Equal("Jan", user.FirstName);
            Assert.Equal("Novak", user.LastName);
            Assert.Equal("jan.novak@weaponshop.local", user.Email);
            Assert.Equal("jan.novak@weaponshop.local", user.UserName);
            Assert.Equal(1, userManager.UpdateCallCount);
            Assert.Equal(1, userManager.ChangePasswordCallCount);
            Assert.Equal("OldPass123?", userManager.LastCurrentPassword);
            Assert.Equal("NewPass123?", userManager.LastNewPassword);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Profile_InvalidCurrentPassword_ReturnsViewAndDoesNotPersistChanges()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var user = new ApplicationUser
            {
                Id = "customer-4",
                UserName = "customer4@weaponshop.local",
                Email = "customer4@weaponshop.local",
                FirstName = "Customer",
                LastName = "Four",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-24)
            };

            var userManager = new TestUserManager(new[] { user })
            {
                PasswordCheck = (_, _) => false
            };
            var controller = CreateController(
                userManager,
                new TestClaimsPrincipalFactory(_ => ["Customer"]),
                tempRoot,
                CreatePrincipal(user, "Customer"));

            var result = await controller.Profile(new ProfileViewModel
            {
                FirstName = "Customer",
                LastName = "Four",
                Email = "updated@weaponshop.local",
                DateOfBirth = user.DateOfBirth,
                CurrentPassword = "WrongPass123?",
                NewPassword = "NewPass123?",
                ConfirmNewPassword = "NewPass123?"
            });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Null(view.ViewName);
            Assert.Equal(0, userManager.UpdateCallCount);
            Assert.Equal(0, userManager.ChangePasswordCallCount);
            Assert.Contains(controller.ModelState[nameof(ProfileViewModel.CurrentPassword)]!.Errors, error =>
                error.ErrorMessage.Contains("není správné", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static AccountController CreateController(
        TestUserManager userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> principalFactory,
        string contentRootPath,
        ClaimsPrincipal? currentUser = null)
    {
        var authenticationService = new TestAuthenticationService();
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authenticationService)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = currentUser ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        var controller = new AccountController(
            userManager,
            principalFactory,
            new TestWebHostEnvironment(contentRootPath),
            DataProtectionProvider.Create(new DirectoryInfo(contentRootPath)),
            new TestNotificationRepository())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
        controller.Url = new TestUrlHelper(controller.ControllerContext);

        return controller;
    }

    private static ClaimsPrincipal CreatePrincipal(ApplicationUser user, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private static IFormFile CreatePdfFormFile(string fileName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test document");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"weaponshop-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
