namespace WeaponShop.Domain;

public enum OrderStatus
{
    Created,
    AwaitingApproval,
    Approved,
    Rejected,
    Completed
}
