using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

public interface IInvoiceDocumentService
{
    InvoiceDocument BuildInvoice(Order order);
}
