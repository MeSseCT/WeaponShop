using Microsoft.AspNetCore.Components;
using WeaponShop.Domain;

namespace WeaponShop.Web.Pages.Weapons;

/// <summary>
/// Shared code-behind base class for weapon create/edit forms.
/// Keeps the Razor markup clean and reusable.
/// </summary>
public class WeaponFormBase : ComponentBase
{
    [Parameter]
    public Weapon Weapon { get; set; } = new();

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string SubmitButtonText { get; set; } = "Save";

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected Task HandleValidSubmit() => OnValidSubmit.InvokeAsync();
    protected Task Cancel() => OnCancel.InvokeAsync();
}

