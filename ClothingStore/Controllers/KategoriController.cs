/* =================================================================================================================
 * DOSYA ADI: KategoriController.cs
 * AMACI: Platformdaki ürün gruplandırmalarını (Kategorileri) yönetir. Mağaza vitrininde ürünlerin 
 * mantıksal bir hiyerarşi içinde sunulmasını ve alfabetik olarak listelenmesini sağlar.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - ASP.NET Core MVC: Kategori verilerinin görünümlere (View) aktarılması ve yönlendirilmesi.
 * - Entity Framework Core: Veritabanı modeline erişim ve veri çekme işlemleri.
 * - LINQ (Language Integrated Query): Kategorilerin sunucu tarafında alfabetik (A'dan Z'ye) sıralanması.
 * - Caching (ResponseCache): Kategoriler gibi sık değişebilen verilerin tarayıcıda hatalı önbelleğe 
 * alınmasını engelleyerek kullanıcıya her zaman en güncel listeyi sunma (NoStore/None).
 * * ÖNE ÇIKAN İŞLEVLER:
 * 1. TumKategoriler: Veritabanındaki tüm kategorileri "Kategori Adı" parametresine göre alfabetik olarak 
 * sıralar ve listeleme sayfasına model olarak gönderir.
 * 2. Dinamik Listeleme: Veritabanı bağlantısı üzerinden asenkron olmayan, hızlı veri çekme (ToList) operasyonu gerçekleştirir.
 * 3. Bellek Yönetimi: Önbellekleme öznitelikleri ile istemci tarafında veri tutarlılığını garanti altına alır.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class KategoriController : Controller
    {
        ClothingStoreDbContext db = new ClothingStoreDbContext();
        public IActionResult TumKategoriler()
        {
            var kategoriler = db.Kategorilers.OrderBy(x => x.KategoriAdi).ToList();
            return View(kategoriler);
        }
    }
}