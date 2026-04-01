namespace WeaponShop.Domain;

public enum OrderStatus
{
    Created = 0,
    AwaitingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4,
    AwaitingGunsmith = 5,
    AwaitingDispatch = 6,
    Shipped = 7,
    ReadyForPickup = 8
}
