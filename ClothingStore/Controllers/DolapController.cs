/* =================================================================================================================
 * DOSYA ADI: DolapController.cs
 * AMACI: Kullanıcıların kişisel dijital gardıroplarını (Dolabım modülü) yönetir. 
 * Fiziksel kıyafetlerin sisteme dijital varlık olarak aktarılması, kategorize edilmesi ve 
 * kombin oluşturma süreçlerine hazır hale getirilmesi bu kontrolcü üzerinden sağlanır.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - ASP.NET Core MVC & Session: Kullanıcı oturum kontrolü ve kişiye özel gardırop verilerinin ayrıştırılması.
 * - Entity Framework Core (Eager Loading): .Include() metodolojisi ile dolap ürünlerinin kategori detaylarıyla birlikte çekilmesi.
 * - File I/O & System.IO: Kullanıcı tarafından yüklenen resimlerin sunucu diskine güvenli şekilde fiziksel olarak kaydedilmesi.
 * - Asenkron Programlama (Task / await): Dosya işlemleri ve veritabanı kayıt süreçlerinde sistemin yanıt verme hızını korumak için.
 * * ÖNE ÇIKAN İŞLEVLER:
 * 1. Index: Aktif kullanıcıya ait dolap ürünlerini "Eklenme Tarihi"ne göre tersten sıralayarak listeler.
 * 2. UrunEkle (Async): Guid kütüphanesi kullanarak resim isimlerini benzersizleştirir ve fiziksel dosyayı 'wwwroot/img' dizinine aktarır.
 * 3. Sil: Veritabanı bütünlüğünü korumak için ürünü silmeden önce o ürünün dahil olduğu tüm kombin detaylarını temizler. 
 * Ayrıca sunucudaki resim dosyasını fiziksel olarak silerek gereksiz depolama kullanımını önler.
 * 4. Duzenle: Mevcut ürünlerin isim ve kategori bilgilerini dinamik olarak günceller; asenkron veri kaydı gerçekleştirir.
 * 5. Session Check: Kullanıcı oturumu kapanmışsa, yetkisiz veri erişimini engellemek için otomatik olarak giriş sayfasına yönlendirir.
 * ================================================================================================================= */

using ClothingStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class DolapController : Controller
{

    private readonly ClothingStoreDbContext _db;

    public DolapController(ClothingStoreDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var dolapUrunleri = _db.Dolap
                              .Include(x => x.Kategori)
                              .Where(x => x.KullaniciId == userId)
                              .OrderByDescending(x => x.EklenmeTarihi) 
                              .ToList();

        ViewBag.UstKategoriler = _db.Kategorilers.ToList();

        return View(dolapUrunleri);
    }

    [HttpPost]
    public async Task<IActionResult> UrunEkle(Dolap model, IFormFile resimDosyasi)
    {
        var userId = HttpContext.Session.GetInt32("KullaniciID");
        if (userId == null) return RedirectToAction("Login", "Account");

        if (resimDosyasi != null && resimDosyasi.Length > 0)
        {
            var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
            var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);

            using (var stream = new FileStream(yol, FileMode.Create))
            {
                await resimDosyasi.CopyToAsync(stream);
            }

            model.ResimYolu = dosyaAdi;
        }
        else
        {
            model.ResimYolu = "resim-yok.png";
        }

        model.KullaniciId = (int)userId;
        model.EklenmeTarihi = DateTime.Now;

        _db.Dolap.Add(model);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Sil(int id)
    {
        var urun = await _db.Dolap.FindAsync(id);
        if (urun != null)
        {
            var bagliKombinler = _db.KombinDetaylari.Where(x => x.DolapId == id);
            _db.KombinDetaylari.RemoveRange(bagliKombinler);

            if (urun.ResimYolu != "resim-yok.png")
            {
                var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", urun.ResimYolu);
                if (System.IO.File.Exists(yol)) System.IO.File.Delete(yol);
            }

            _db.Dolap.Remove(urun);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Duzenle(int DolapId, string UrunAdi, string KategoriId)
    {
        var urun = await _db.Dolap.FindAsync(DolapId);
        if (urun != null)
        {
            urun.UrunAdi = UrunAdi;
            urun.KategoriId = KategoriId;

            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}