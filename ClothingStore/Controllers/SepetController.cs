/* =================================================================================================================
 * DOSYA ADI: SepetController.cs
 * AMACI: Alışveriş sepeti işlemlerini, dinamik fiyat hesaplamalarını, stok kontrolünü ve 
 * nihai satın alma/sipariş süreçlerini yönetir. Müşteri alışveriş döngüsünün tamamlandığı ana merkezdir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - ASP.NET Core MVC & Session: Kullanıcının sepet sayısını ve kimlik bilgisini oturum bazlı takip etme.
 * - Entity Framework Core (Eager Loading): .Include() ve .ThenInclude() yapıları ile Sepet -> Ürün -> Beden 
 * ve Stok hiyerarşisinin çok seviyeli olarak veritabanından çekilmesi.
 * - LINQ: Ara toplam, indirim tutarı ve genel toplam gibi matematiksel hesaplamaların sunucu tarafında işlenmesi.
 * - State Management: Önbellekleme (ResponseCache) yönetimi ile sepet verilerinin anlık doğruluğunun korunması.
 * * ÖNE ÇIKAN İŞLEVLER:
 * 1. Index: Sepetteki ürünleri, uygulanan %20'lik kampanya indirimi ve güncel stok durumlarıyla birlikte listeler.
 * 2. Ekle: Seçilen ürün ve beden ikilisi için stok doğrulaması yapar; ürün sepette varsa adedi artırır, yoksa yeni kayıt oluşturur.
 * 3. SatinAl (Post): Sipariş kaydını oluşturur, sepet kalemlerini 'Sipariş Detayları' tablosuna aktarır ve 
 * en kritik işlem olarak ürünün seçilen bedenine ait stok adedini veritabanında otomatik olarak düşürür.
 * 4. SepetSayisiniGuncelle: Layout (arayüz) üzerindeki sepet ikonunun her işlem sonrası (ekleme/silme/güncelleme) 
 * asenkron olarak güncellenmesini sağlar.
 * 5. Beden & Adet Guncelle: Kullanıcının sepet sayfasından ayrılmadan tercihlerini değiştirebilmesine olanak tanır.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class SepetController : Controller
    {
        ClothingStoreDbContext db = new ClothingStoreDbContext();

        public IActionResult Index()
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");

            if (kullaniciId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var sepetim = db.Sepets
                .Include(s => s.Beden)
                .Include(s => s.Urun)
                    .ThenInclude(u => u.UrunBedenStok)
                        .ThenInclude(ubs => ubs.Beden)
                .Where(s => s.KullaniciId == kullaniciId)
                .ToList();

            decimal araToplam = sepetim.Sum(x => (x.Urun?.Fiyat ?? 0) * (x.Adet ?? 1));
            decimal indirimTutari = araToplam * 0.20m;
            decimal genelToplam = araToplam - indirimTutari;

            ViewBag.AraToplam = araToplam;
            ViewBag.IndirimTutari = indirimTutari;
            ViewBag.GenelToplam = genelToplam;

            return View(sepetim);
        }

        public IActionResult Ekle(int UrunId, int? BedenId)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login", "Account");

            if (BedenId == null || BedenId == 0)
            {
                return Content("HATA: URL veya Form üzerinden 'BedenId' değeri gelmiyor. HTML'deki name='BedenId' kısmını kontrol edin.");
            }

            var stokKontrol = db.UrunBedenStok.FirstOrDefault(x => x.UrunId == UrunId && x.BedenId == BedenId);

            if (stokKontrol == null)
            {
                return Content($"HATA: {UrunId} ID'li ürünün stoklarında {BedenId} ID'li bir beden tanımlı değil! Veritabanı tutarsızlığı olabilir.");
            }

            var sepetUrun = db.Sepets.FirstOrDefault(s =>
                s.KullaniciId == kullaniciId &&
                s.UrunId == UrunId &&
                s.BedenId == BedenId);

            if (sepetUrun != null)
            {
                sepetUrun.Adet++;
            }
            else
            {
                var yeniSepet = new Sepet
                {
                    KullaniciId = (int)kullaniciId,
                    UrunId = UrunId,
                    Adet = 1,
                    BedenId = BedenId 
                };
                db.Sepets.Add(yeniSepet);
            }

            db.SaveChanges();
            SepetSayisiniGuncelle((int)kullaniciId);

            // Başarılıysa sepete yönlendir
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult BedenGuncelle(int sepetId, int yeniBedenId)
        {
            var urun = db.Sepets.Find(sepetId);
            if (urun != null)
            {
                urun.BedenId = yeniBedenId;
                db.SaveChanges();
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        public IActionResult AdetGuncelle(int sepetId, int adet)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return Unauthorized();

            var urun = db.Sepets.Find(sepetId);

            if (urun != null && urun.KullaniciId == kullaniciId)
            {
                urun.Adet = adet;
                db.SaveChanges();
                SepetSayisiniGuncelle((int)kullaniciId);
                return Ok();
            }

            return BadRequest();
        }

        public IActionResult Sil(int id)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            var urun = db.Sepets.Find(id);
            if (urun != null)
            {
                db.Sepets.Remove(urun);
                db.SaveChanges();
                if (kullaniciId != null) SepetSayisiniGuncelle((int)kullaniciId);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult SatinAl()
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login", "Account");

            var sepet = db.Sepets.Include(s => s.Urun).Where(x => x.KullaniciId == kullaniciId).ToList();
            if (sepet.Count == 0) return RedirectToAction("Index");

            decimal araToplam = sepet.Sum(x => (x.Urun.Fiyat) * (x.Adet ?? 1));
            decimal indirim = araToplam * 0.20m;
            decimal odenecekTutar = araToplam - indirim;

            ViewBag.Kargolar = db.KargoFirmalaris.ToList();
            ViewBag.OdenecekTutar = odenecekTutar;
            ViewBag.Indirim = indirim;

            var kullanici = db.Kullanicilars.Find(kullaniciId);
            ViewBag.KayitliAdres = kullanici?.Adres;

            return View();
        }

        [HttpPost]
        public IActionResult SatinAl(string teslimatAdresi, int kargoId, string odemeTipi)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login", "Account");

            var sepet = db.Sepets.Include(s => s.Urun).Where(x => x.KullaniciId == kullaniciId).ToList();
            if (sepet.Count == 0) return RedirectToAction("Index");

            var kullanici = db.Kullanicilars.Find(kullaniciId);
            if (kullanici != null && string.IsNullOrEmpty(kullanici.Adres))
            {
                kullanici.Adres = teslimatAdresi;
                db.SaveChanges();
            }

            Siparisler yeniSiparis = new Siparisler
            {
                KullaniciId = (int)kullaniciId,
                Tarih = DateTime.Now,
                Durum = "Sipariş Alındı",
                TeslimatAdresi = teslimatAdresi,
                KargoId = kargoId,
                OdemeTipi = odemeTipi
            };

            decimal toplam = sepet.Sum(x => (x.Urun.Fiyat) * (x.Adet ?? 1));
            yeniSiparis.ToplamTutar = toplam * 0.80m;

            db.Siparislers.Add(yeniSiparis);
            db.SaveChanges(); 
            foreach (var item in sepet)
            {
                int alinanAdet = item.Adet ?? 1;

                SiparisDetaylari detay = new SiparisDetaylari
                {
                    SiparisId = yeniSiparis.SiparisId,
                    UrunId = item.UrunId,
                    Adet = alinanAdet,
                    BirimFiyat = item.Urun.Fiyat * 0.80m,
                    BedenId = item.BedenId
                };
                db.SiparisDetaylaris.Add(detay);

                var bedenStok = db.UrunBedenStok.FirstOrDefault(x => x.UrunId == item.UrunId && x.BedenId == item.BedenId);
                if (bedenStok != null && bedenStok.StokAdedi >= alinanAdet)
                {
                    bedenStok.StokAdedi -= alinanAdet;
                }

            }

            db.Sepets.RemoveRange(sepet);
            HttpContext.Session.SetInt32("SepetAdet", 0);

            db.SaveChanges();

            return RedirectToAction("SiparisTamamlandi");
        }

        public IActionResult SiparisTamamlandi() => View();

        private void SepetSayisiniGuncelle(int kullaniciId)
        {
            var toplamAdet = db.Sepets.Where(x => x.KullaniciId == kullaniciId).Sum(x => x.Adet);
            HttpContext.Session.SetInt32("SepetAdet", (int)(toplamAdet ?? 0));
        }
    }
}