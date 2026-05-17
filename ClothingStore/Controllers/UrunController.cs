/* =================================================================================================================
 * DOSYA ADI: UrunController.cs
 * AMACI: Mağaza envanterindeki tüm ürünlerin sergilenmesi, filtrelenmesi ve detaylandırılması süreçlerini yönetir. 
 * Dinamik arama motoru, ürün yorumlama sistemi ve stok durum takibi bu birim üzerinden gerçekleştirilir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - Entity Framework Core (Eager Loading): .Include() ve .ThenInclude() yapıları ile Ürün -> Marka -> Beden -> Stok 
 * ilişkilerinin tek seferde, performanslı bir şekilde veritabanından çekilmesi.
 * - Kompleks LINQ Sorguları: Kategorilerin virgülle ayrılmış (CSV) yapısını analiz eden `.AsEnumerable()` tabanlı 
 * dinamik kategori eşleştirme ve çok parametreli (Marka, Beden, Renk) filtreleme algoritmaları.
 * - SQL Fonksiyon Entegrasyonu (SqlQueryRaw): Veritabanı seviyesinde tanımlanmış `fn_StokDurum` fonksiyonunu 
 * C# tarafında çağırarak stok kritik seviye kontrollerinin gerçekleştirilmesi.
 * - State Management: `TempData` ve `ViewBag` üzerinden kullanıcıya işlem sonuçlarının ve favori listesi gibi 
 * dinamik verilerin anlık olarak aktarılması.
 * * ÖNE ÇIKAN İŞLEVLER:
 * 1. TumUrunler: Mağaza kataloğunda fiyat, yenilik ve satış performansına göre sıralama yapabilen, aynı zamanda 
 * kategori hiyerarşisini bozmadan filtreleme sağlayan kapsamlı liste motorudur.
 * 2. Detay: Seçilen ürünün tüm özelliklerini, onaylı kullanıcı yorumlarını ve "Benzer Ürünler" algoritması ile 
 * kategori bazlı önerileri tek bir ekranda birleştirir.
 * 3. YorumEkle: Satın alma kontrolü ve admin onay mekanizmasıyla entegre çalışan güvenli geri bildirim modülüdür.
 * 4. UrunBedenleriGetir (JSON API): AJAX istekleri için ürünün sadece stokta bulunan bedenlerini asenkron 
 * olarak dönen veri servisidir.
 * 5. Favori Listesi Entegrasyonu: Kullanıcının oturum bilgisini kontrol ederek liste ekranında favoriye 
 * eklenen ürünlerin görsel olarak işaretlenmesini sağlar.
 * ================================================================================================================= */

using Microsoft.AspNetCore.Mvc;
using ClothingStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;

namespace ClothingStore.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class UrunController : Controller
    {
        ClothingStoreDbContext db = new ClothingStoreDbContext();

        public IActionResult TumUrunler(int? kategoriId, string marka, string beden, string renk, string aranacakKelime, string siralama)
        {
            var sorgu = db.Urunlers
                .Include(u => u.Marka)
                .Include(u => u.UrunBedenStok).ThenInclude(ubs => ubs.Beden)
                .AsQueryable();

            if (!string.IsNullOrEmpty(aranacakKelime))
            {
                string terim = aranacakKelime.ToLower();
                sorgu = sorgu.Where(u => u.UrunAdi.ToLower().Contains(terim) ||
                                         u.Aciklama.ToLower().Contains(terim));
            }

            IEnumerable<Urunler> urunHavuzu;
            if (kategoriId != null)
            {
                string idStr = kategoriId.ToString();
                urunHavuzu = sorgu.AsEnumerable()
                    .Where(u => !string.IsNullOrEmpty(u.KategoriId) &&
                                u.KategoriId.Split(',').Select(s => s.Trim()).Contains(idStr));

                var kat = db.Kategorilers.FirstOrDefault(k => k.KategoriId == idStr);
                ViewBag.FiltreBaslik = kat?.KategoriAdi;
            }
            else
            {
                urunHavuzu = sorgu.AsEnumerable();
            }

            if (!string.IsNullOrEmpty(marka)) urunHavuzu = urunHavuzu.Where(u => u.Marka?.MarkaAdi == marka);
            if (!string.IsNullOrEmpty(beden)) urunHavuzu = urunHavuzu.Where(u => u.UrunBedenStok.Any(st => st.Beden.BedenTanimi == beden));
            if (!string.IsNullOrEmpty(renk)) urunHavuzu = urunHavuzu.Where(u => u.Renk == renk);

            switch (siralama)
            {
                case "en_yeniler": urunHavuzu = urunHavuzu.OrderByDescending(u => u.UrunId).Take(10); break;
                case "cok_satanlar": urunHavuzu = urunHavuzu.OrderBy(u => u.StokAdedi).Take(10); break;
                case "fiyat_artan": urunHavuzu = urunHavuzu.OrderBy(u => u.Fiyat); break;
                case "fiyat_azalan": urunHavuzu = urunHavuzu.OrderByDescending(u => u.Fiyat); break;
                case "a_z": urunHavuzu = urunHavuzu.OrderBy(u => u.UrunAdi); break;
                case "z_a": urunHavuzu = urunHavuzu.OrderByDescending(u => u.UrunAdi); break;
                default: urunHavuzu = urunHavuzu.OrderByDescending(u => u.UrunId); break;
            }

            ViewBag.Kategoriler = db.Kategorilers.OrderBy(x => x.KategoriAdi).ToList();
            ViewBag.Markalar = db.Markalar.Select(m => m.MarkaAdi).Distinct().ToList();
            ViewBag.Bedenler = db.Bedenler.Select(b => b.BedenTanimi).ToList();
            ViewBag.Renkler = db.Urunlers.Where(u => u.Renk != null).Select(u => u.Renk).Distinct().ToList();
            ViewBag.SeciliKategori = kategoriId;
            ViewBag.SeciliMarka = marka;
            ViewBag.SeciliBeden = beden;
            ViewBag.SeciliRenk = renk;
            ViewBag.SeciliSiralama = siralama;

            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            ViewBag.FavoriListesi = kullaniciId != null ? db.Favorilers.Where(x => x.KullaniciId == (int)kullaniciId).Select(x => x.UrunId).ToList() : new List<int>();
            if (ViewBag.FiltreBaslik == null) ViewBag.FiltreBaslik = "Tüm Ürünler";

            return View(urunHavuzu.ToList());
        }

        public IActionResult Detay(int id)
        {
            var urun = db.Urunlers
                .Include(u => u.Marka)
                .Include(u => u.UrunBedenStok).ThenInclude(ubs => ubs.Beden)
                .Include(u => u.Yorumlars.Where(y => y.OnayDurumu == true)).ThenInclude(y => y.Kullanici)
                .FirstOrDefault(x => x.UrunId == id);

            if (urun == null) return RedirectToAction("TumUrunler");

            ViewBag.MevcutBedenler = urun.UrunBedenStok
                .Where(stok => stok.StokAdedi > 0)
                .Select(stok => stok.Beden)
                .ToList();

            ViewBag.StokDurumMetni = db.Database
                .SqlQueryRaw<string>("SELECT dbo.fn_StokDurum({0})", urun.StokAdedi ?? 0)
                .AsEnumerable().FirstOrDefault();

            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            var rol = HttpContext.Session.GetString("Rol");

            ViewBag.SatinAldiMi = kullaniciId != null && rol == "Uye" && db.SiparisDetaylaris.Any(sd =>
                    sd.UrunId == id && sd.Siparis.KullaniciId == kullaniciId && sd.Siparis.Durum == "Tamamlandı");

            ViewBag.YorumYaptiMi = kullaniciId != null && db.Yorumlars.Any(y => y.UrunId == id && y.KullaniciId == kullaniciId);

            var urunKategoriIdleri = urun.KategoriId?.Split(',').Select(k => k.Trim()).ToList() ?? new List<string>();
            ViewBag.BenzerUrunler = db.Urunlers
                .AsEnumerable()
                .Where(x => x.UrunId != id && x.KategoriId != null &&
                            x.KategoriId.Split(',').Any(cid => urunKategoriIdleri.Contains(cid.Trim())))
                .Take(10).ToList();

            return View(urun);
        }

        [HttpPost]
        public IActionResult YorumEkle(int UrunId, int Puan, string YorumMetni)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciID");
            if (kullaniciId == null) return RedirectToAction("Login", "Account");

            try
            {
                var yeniYorum = new Yorumlar
                {
                    KullaniciId = (int)kullaniciId,
                    UrunId = UrunId,
                    YorumMetni = YorumMetni,
                    Puan = Puan,
                    Tarih = DateTime.Now,
                    OnayDurumu = false 
                };

                db.Yorumlars.Add(yeniYorum);
                db.SaveChanges();

                TempData["Mesaj"] = "Yorumunuz başarıyla alındı, admin onayından sonra yayınlanacaktır.";
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Yorum kaydedilirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Detay", new { id = UrunId });
        }

        [HttpGet]
        public IActionResult UrunBedenleriGetir(int urunId)
        {
            var bedenler = db.UrunBedenStok
                .Where(x => x.UrunId == urunId && x.StokAdedi > 0)
                .Select(x => new { bedenId = x.BedenId, bedenAdi = x.Beden.BedenTanimi })
                .Distinct().ToList();
            return Json(bedenler);
        }

        [HttpGet]
        public JsonResult GetBedenStok(int urunId, int bedenId)
        {
            var stok = db.UrunBedenStok.FirstOrDefault(x => x.UrunId == urunId && x.BedenId == bedenId)?.StokAdedi ?? 0;
            return Json(stok);
        }

        public IActionResult Index() => RedirectToAction("TumUrunler");
    }
}