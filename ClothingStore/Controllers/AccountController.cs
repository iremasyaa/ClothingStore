/* =================================================================================================================
 * DOSYA ADI: AccountController.cs
 * AMACI: Sitenin kullanıcı (üye, admin, firma) hesap işlemlerini yöneten ana denetleyicidir. 
 * Giriş, kayıt, profil güncelleme, sipariş geçmişi, favoriler ve yorumlar bu dosyadan yönetilir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - ASP.NET Core MVC: Sitenin temel mimarisi. (Controller ve IActionResult yapıları)
 * - Entity Framework Core (LINQ): Veritabanı sorgularını (SQL yazmadan) C# objeleriyle yapmak için. (.Where, .FirstOrDefault, .Include)
 * - Session (HttpContext.Session): Kullanıcı giriş yaptığında bilgilerini tarayıcı kapana kadar hafızada tutmak için.
 * - AJAX Desteği (JSON): Sayfayı yenilemeden arka planda işlem yapmak için (Örn: Favori ekleme ve yorum güncelleme).
 * * METOTLAR (FONKSİYONLAR) VE İŞLEVLERİ:
 * 1. Login (GET/POST): Kullanıcının e-posta ve şifresiyle giriş yapmasını sağlar. Başarılı olursa Session (oturum) başlatır 
 * ve rolüne göre (Admin, Firma, Üye) ilgili sayfaya yönlendirir.
 * 2. AdminLogin / FirmaLogin (GET): Yönetici ve firmalar için özel giriş sayfalarını açar.
 * 3. Register (GET/POST): Yeni üye kaydı yapar. Aynı e-postadan varsa uyarır, yoksa veritabanına ekleyip oturum açar.
 * 4. Hesabim (GET): Kullanıcının profil bilgilerini veritabanından çekip ekrana basar.
 * 5. Siparislerim / SiparisDetay (GET): Kullanıcının geçmiş siparişlerini ve bu siparişlerin içindeki ürün detaylarını listeler.
 * 6. BilgiGuncelle (POST): Kullanıcının ad, adres, telefon ve şifre bilgilerini günceller.
 * 7. Yorumlarim / YorumDetayGetir / YorumGuncelle / YorumSil: Kullanıcının yaptığı yorumları listeler, 
 * JSON formatında verisini getirir (düzenlemek için), günceller veya siler.
 * 8. Favorilerim / FavoriEkleCikar: Kullanıcının beğendiği ürünleri listeler. Sayfa yenilenmeden (AJAX ile) 
 * ürünü favoriye ekleyip çıkarmayı sağlar.
 * 9. Logout: Kullanıcının oturumunu (Session) temizleyerek sistemden güvenli çıkış yapmasını sağlar.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccountController : Controller
    {
        ClothingStoreDbContext db = new ClothingStoreDbContext();

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string sifre)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
            {
                ViewBag.Hata = "Lütfen tüm alanları doldurun.";
                return View();
            }

            var kullanici = db.Kullanicilars.FirstOrDefault(x => x.Email == email.Trim() && x.Sifre == sifre.Trim());

            if (kullanici != null)
            {
                HttpContext.Session.SetInt32("KullaniciID", kullanici.KullaniciId);
                string adSoyad = (kullanici.Ad ?? "") + " " + (kullanici.Soyad ?? "");
                HttpContext.Session.SetString("AdSoyad", adSoyad.Trim());
                HttpContext.Session.SetString("Email", kullanici.Email);
                HttpContext.Session.SetString("Rol", kullanici.Rol ?? "Uye");

                var sepetAdet = db.Sepets.Where(x => x.KullaniciId == kullanici.KullaniciId).Sum(x => x.Adet ?? 0);
                HttpContext.Session.SetInt32("SepetAdet", (int)sepetAdet);

                if (kullanici.Rol == "Admin") return RedirectToAction("Index", "Admin");
                else if (kullanici.Rol == "Firma") return RedirectToAction("Index", "Firma");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Hata = "E-mail adresi veya şifre hatalı!";
            return View();
        }

        [HttpGet]
        public IActionResult AdminLogin() => View();

        [HttpGet]
        public IActionResult FirmaLogin() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(Kullanicilar k)
        {
            try
            {
                if (db.Kullanicilars.Any(x => x.Email == k.Email))
                {
                    ViewBag.Hata = "Bu e-posta adresi zaten kayıtlı.";
                    return View(k);
                }

                k.Rol = "Uye";
                k.KayitTarihi = DateTime.Now;

                db.Kullanicilars.Add(k);
                db.SaveChanges();

                HttpContext.Session.SetInt32("KullaniciID", k.KullaniciId);
                string adSoyad = (k.Ad ?? "") + " " + (k.Soyad ?? "");
                HttpContext.Session.SetString("AdSoyad", adSoyad.Trim());
                HttpContext.Session.SetString("Email", k.Email);
                HttpContext.Session.SetString("Rol", k.Rol);
                HttpContext.Session.SetInt32("SepetAdet", 0);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                string hata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewBag.Hata = "Kayıt sırasında bir hata oluştu: " + hata;
                return View(k);
            }
        }

        public IActionResult Hesabim()
        {
            var id = HttpContext.Session.GetInt32("KullaniciID");
            if (id == null) return RedirectToAction("Login");

            var kullanici = db.Kullanicilars.FirstOrDefault(x => x.KullaniciId == id);
            return View(kullanici);
        }

        public IActionResult Siparislerim()
        {
            var id = HttpContext.Session.GetInt32("KullaniciID");
            if (id == null) return RedirectToAction("Login");

            var siparisler = db.Siparislers
                               .Include(x => x.SiparisDetaylaris)
                               .Where(x => x.KullaniciId == id)
                               .OrderByDescending(x => x.Tarih)
                               .ToList();
            return View(siparisler);
        }

        public IActionResult SiparisDetay(int id)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login");

            var siparis = db.Siparislers
                            .Include(x => x.Kargo)
                            .Include(x => x.SiparisDetaylaris)
                            .ThenInclude(x => x.Urun)
                            .FirstOrDefault(x => x.SiparisId == id && x.KullaniciId == kullaniciId);

            if (siparis == null) return RedirectToAction("Siparislerim");
            return View(siparis);
        }

        [HttpPost]
        public IActionResult BilgiGuncelle(Kullanicilar k, string YeniSifre)
        {
            var kullanici = db.Kullanicilars.Find(k.KullaniciId);

            if (kullanici != null)
            {
                kullanici.Ad = k.Ad;
                kullanici.Soyad = k.Soyad;
                kullanici.Telefon = k.Telefon;
                kullanici.Adres = k.Adres;

                if (!string.IsNullOrWhiteSpace(YeniSifre))
                {
                    kullanici.Sifre = YeniSifre.Trim();
                }

                if (!string.IsNullOrEmpty(k.Email))
                {
                    var idn = new System.Globalization.IdnMapping();
                    try
                    {
                        kullanici.Email = k.Email.Contains("xn--") ? idn.GetUnicode(k.Email) : k.Email;
                    }
                    catch { kullanici.Email = k.Email; }
                }

                db.SaveChanges();

                string adSoyad = (kullanici.Ad ?? "") + " " + (kullanici.Soyad ?? "");
                HttpContext.Session.SetString("AdSoyad", adSoyad.Trim());

                TempData["Mesaj"] = "Bilgileriniz başarıyla güncellendi! ✨";
                return RedirectToAction("Hesabim");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Yorumlarim()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login");

            var yorumlar = db.Yorumlars
                .Include(y => y.Urun)
                .Where(y => y.KullaniciId == kullaniciId)
                .OrderByDescending(y => y.Tarih)
                .ToList();

            return View(yorumlar);
        }

        [HttpGet]
        public IActionResult YorumDetayGetir(int id)
        {
            var yorum = db.Yorumlars.Select(y => new { y.YorumId, y.YorumMetni, y.Puan }).FirstOrDefault(x => x.YorumId == id);
            return Json(yorum);
        }

        [HttpPost]
        public IActionResult YorumGuncelle(int YorumId, string YeniMetin, int YeniPuan)
        {
            var yorum = db.Yorumlars.Find(YorumId);
            if (yorum != null)
            {
                yorum.YorumMetni = YeniMetin;
                yorum.Puan = YeniPuan;
                yorum.Tarih = DateTime.Now;
                yorum.OnayDurumu = false;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public IActionResult YorumSil(int id)
        {
            var yorum = db.Yorumlars.Find(id);
            if (yorum != null)
            {
                db.Yorumlars.Remove(yorum);
                db.SaveChanges();
            }
            return RedirectToAction("Yorumlarim");
        }

        public IActionResult Favorilerim()
        {
            var id = HttpContext.Session.GetInt32("KullaniciID");
            if (id == null) return RedirectToAction("Login");

            var favoriler = db.Favorilers
                              .Where(x => x.KullaniciId == id)
                              .Include(x => x.Urun)
                              .Select(x => x.Urun)
                              .ToList();

            return View(favoriler);
        }

        public IActionResult FavoriEkleCikar(int UrunId)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");

            if (kullaniciId == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false });
                else return RedirectToAction("Login");
            }

            var mevcutFavori = db.Favorilers.FirstOrDefault(x => x.KullaniciId == kullaniciId && x.UrunId == UrunId);

            if (mevcutFavori != null) db.Favorilers.Remove(mevcutFavori);
            else db.Favorilers.Add(new Favoriler { KullaniciId = (int)kullaniciId, UrunId = UrunId });

            db.SaveChanges();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true });

            string referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/Home/Index" : referer);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}