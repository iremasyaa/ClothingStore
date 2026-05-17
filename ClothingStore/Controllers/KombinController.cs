/* =================================================================================================================
 * DOSYA ADI: KombinController.cs
 * AMACI: Projenin en özgün modülü olan "Hibrit Kombin Stüdyosu"nun yönetim merkezidir. 
 * Mağaza envanteri ile kullanıcıların dijital gardıroplarını (Dolabım) entegre ederek 
 * kişiselleştirilmiş kombinlerin oluşturulması, düzenlenmesi ve sepete aktarılması süreçlerini yönetir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - Entity Framework Core (Include & ThenInclude): Üç seviyeli ilişkisel veri yükleme (Kombin -> Detay -> Ürün/Dolap) 
 * yapılandırmasıyla karmaşık veri setlerinin tek sorguda çekilmesi.
 * - Database Transactions (BeginTransactionAsync): Kombin silme ve güncelleme gibi çoklu tablo operasyonlarında 
 * veri tutarlılığını garanti altına alan "Ya Hep Ya Hiç" prensibinin uygulanması.
 * - Asenkron Programlama & JSON API: Kullanıcı deneyimini (UX) kesintiye uğratmayan, sayfa yenilenmeden 
 * çalışan AJAX tabanlı asenkron metotlar.
 * - Dinamik Veri Yönetimi: List<int> parametreleri üzerinden mağaza ve dolap ID'lerini eşleştiren hibrit veri işleme motoru.
 * * ÖNE ÇIKAN İŞLEVLER:
 * 1. Index: Kullanıcının arşivlediği tüm kombinleri, içerisindeki tüm ürün detaylarıyla birlikte kronolojik sırada listeler.
 * 2. KombinKaydet (Async): Hem yeni kayıt hem de güncelleme (Update) yeteneğine sahip akıllı metottur. Eski detayları 
 * temizleyip yeni hiyerarşiyi kurarken Toplam Tutar hesaplamasını eşzamanlı gerçekleştirir.
 * 3. KaydetVeSepeteGit: Kombini kalıcı olarak arşive eklerken, içerisindeki mağaza ürünlerini otomatik olarak 
 * kullanıcının sepetine transfer eden entegrasyon birimidir.
 * 4. KombinDetaylariniGetir: Modal (açılır pencere) ekranları için JSON formatında hızlı veri servisi sağlar.
 * 5. KombiniSil: Bağlı bulunan tüm ilişkisel detayları (KombinDetaylari) temizleyerek ana kaydı sistemden 
 * güvenli bir şekilde (Transaction kontrolünde) kaldırır.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

public class KombinController : Controller
{
    private readonly ClothingStoreDbContext _db;

    public KombinController(ClothingStoreDbContext db)
    {
        _db = db;
    }
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return RedirectToAction("Login", "Account");

        var kombinler = _db.Kombinler
                           .Where(x => x.KullaniciId == userId)
                           .Include(k => k.KombinDetaylari) 
                               .ThenInclude(d => d.SitedekiUrun) 
                           .Include(k => k.KombinDetaylari)
                               .ThenInclude(d => d.DolaptakiUrun) 
                           .OrderByDescending(x => x.OlusturulmaTarihi)
                           .ToList();

        return View(kombinler);
    }

    public IActionResult KombinOlustur(int? id)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return RedirectToAction("Login", "Account");

        ViewBag.MagazaUrunleri = _db.Urunlers.Where(x => x.StokAdedi > 0).ToList();
        ViewBag.KisiselDolap = _db.Dolap.Where(x => x.KullaniciId == userId).ToList();

        if (id.HasValue)
        {
            var duzenlenecekKombin = _db.Kombinler
                .Include(k => k.KombinDetaylari)
                    .ThenInclude(d => d.SitedekiUrun)
                .Include(k => k.KombinDetaylari)
                    .ThenInclude(d => d.DolaptakiUrun)
                .FirstOrDefault(k => k.KombinId == id && k.KullaniciId == userId);

            if (duzenlenecekKombin != null)
            {
                return View(duzenlenecekKombin);
            }
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> KombiniKaydet(string KombinAdi, List<int> MagazaUrunIds, List<int> DolapUrunIds, int? KombinId)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return Json(new { success = false, message = "Oturum süresi doldu." });

        if (string.IsNullOrEmpty(KombinAdi))
            return Json(new { success = false, message = "Lütfen kombine bir isim verin." });

        int toplamSecilen = (MagazaUrunIds?.Count ?? 0) + (DolapUrunIds?.Count ?? 0);
        if (toplamSecilen < 2)
            return Json(new { success = false, message = "Kombin oluşturmak için en az 2 parça seçmelisiniz." });

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                Kombinler kombin;

                if (KombinId.HasValue && KombinId > 0)
                {
                    kombin = _db.Kombinler.FirstOrDefault(x => x.KombinId == KombinId && x.KullaniciId == userId);
                    if (kombin == null) return Json(new { success = false, message = "Güncellenecek kombin bulunamadı." });

                    kombin.KombinAdi = KombinAdi;
                    kombin.OlusturulmaTarihi = DateTime.Now;

                    var eskiDetaylar = _db.KombinDetaylari.Where(x => x.KombinId == KombinId).ToList();
                    _db.KombinDetaylari.RemoveRange(eskiDetaylar);
                }
                else
                {
                    kombin = new Kombinler { KullaniciId = (int)userId, KombinAdi = KombinAdi, OlusturulmaTarihi = DateTime.Now, ToplamTutar = 0 };
                    _db.Kombinler.Add(kombin);
                }

                await _db.SaveChangesAsync();

                decimal toplamFiyat = 0;

                if (MagazaUrunIds != null)
                {
                    foreach (var uId in MagazaUrunIds)
                    {
                        var urun = await _db.Urunlers.FindAsync(uId);
                        if (urun != null)
                        {
                            toplamFiyat += urun.Fiyat;
                            _db.KombinDetaylari.Add(new KombinDetaylari { KombinId = kombin.KombinId, UrunId = uId });
                        }
                    }
                }

                if (DolapUrunIds != null)
                {
                    foreach (var dId in DolapUrunIds)
                    {
                        _db.KombinDetaylari.Add(new KombinDetaylari { KombinId = kombin.KombinId, DolapId = dId });
                    }
                }

                kombin.ToplamTutar = toplamFiyat;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = KombinId.HasValue ? "Kombin güncellendi!" : "Kombin başarıyla oluşturuldu!", kombinId = kombin.KombinId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Hata: " + errorMsg });
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> KaydetVeSepeteGit(string KombinAdi, List<int> MagazaUrunIds, List<int> DolapUrunIds)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

        var kayitSonucu = await KombiniKaydet(KombinAdi, MagazaUrunIds, DolapUrunIds, null);

        if (kayitSonucu is JsonResult jsonResult)
        {
            dynamic result = jsonResult.Value;
            if (result.success == false) return kayitSonucu;
        }

        if (MagazaUrunIds != null && MagazaUrunIds.Count > 0)
        {
            foreach (var uId in MagazaUrunIds)
            {
                var sepetItem = _db.Sepets.FirstOrDefault(s => s.KullaniciId == userId && s.UrunId == uId);
                if (sepetItem != null)
                {
                    sepetItem.Adet++;
                }
                else
                {
                    _db.Sepets.Add(new Sepet
                    {
                        KullaniciId = (int)userId,
                        UrunId = uId,
                        Adet = 1,
                        EklenmeTarihi = DateTime.Now
                    });
                }
            }
            await _db.SaveChangesAsync();
        }

        return Json(new { success = true, redirectUrl = Url.Action("Index", "Sepet") });
    }

    [HttpGet]
    public IActionResult KombinDetaylariniGetir(int id)
    {
        var detaylar = _db.KombinDetaylari
            .Where(d => d.KombinId == id)
            .Include(d => d.SitedekiUrun)
            .Include(d => d.DolaptakiUrun)
            .Select(d => new {
                Ad = d.UrunId != null ? d.SitedekiUrun.UrunAdi : d.DolaptakiUrun.UrunAdi,
                Resim = d.UrunId != null ? d.SitedekiUrun.ResimYolu : d.DolaptakiUrun.ResimYolu,
                Fiyat = d.UrunId != null ? d.SitedekiUrun.Fiyat : 0,
                Tur = d.UrunId != null ? "Mağaza Ürünü" : "Kişisel Dolap"
            }).ToList();

        return Json(detaylar);
    }

    [HttpPost]
    public async Task<IActionResult> KombiniSepeteEkle(int id)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

        var urunler = _db.KombinDetaylari.Where(x => x.KombinId == id && x.UrunId != null).ToList();

        foreach (var item in urunler)
        {
            var sepet = _db.Sepets.FirstOrDefault(s => s.KullaniciId == userId && s.UrunId == item.UrunId);
            if (sepet != null) sepet.Adet++;
            else _db.Sepets.Add(new Sepet { KullaniciId = (int)userId, UrunId = (int)item.UrunId, Adet = 1, EklenmeTarihi = DateTime.Now });
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> KombiniSil(int id)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return Json(new { success = false, message = "Oturum süresi doldu." });

        var kombin = _db.Kombinler.FirstOrDefault(x => x.KombinId == id && x.KullaniciId == userId);
        if (kombin == null) return Json(new { success = false, message = "Kombin bulunamadı." });

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var detaylar = _db.KombinDetaylari.Where(x => x.KombinId == id).ToList();
                _db.KombinDetaylari.RemoveRange(detaylar);
                _db.Kombinler.Remove(kombin);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Kombin başarıyla silindi." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}