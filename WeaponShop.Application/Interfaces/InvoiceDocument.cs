namespace WeaponShop.Application.Interfaces;

public sealed class InvoiceDocument
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string HtmlFileName { get; init; } = string.Empty;
    public string HtmlContent { get; init; } = string.Empty;
    public string PdfFileName { get; init; } = string.Empty;
    public byte[] PdfContent { get; init; } = Array.Empty<byte>();
}
