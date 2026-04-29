using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Tests;

internal sealed class TestUserManager : UserManager<ApplicationUser>
{
    private readonly Dictionary<string, ApplicationUser> _usersById;
    private readonly Dictionary<string, ApplicationUser> _usersByEmail;

    public TestUserManager(IEnumerable<ApplicationUser> users)
        : base(
            new NullUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            LoggerFactory.Create(builder => { }).CreateLogger<UserManager<ApplicationUser>>())
    {
        var userList = users.ToList();
        _usersById = userList.ToDictionary(user => user.Id, StringComparer.Ordinal);
        _usersByEmail = userList
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .ToDictionary(user => user.Email!, StringComparer.OrdinalIgnoreCase);
    }

    public Func<ApplicationUser, string, bool> PasswordCheck { get; set; } = (_, _) => true;
    public IdentityResult UpdateResult { get; set; } = IdentityResult.Success;
    public IdentityResult CreateResult { get; set; } = IdentityResult.Success;
    public IdentityResult ChangePasswordResult { get; set; } = IdentityResult.Success;
    public int UpdateCallCount { get; private set; }
    public int CreateCallCount { get; private set; }
    public int ChangePasswordCallCount { get; private set; }
    public List<string> AddedRoles { get; } = [];
    public string? LastCurrentPassword { get; private set; }
    public string? LastNewPassword { get; private set; }

    public override Task<ApplicationUser?> FindByEmailAsync(string email)
    {
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public override Task<ApplicationUser?> FindByIdAsync(string userId)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public override Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId)
            ? Task.FromResult<ApplicationUser?>(null)
            : FindByIdAsync(userId);
    }

    public override Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return Task.FromResult(PasswordCheck(user, password));
    }

    public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
    {
        UpdateCallCount++;
        _usersById[user.Id] = user;
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            _usersByEmail[user.Email] = user;
        }

        return Task.FromResult(UpdateResult);
    }

    public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        CreateCallCount++;
        if (string.IsNullOrWhiteSpace(user.Id))
        {
            user.Id = Guid.NewGuid().ToString("N");
        }

        _usersById[user.Id] = user;
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            _usersByEmail[user.Email] = user;
        }

        return Task.FromResult(CreateResult);
    }

    public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
    {
        AddedRoles.Add(role);
        return Task.FromResult(IdentityResult.Success);
    }

    public override Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
    {
        ChangePasswordCallCount++;
        LastCurrentPassword = currentPassword;
        LastNewPassword = newPassword;
        return Task.FromResult(ChangePasswordResult);
    }
}

internal sealed class TestClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly Func<ApplicationUser, IEnumerable<string>> _rolesFactory;

    public TestClaimsPrincipalFactory(Func<ApplicationUser, IEnumerable<string>> rolesFactory)
    {
        _rolesFactory = rolesFactory;
    }

    public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };

        claims.AddRange(_rolesFactory(user).Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, "Test");
        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}

internal sealed class TestAuthenticationService : IAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
}

internal sealed class TestTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}

internal sealed class TestUrlHelper : IUrlHelper
{
    public TestUrlHelper(ActionContext actionContext)
    {
        ActionContext = actionContext;
    }

    public ActionContext ActionContext { get; }

    public string? Action(UrlActionContext actionContext) => "/test-action";

    public string? Content(string? contentPath) => contentPath;

    public bool IsLocalUrl(string? url) => true;

    public string? Link(string? routeName, object? values) => "/test-link";

    public string? RouteUrl(UrlRouteContext routeContext) => "/test-route";
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        WebRootPath = contentRootPath;
        ContentRootFileProvider = new NullFileProvider();
        WebRootFileProvider = new NullFileProvider();
    }

    public string ApplicationName { get; set; } = "WeaponShop.Tests";
    public IFileProvider WebRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}

internal sealed class TestNotificationRepository : INotificationRepository
{
    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<List<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<Notification>());
}

internal sealed class TestOrderService : IOrderService
{
    public Order? CurrentOrder { get; set; }
    public Order? OrderById { get; set; }
    public List<Order> SubmittedOrders { get; } = [];
    public int? AddedAccessoryId { get; private set; }
    public int? AddedQuantity { get; private set; }
    public int? RemovedAccessoryId { get; private set; }
    public string? LastUserId { get; private set; }
    public bool CheckoutCalled { get; private set; }
    public CheckoutDetails? LastCheckoutDetails { get; private set; }

    public Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default) => Task.FromResult(OrderById);
    public Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        LastUserId = userId;
        return Task.FromResult(CurrentOrder);
    }

    public Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(SubmittedOrders);

    public Task<List<Order>> GetUserHistoryAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<OrderAudit>> GetAuditsByActorAsync(string actorUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<Order>> GetAwaitingApprovalOrdersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<Order>> GetWarehouseOrdersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<Order>> GetGunsmithOrdersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> AddItemAsync(int orderId, int weaponId, int quantity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> AddItemToCurrentOrderAsync(string userId, int weaponId, int quantity, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Order> AddAccessoryItemToCurrentOrderAsync(string userId, int accessoryId, int quantity, CancellationToken cancellationToken = default)
    {
        LastUserId = userId;
        AddedAccessoryId = accessoryId;
        AddedQuantity = quantity;
        return Task.FromResult(CurrentOrder ?? new Order());
    }

    public Task<Order> RemoveItemFromCurrentOrderAsync(string userId, int weaponId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Order> RemoveAccessoryItemFromCurrentOrderAsync(string userId, int accessoryId, CancellationToken cancellationToken = default)
    {
        LastUserId = userId;
        RemovedAccessoryId = accessoryId;
        return Task.FromResult(CurrentOrder ?? new Order());
    }

    public Task<Order> CheckoutCurrentOrderAsync(string userId, CheckoutDetails checkoutDetails, CancellationToken cancellationToken = default)
    {
        CheckoutCalled = true;
        LastUserId = userId;
        LastCheckoutDetails = checkoutDetails;
        return Task.FromResult(CurrentOrder ?? new Order());
    }

    public Task<Order> ApproveOrderAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> RejectOrderAsync(int orderId, string actorUserId, string? actorName, string? actorRole, string? reason, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> AssignWeaponUnitsAsync(int orderId, int weaponId, IReadOnlyCollection<int> unitIds, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> MarkWarehouseCheckedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> MarkGunsmithCheckedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> MarkShippedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> MarkReadyForPickupAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> MarkPickupHandedOverAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> RecalculateTotalPriceAsync(int orderId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Order> ChangeStatusAsync(int orderId, OrderStatus status, string actorUserId, string? actorName, string? actorRole, string? notes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class TestInvoiceDocumentService : IInvoiceDocumentService
{
    public InvoiceDocument Document { get; set; } = new()
    {
        HtmlContent = "<html></html>",
        PdfContent = Array.Empty<byte>(),
        PdfFileName = "invoice.pdf"
    };

    public InvoiceDocument BuildInvoice(Order order) => Document;
}

internal sealed class NullUserStore : IUserStore<ApplicationUser>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult<ApplicationUser?>(null);

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult<ApplicationUser?>(null);
}
