/* * -----------------------------------------------------------------------------------------------------------------
 * ERROR VIEW MODEL - HATA YÖNETÝMÝ VE ÝZLENEBÝLÝRLÝK
 * -----------------------------------------------------------------------------------------------------------------
 * Bu model, uygulama çalýþma zamanýnda beklenmeyen bir hata oluþtuðunda devreye giren 
 * standart MVC yapýsýnýn bir parçasýdýr. Kullaným amacým þudur:
 * 
 * * 1. HATA TAKÝBÝ (TRACING): Hata oluþan isteðin benzersiz kimliðini (RequestId) yakalayarak 
 * View katmanýna taþýr. Bu sayede kullanýcýya "Hata Kodunuz: X" gibi bir bilgi vererek, 
 * arka plandaki loglarda sorunun tam kaynaðýný bulmamýzý saðlar.
 * 
 * * 2. UI KONTROLÜ: Kullanýcýya boþ veya null bir hata kodu göstermemek için, 'ShowRequestId' özelliði 
 * üzerinden arayüzde (HTML) koþullu gösterim saðlar.
 * -----------------------------------------------------------------------------------------------------------------
 */

namespace ClothingStore.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
