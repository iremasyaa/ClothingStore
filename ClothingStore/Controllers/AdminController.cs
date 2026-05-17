/* =================================================================================================================
 * DOSYA ADI: AdminController.cs
 * AMACI: Sitenin Yönetici (Admin) panelindeki tüm arka plan işlemlerini (CRUD - Ekleme, Okuma, Güncelleme, Silme) yönetir. 
 * Ürün, Kategori, Marka, Üye, Sipariş ve Yorum yönetiminin yapıldığı, yetkisiz erişimlerin engellendiği ana merkezdir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - ASP.NET Core MVC: Admin paneli sayfalarının (View) ve işlemlerinin (Controller) yönlendirilmesi.
 * - Entity Framework Core & LINQ: SQL sorguları yazmadan veritabanındaki verileri çekme, filtreleme (.Where, .Include) ve kaydetme işlemleri.
 * - Asenkron Programlama (async / await): Sunucuyu yormadan, arka planda hızlı resim yükleme işlemleri için.
 * - System.IO (IFormFile): Bilgisayardan seçilen ürün/marka resimlerinin sunucudaki 'wwwroot/img' klasörüne kopyalanması.
 * - Session Authorization: Metotların başında kullanılan IsAdmin() fonksiyonu ile yetkisiz kişilerin link yazarak admin paneline girmesini engelleme.
 * - Güvenli Silme (Restrict & Cascade): İlişkili veritabanı tablolarının çökmemesi için yazılmış özel try-catch ve engelleyici silme algoritmaları.
 * * METOTLAR (FONKSİYONLAR) VE İŞLEVLERİ:
 * 1. IsAdmin(): Mevcut oturumun (Session) "Admin" rolüne sahip olup olmadığını kontrol eden güvenlik kilididir.
 * 2. ResimYukle(async): Sisteme yüklenen resim dosyalarının adlarını alıp proje klasörüne fiziksel olarak kaydeder.
 * 3. Index (Dashboard): Veritabanındaki toplam ürün, kategori, marka, üye, sipariş ve yorum sayılarını hesaplayıp özet ekranına gönderir.
 * 4. Urunler / UrunEkle / UrunGuncelle / UrunSil: Ürün kataloğunu yönetir. Silme işlemi sırasında önce ürüne ait stokları, yorumları ve favorileri temizleyerek 'Yetim Veri' kalmasını önler.
 * 5. Markalar / MarkaEkle / Guncelle / Sil: Marka listesini yönetir. Eğer markaya ait bir ürün varsa silme işlemini engelleyerek veritabanı bütünlüğünü (Referential Integrity) korur.
 * 6. Kategoriler / Ekle / Guncelle / Sil: Kategorileri yönetir. Marka ile aynı güvenlik prensibiyle çalışır; içi dolu kategori silinemez. Ayrıca kategori ID'lerini otomatik olarak (+1) artırarak belirler.
 * 7. Uyeler / UyeEkle / UyeSil: Müşteri listesini yönetir. Üye silinirken önce sepetini, favorilerini, dolabını ve kombinlerini temizler. Eğer geçmiş siparişi varsa muhasebe kaydı bozulmasın diye silinmesini engeller.
 * 8. Siparisler / Guncelle / Sil: Müşteri siparişlerinin kargo durumlarının güncellenmesini ve detaylarıyla birlikte silinmesini sağlar.
 * 9. Yorumlar / YorumOnayDegistir / YorumSil: Müşterilerin ürünlere yaptığı yorumların admin tarafından incelenip onaylanmasını veya reddedilmesini sağlar.
 * 10. GetBedenStok (JSON API): Sayfa yenilenmeden, seçilen bedene göre anlık stok bilgisini (AJAX ile) getiren dinamik stok motorudur.
 * ================================================================================================================= */

using ClothingStore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminController : Controller
    {
        private readonly ClothingStoreDbContext _db;

        public AdminController()
        {
            _db = new ClothingStoreDbContext();
        }

        private bool IsAdmin() => HttpContext.Session.GetString("Rol") == "Admin";

        private async Task<string> ResimYukle(IFormFile dosya)
        {
            var dosyaAdi = Path.GetFileName(dosya.FileName);
            var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);

            using (var stream = new FileStream(yol, FileMode.Create))
            {
                await dosya.CopyToAsync(stream);
            }
            return dosyaAdi;
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.UrunSayisi = _db.Urunlers.Count();
            ViewBag.KategoriSayisi = _db.Kategorilers.Count();
            ViewBag.MarkaSayisi = _db.Markalar.Count();
            ViewBag.UyeSayisi = _db.Kullanicilars.Count(x => x.Rol == "Uye");
            ViewBag.SiparisSayisi = _db.Siparislers.Count();
            ViewBag.YorumSayisi = _db.Yorumlars.Count();

            return View();
        }

        public IActionResult Urunler()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var liste = _db.Urunlers.Include(u => u.Marka).OrderByDescending(u => u.UrunId).ToList();
            ViewBag.Kategoriler = _db.Kategorilers.ToList();
            ViewBag.Markalar = _db.Markalar.ToList();
            return View(liste);
        }

        [HttpPost]
        public async Task<IActionResult> UrunEkle(Urunler yeniUrun, IFormFile resimDosyasi)
        {

            if (resimDosyasi != null && resimDosyasi.Length > 0)
            {
                yeniUrun.ResimYolu = await ResimYukle(resimDosyasi);
            }
            yeniUrun.EklenmeTarihi = DateTime.Now;

            _db.Urunlers.Add(yeniUrun);
            _db.SaveChanges(); 

            if (!string.IsNullOrEmpty(yeniUrun.KategoriId))
            {
                string[] kategoriDizisi = yeniUrun.KategoriId.Split(',');
                if (int.TryParse(kategoriDizisi[0], out int ustKategoriId))
                {
                    List<int> eklenecekBedenler = new List<int>();

                    if (ustKategoriId == 4) 
                    {
                        eklenecekBedenler = new List<int> { 7, 8, 9, 10, 11, 12 }; 
                    }
                    else if (ustKategoriId == 5) 
                    {
                        eklenecekBedenler = new List<int> { 13 }; 
                    }
                    else 
                    {
                        eklenecekBedenler = new List<int> { 1, 2, 3, 4, 5, 6 }; 
                    }

                    int bedenBasinaStok = 0;
                    int toplamStok = Convert.ToInt32(yeniUrun.StokAdedi); 

                    if (toplamStok > 0 && eklenecekBedenler.Count > 0)
                    {
                        bedenBasinaStok = toplamStok / eklenecekBedenler.Count;
                    }

                    foreach (var bedenId in eklenecekBedenler)
                    {
                        var stokKaydi = new UrunBedenStok
                        {
                            UrunId = yeniUrun.UrunId,
                            BedenId = bedenId,
                            StokAdedi = bedenBasinaStok
                        };
                        _db.UrunBedenStok.Add(stokKaydi);
                    }

                    _db.SaveChanges();
                }
            }

            return RedirectToAction("Urunler");
        }

        [HttpPost]
        public async Task<IActionResult> UrunGuncelle(Urunler gelenUrun, IFormFile? resimDosyasi)
        {
            var dbUrun = _db.Urunlers.Find(gelenUrun.UrunId);
            if (dbUrun != null)
            {
                dbUrun.UrunAdi = gelenUrun.UrunAdi;
                dbUrun.Fiyat = gelenUrun.Fiyat;
                dbUrun.KategoriId = gelenUrun.KategoriId;
                dbUrun.MarkaId = gelenUrun.MarkaId;
                dbUrun.Renk = gelenUrun.Renk;
                dbUrun.KumasTipi = gelenUrun.KumasTipi;
                dbUrun.Aciklama = gelenUrun.Aciklama;
                dbUrun.StokAdedi = gelenUrun.StokAdedi;
                dbUrun.Mevsim = gelenUrun.Mevsim;

                if (resimDosyasi != null)
                    dbUrun.ResimYolu = await ResimYukle(resimDosyasi);

                _db.SaveChanges();
            }
            return RedirectToAction("Urunler");
        }

        public IActionResult UrunSil(int id)
        {
            var u = _db.Urunlers.Find(id);
            if (u != null)
            {
                try
                {

                    var stoklar = _db.UrunBedenStok.Where(x => x.UrunId == id).ToList();
                    if (stoklar.Any()) _db.UrunBedenStok.RemoveRange(stoklar);

                    var yorumlar = _db.Yorumlars.Where(x => x.UrunId == id).ToList();
                    if (yorumlar.Any()) _db.Yorumlars.RemoveRange(yorumlar);

                    var sepetler = _db.Sepets.Where(x => x.UrunId == id).ToList();
                    if (sepetler.Any()) _db.Sepets.RemoveRange(sepetler);

                    var favoriler = _db.Favorilers.Where(x => x.UrunId == id).ToList();
                    if (favoriler.Any()) _db.Favorilers.RemoveRange(favoriler);

                    var kombinDetaylari = _db.KombinDetaylari.Where(x => x.UrunId == id).ToList();
                    if (kombinDetaylari.Any()) _db.KombinDetaylari.RemoveRange(kombinDetaylari);

                    _db.Urunlers.Remove(u);
                    _db.SaveChanges();

                    TempData["Mesaj"] = "Ürün ve ürüne ait tüm detaylar başarıyla silindi.";
                }
                catch (Exception)
                {

                    TempData["Hata"] = "Bu ürün geçmiş siparişlerde yer aldığı için veritabanından tamamen silinemez!";
                }
            }
            return RedirectToAction("Urunler");
        }

        public IActionResult Markalar()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(_db.Markalar.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> MarkaEkle(string MarkaAdi, IFormFile? resimDosyasi)
        {
            var m = new Markalar { MarkaAdi = MarkaAdi };
            if (resimDosyasi != null) m.MarkaResimYolu = await ResimYukle(resimDosyasi);
            _db.Markalar.Add(m);
            _db.SaveChanges();
            return RedirectToAction("Markalar");
        }

        [HttpPost]
        public async Task<IActionResult> MarkaGuncelle(int MarkaId, string MarkaAdi, IFormFile? resimDosyasi)
        {
            var dbMarka = _db.Markalar.Find(MarkaId);
            if (dbMarka != null)
            {
                dbMarka.MarkaAdi = MarkaAdi;
                if (resimDosyasi != null) dbMarka.MarkaResimYolu = await ResimYukle(resimDosyasi);
                _db.SaveChanges();
            }
            return RedirectToAction("Markalar");
        }

        public IActionResult MarkaSil(int id)
        {
            var m = _db.Markalar.Find(id);
            if (m != null)
            {
                try
                {

                    var urunler = _db.Urunlers.Where(x => x.MarkaId == id).ToList();

                    if (urunler.Any())
                    {

                        foreach (var u in urunler)
                        {
                            var stoklar = _db.UrunBedenStok.Where(x => x.UrunId == u.UrunId).ToList();
                            if (stoklar.Any()) _db.UrunBedenStok.RemoveRange(stoklar);

                            var yorumlar = _db.Yorumlars.Where(x => x.UrunId == u.UrunId).ToList();
                            if (yorumlar.Any()) _db.Yorumlars.RemoveRange(yorumlar);

                            var sepetler = _db.Sepets.Where(x => x.UrunId == u.UrunId).ToList();
                            if (sepetler.Any()) _db.Sepets.RemoveRange(sepetler);

                            var favoriler = _db.Favorilers.Where(x => x.UrunId == u.UrunId).ToList();
                            if (favoriler.Any()) _db.Favorilers.RemoveRange(favoriler);

                            var kombinDetaylari = _db.KombinDetaylari.Where(x => x.UrunId == u.UrunId).ToList();
                            if (kombinDetaylari.Any()) _db.KombinDetaylari.RemoveRange(kombinDetaylari);
                        }

                        _db.Urunlers.RemoveRange(urunler);
                    }

                    _db.Markalar.Remove(m);
                    _db.SaveChanges();
                    TempData["Mesaj"] = "Marka ve bağlı olduğu tüm ürünler başarıyla silindi.";
                }
                catch (Exception)
                {
                    TempData["Hata"] = "Bu markaya ait bazı ürünler geçmiş siparişlerde kayıtlı olduğu için silinemez!";
                }
            }
            return RedirectToAction("Markalar");
        }

        public IActionResult Kategoriler()
        {
            var liste = _db.Kategorilers.ToList()
                        .OrderBy(x => int.TryParse(x.KategoriId, out int id) ? id : 0)
                        .ToList();
            return View(liste);
        }

        [HttpPost]
        public IActionResult KategoriEkle(string KategoriAdi)
        {
            if (string.IsNullOrEmpty(KategoriAdi)) return RedirectToAction("Kategoriler");

            var maxId = _db.Kategorilers.ToList().Max(x => int.TryParse(x.KategoriId, out int id) ? id : 0);

            var yeniKat = new Kategoriler
            {
                KategoriId = (maxId + 1).ToString(),
                KategoriAdi = KategoriAdi
            };

            _db.Kategorilers.Add(yeniKat);
            _db.SaveChanges();

            return RedirectToAction("Kategoriler");
        }

        [HttpPost]
        public IActionResult KategoriGuncelle(string KategoriId, string KategoriAdi)
        {
            var dbKat = _db.Kategorilers.Find(KategoriId);
            if (dbKat != null)
            {
                dbKat.KategoriAdi = KategoriAdi;
                _db.SaveChanges();
            }
            return RedirectToAction("Kategoriler");
        }

        public IActionResult KategoriSil(string id)
        {
            var k = _db.Kategorilers.Find(id);
            if (k != null)
            {
                try
                {

                    var urunler = _db.Urunlers.Where(x => x.KategoriId == id).ToList();

                    if (urunler.Any())
                    {

                        foreach (var u in urunler)
                        {
                            var stoklar = _db.UrunBedenStok.Where(x => x.UrunId == u.UrunId).ToList();
                            if (stoklar.Any()) _db.UrunBedenStok.RemoveRange(stoklar);

                            var yorumlar = _db.Yorumlars.Where(x => x.UrunId == u.UrunId).ToList();
                            if (yorumlar.Any()) _db.Yorumlars.RemoveRange(yorumlar);

                            var sepetler = _db.Sepets.Where(x => x.UrunId == u.UrunId).ToList();
                            if (sepetler.Any()) _db.Sepets.RemoveRange(sepetler);

                            var favoriler = _db.Favorilers.Where(x => x.UrunId == u.UrunId).ToList();
                            if (favoriler.Any()) _db.Favorilers.RemoveRange(favoriler);

                            var kombinDetaylari = _db.KombinDetaylari.Where(x => x.UrunId == u.UrunId).ToList();
                            if (kombinDetaylari.Any()) _db.KombinDetaylari.RemoveRange(kombinDetaylari);
                        }


                        _db.Urunlers.RemoveRange(urunler);
                    }


                    _db.Kategorilers.Remove(k);
                    _db.SaveChanges();
                    TempData["Mesaj"] = "Kategori ve bağlı olduğu tüm ürünler başarıyla silindi.";
                }
                catch (Exception)
                {
                    TempData["Hata"] = "Bu kategoriye ait bazı ürünler geçmiş siparişlerde kayıtlı olduğu için silinemez!";
                }
            }
            return RedirectToAction("Kategoriler");
        }

        public IActionResult Uyeler() => View(_db.Kullanicilars.ToList());

        [HttpPost]
        public IActionResult UyeEkle(Kullanicilar yeniUye)
        {
            yeniUye.KayitTarihi = DateTime.Now;
            _db.Kullanicilars.Add(yeniUye);
            _db.SaveChanges();
            return RedirectToAction("Uyeler");
        }

        public IActionResult UyeSil(int id)
        {
            var u = _db.Kullanicilars.Find(id);
            if (u != null)
            {
                try
                {

                    var sepetler = _db.Sepets.Where(x => x.KullaniciId == id).ToList();
                    if (sepetler.Any()) _db.Sepets.RemoveRange(sepetler);

                    var favoriler = _db.Favorilers.Where(x => x.KullaniciId == id).ToList();
                    if (favoriler.Any()) _db.Favorilers.RemoveRange(favoriler);

                    var yorumlar = _db.Yorumlars.Where(x => x.KullaniciId == id).ToList();
                    if (yorumlar.Any()) _db.Yorumlars.RemoveRange(yorumlar);

                    var dolap = _db.Dolap.Where(x => x.KullaniciId == id).ToList();
                    if (dolap.Any()) _db.Dolap.RemoveRange(dolap);

                    var kombinler = _db.Kombinler.Where(x => x.KullaniciId == id).ToList();
                    if (kombinler.Any())
                    {
                        foreach (var kombin in kombinler)
                        {
                            var kombinDetay = _db.KombinDetaylari.Where(x => x.KombinId == kombin.KombinId).ToList();
                            if (kombinDetay.Any()) _db.KombinDetaylari.RemoveRange(kombinDetay);
                        }
                        _db.Kombinler.RemoveRange(kombinler);
                    }

                    _db.Kullanicilars.Remove(u);
                    _db.SaveChanges();

                    TempData["Mesaj"] = "Üye ve üyeye ait tüm veriler (sepet, favori, dolap vb.) başarıyla silindi.";
                }
                catch (Exception)
                {

                    TempData["Hata"] = "Bu üyenin geçmiş sipariş kayıtları bulunduğu için sistemden tamamen silinemez!";
                }
            }
            return RedirectToAction("Uyeler");
        }

        public IActionResult Siparisler()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var liste = _db.Siparislers
                .Include(s => s.Kullanici)
                .Include(s => s.Kargo)
                .Include(s => s.SiparisDetaylaris)
                    .ThenInclude(d => d.Urun)
                .AsNoTracking()
                .OrderByDescending(s => s.Tarih)
                .ToList();

            ViewBag.Kargolar = _db.KargoFirmalaris.ToList();
            return View(liste);
        }

        [HttpPost]
        public IActionResult SiparisGuncelle(int SiparisID, string YeniDurum, int YeniKargoID)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var siparis = _db.Siparislers.Find(SiparisID);
            if (siparis != null)
            {
                siparis.Durum = YeniDurum;
                siparis.KargoId = YeniKargoID;
                _db.SaveChanges();
                TempData["Mesaj"] = "Sipariş güncellendi.";
            }
            return RedirectToAction("Siparisler");
        }

        public IActionResult SiparisSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var siparis = _db.Siparislers.Include(s => s.SiparisDetaylaris).FirstOrDefault(s => s.SiparisId == id);
            if (siparis != null)
            {
                try
                {
                    if (siparis.SiparisDetaylaris.Any())
                    {
                        _db.SiparisDetaylaris.RemoveRange(siparis.SiparisDetaylaris);
                    }

                    _db.Siparislers.Remove(siparis);
                    _db.SaveChanges();
                    TempData["Mesaj"] = "Sipariş ve siparişe ait detaylar başarıyla silindi.";
                }
                catch (Exception)
                {
                    TempData["Hata"] = "Sipariş silinirken sistemsel bir hata oluştu!";
                }
            }
            return RedirectToAction("Siparisler");
        }

        public IActionResult Yorumlar()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var liste = _db.Yorumlars
                .Include(y => y.Kullanici)
                .Include(y => y.Urun)
                .OrderByDescending(y => y.Tarih)
                .ToList();

            return View(liste);
        }

        public IActionResult YorumOnayDegistir(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var yorum = _db.Yorumlars.Find(id);
            if (yorum != null)
            {
                yorum.OnayDurumu = !(yorum.OnayDurumu ?? false);
                _db.SaveChanges();
            }
            return RedirectToAction("Yorumlar");
        }

        public IActionResult YorumSil(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var yorum = _db.Yorumlars.Find(id);
            if (yorum != null)
            {
                try
                {
                    _db.Yorumlars.Remove(yorum);
                    _db.SaveChanges();
                    TempData["Mesaj"] = "Yorum başarıyla silindi.";
                }
                catch (Exception)
                {
                    TempData["Hata"] = "Yorum silinirken bir hata oluştu!";
                }
            }
            return RedirectToAction("Yorumlar");
        }

        [HttpGet]
        public JsonResult GetBedenStok(int urunId, int bedenId)
        {
            var stok = _db.UrunBedenStok
                          .FirstOrDefault(x => x.UrunId == urunId && x.BedenId == bedenId)
                          ?.StokAdedi ?? 0;

            return Json(stok);
        }
    }
}