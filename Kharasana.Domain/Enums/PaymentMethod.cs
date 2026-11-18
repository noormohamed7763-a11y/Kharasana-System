namespace Kharasana.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    KareemiTransfer = 2, // كريمي تحويل
    BankDeposit = 3, // إيداع بنكي
    MobileMoney = 4, // حوالة موبايل (محافظ إلكترونية)
    Cheque = 5,
    Other = 6
}
