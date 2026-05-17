/* =================================================================================================================
 * DOSYA ADI: HomeController.cs
 * AMACI: Uygulamanýn ana sayfa dinamiklerini, arama motoru yönlendirmelerini ve akýllý kombin 
 * algoritmalarýný yönetir. Kullanýcýya mevsimsel ve kiþiselleþtirilmiþ içerikler sunan ana merkezdir.
 * * KULLANILAN TEKNOLOJÝLER VE KÜTÜPHANELER:
 * - AnasayfaViewModel & KombinViewModel: Birden fazla veri tablosunu (Ürün, Kategori, Marka) 
 * tek bir görünümde birleþtiren kompleks model yapýlarý.
 * - LINQ (Language Integrated Query): Veritabaný üzerinde rastgele veri çekme (Guid.NewGuid()) 
 * ve mevsimsel filtreleme operasyonlarý.
 * - Dinamik Algoritmalar: Mevcut tarihe göre mevsim tespiti yapan ve buna uygun ürün gruplarýný 
 * (Üst, Alt, Dýþ Giyim) renk uyumuyla eþleþtiren akýllý kombin motoru.
 * - Try-Catch Hata Yönetimi: Ana sayfa yüklenirken oluþabilecek veritabaný baðlantý hatalarýnýn 
 * yakalanmasý ve kullanýcýya kontrollü hata mesajý sunulmasý.
 * * ÖNE ÇIKAN ÝÞLEVLER:
 * 1. Index: Yeni gelen ürünleri tarihe göre, çok satanlarý stok durumuna göre filtreleyerek ana sayfaya taþýr.
 * 2. GununKombini: Sistemin en özgün parçasýdýr; ay bazlý mevsim tespiti yaparak üst, alt ve ayakkabý 
 * gruplarýný joker renkler (Siyah, Beyaz, Gri) dengesiyle otomatik eþleþtirir.
 * 3. SansiniDene: Veritabanýndaki ürün ID'leri arasýndan rastgele seçim yaparak kullanýcýyý sürpriz bir ürün detayýna yönlendirir.
 * 4. Ara: Arama çubuðundan gelen anahtar kelimeleri UrunController üzerindeki arama motoruna parametre olarak iletir.
 * 5. Resimleri Tamir Et: Veri giriþindeki eksiklikleri gidermek için ürün isimlerini analiz eder ve 
 * uygun Unsplash API görsellerini toplu olarak veritabanýna iþler.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        ClothingStoreDbContext db = new ClothingStoreDbContext();

        public IActionResult Index()
        {
            try
            {
                AnasayfaViewModel model = new AnasayfaViewModel();

                model.YeniGelenler = db.Urunlers.OrderByDescending(x => x.EklenmeTarihi).Take(10).ToList();
                model.CokSatanlar = db.Urunlers.OrderBy(x => x.StokAdedi).Take(10).ToList();
                model.Kategoriler = db.Kategorilers.ToList();

                model.Markalar = db.Markalar.ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                return Content("Hata oluþtu: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult Ara(string kelime)
        {
            return RedirectToAction("TumUrunler", "Urun", new { aranacakKelime = kelime });
        }

        public IActionResult SansiniDene()
        {
            var idler = db.Urunlers.Select(k => k.UrunId).ToList();

            if (idler.Count > 0)
            {
                Random rnd = new Random();
                int id = idler[rnd.Next(idler.Count)];

                return RedirectToAction("Detay", "Urun", new { id = id });
            }
            return RedirectToAction("Index");
        }

        public IActionResult GununKombini()
        {
            int ay = DateTime.Now.Month;
            string aktifMevsim = (ay >= 3 && ay <= 5) ? "Ýlkbahar" :
                                 (ay >= 6 && ay <= 8) ? "Yaz" :
                                 (ay >= 9 && ay <= 11) ? "Sonbahar" : "Kýþ";

            var ustGiyim = db.Urunlers
                .Where(u => u.KategoriId != null && u.KategoriId.Contains("1") && u.Mevsim != null && u.Mevsim.Contains(aktifMevsim))
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefault();

            if (ustGiyim == null)
            {
                ViewBag.Hata = $"{aktifMevsim} mevsimine uygun Üst Giyim bulunamadý.";
                return View(new KombinViewModel());
            }

            string[] jokerRenkler = { "Beyaz", "Siyah", "Gri" };
            string arananRenk = (ustGiyim.Renk != null && jokerRenkler.Contains(ustGiyim.Renk)) ? null : ustGiyim.Renk;

            var altGiyimQuery = db.Urunlers
                .Where(u => u.KategoriId != null && u.KategoriId.Contains("2") && u.Mevsim != null && u.Mevsim.Contains(aktifMevsim));

            if (arananRenk != null)
                altGiyimQuery = altGiyimQuery.Where(u => u.Renk == arananRenk);

            var altGiyim = altGiyimQuery.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

            var ayakkabiQuery = db.Urunlers
                .Where(u => u.KategoriId != null && u.KategoriId.Contains("4") && u.Mevsim != null && u.Mevsim.Contains(aktifMevsim));

            if (arananRenk != null)
                ayakkabiQuery = ayakkabiQuery.Where(u => u.Renk == arananRenk);

            var ayakkabi = ayakkabiQuery.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

            Urunler disGiyim = null;
            if (aktifMevsim != "Yaz")
            {
                var disGiyimQuery = db.Urunlers
                    .Where(u => u.KategoriId != null && u.KategoriId.Contains("3") && u.Mevsim != null && u.Mevsim.Contains(aktifMevsim));

                if (arananRenk != null)
                    disGiyimQuery = disGiyimQuery.Where(u => u.Renk == arananRenk);

                disGiyim = disGiyimQuery.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            }

            var kombinModel = new KombinViewModel
            {
                Mevsim = aktifMevsim,
                UstGiyim = ustGiyim,
                AltGiyim = altGiyim,
                Ayakkabi = ayakkabi,
                DisGiyim = disGiyim
            };

            return View(kombinModel);
        }

        public IActionResult ResimleriTamirEt()
        {
            var Urunler = db.Urunlers.ToList();
            int sayac = 0;

            foreach (var k in Urunler)
            {
                string ad = k.UrunAdi?.ToLower().Trim() ?? "";
                string yeniResim = "";

                if (ad.Contains("tiþört") || ad.Contains("t-shirt") || ad.Contains("gömlek"))
                {
                    yeniResim = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=600&q=80";
                }
                else if (ad.Contains("kaban") || ad.Contains("mont") || ad.Contains("ceket"))
                {
                    yeniResim = "https://images.unsplash.com/photo-1539533113208-f6df8cc8b543?auto=format&fit=crop&w=600&q=80";
                }
                else if (ad.Contains("pantolon") || ad.Contains("jean") || ad.Contains("etek"))
                {
                    yeniResim = "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=600&q=80";
                }
                else if (ad.Contains("kazak") || ad.Contains("hýrka"))
                {
                    yeniResim = "https://images.unsplash.com/photo-1620799140188-3b2a02fd9a77?auto=format&fit=crop&w=600&q=80";
                }
                else if (ad.Contains("ayakkabý") || ad.Contains("sneaker"))
                {
                    yeniResim = "https://images.unsplash.com/photo-1549298916-b41d501d3772?auto=format&fit=crop&w=600&q=80";
                }

                if (!string.IsNullOrEmpty(yeniResim))
                {
                    k.ResimYolu = yeniResim;
                    sayac++;
                }
                else if (string.IsNullOrEmpty(k.ResimYolu) || !k.ResimYolu.StartsWith("http"))
                {
                    k.ResimYolu = "https://upload.wikimedia.org/wikipedia/commons/1/14/No_Image_Available.jpg";
                }
            }

            db.SaveChanges();
            return Content($"MÜKEMMEL! Tam {sayac} adet ürünün resmi güncellendi.");
        }
    }

    public class UrunVerisi
    {
        public string Cat { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public string Aciklama { get; set; }
        public string Img { get; set; }
        public string Renk { get; set; }
        public string Beden { get; set; }
    }

    public class KombinViewModel
    {
        public string Mevsim { get; set; }
        public Urunler UstGiyim { get; set; }
        public Urunler AltGiyim { get; set; }
        public Urunler Ayakkabi { get; set; }
        public Urunler DisGiyim { get; set; }
    }
}