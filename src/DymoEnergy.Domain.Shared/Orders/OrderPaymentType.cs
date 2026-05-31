namespace DymoEnergy.Orders;

public enum OrderPaymentType
{
    Cash            = 1,
    CreditCard      = 2,
    DebitCard       = 3,
    BankTransfer    = 4,
    MobileBanking   = 5,
    Cheque          = 6,
    Online          = 7,
    CashOnDelivery  = 8,
    Other           = 9,
}
