/* =================================================================================================================
 * DOSYA ADI: ClothingStoreDbContext.cs
 * AMACI: Uygulamanın veritabanı katmanını (Data Access Layer) temsil eder. Entity Framework Core kullanarak 
 * C# sınıfları ile SQL Server tabloları arasındaki nesne-ilişkisel eşleşmeyi (ORM) yönetir. 
 * Veritabanı şemasının, ilişkilerin (1:n, n:m) ve veri bütünlüğü kurallarının ana merkezidir.
 * * KULLANILAN TEKNOLOJİLER VE KÜTÜPHANELER:
 * - Entity Framework Core (Fluent API): Tablo isimleri, birincil anahtarlar (Primary Key) ve 
 * yabancı anahtarların (Foreign Key) detaylı konfigürasyonu.
 * - SQL Server Entegrasyonu: Bağlantı dizgisi (Connection String) yönetimi ve SQL veri tiplerinin 
 * (decimal, datetime) C# karşılıklarının belirlenmesi.
 * - Trigger (Tetikleyici) Bildirimleri: Veritabanı seviyesinde çalışan tetikleyicilerin (trg_Siparis, trg_Sepet vb.) 
 * EF Core tarafına tanıtılarak veri tutarlılığının senkronize edilmesi.
 * * ÖNE ÇIKAN YAPILANDIRMALAR:
 * 1. OnModelCreating: Tüm varlıkların (Entities) tablo isimlerini otomatik olarak sınıf isimlerine eşitler.
 * 2. Fluent API İlişkileri: Sepet, Favoriler ve Sipariş Detayları gibi tabloların Ürünler ve Kullanıcılar 
 * tablolarıyla olan hiyerarşisini kurgular.
 * 3. Cascade Delete Yönetimi: Sepet ve Favori kayıtlarında ürün silindiğinde ilgili detayların da 
 * temizlenmesini sağlayarak "Yetim Veri" oluşumunu engeller.
 * 4. Veri Tipi Hassasiyeti: Finansal işlemler için 'decimal(10,2)' kullanarak kuruş hassasiyetli 
 * hesaplamalara olanak tanır.
 * 5. OnConfiguring: SQL Server bağlantı parametrelerini ve güvenlik sertifikası tercihlerini yönetir.
 * ================================================================================================================= */

using ClothingStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class ClothingStoreDbContext : DbContext
{
    public ClothingStoreDbContext() { }

    public ClothingStoreDbContext(DbContextOptions<ClothingStoreDbContext> options)
        : base(options) { }

    public virtual DbSet<Favoriler> Favorilers { get; set; }
    public virtual DbSet<Dolap> Dolap { get; set; }
    public virtual DbSet<Kombinler> Kombinler { get; set; }
    public virtual DbSet<KombinDetaylari> KombinDetaylari { get; set; }
    public virtual DbSet<KargoFirmalari> KargoFirmalaris { get; set; }
    public virtual DbSet<Kategoriler> Kategorilers { get; set; }
    public DbSet<Markalar> Markalar { get; set; }
    public virtual DbSet<Urunler> Urunlers { get; set; }
    public virtual DbSet<Kullanicilar> Kullanicilars { get; set; }
    public virtual DbSet<Sepet> Sepets { get; set; }
    public virtual DbSet<SiparisDetaylari> SiparisDetaylaris { get; set; }
    public virtual DbSet<Siparisler> Siparislers { get; set; }
    public virtual DbSet<Yorumlar> Yorumlars { get; set; }
    public virtual DbSet<Bedenler> Bedenler { get; set; }
    public virtual DbSet<UrunBedenStok> UrunBedenStok { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=iremasya_\\SQLEXPRESS;Database=ClothingStoreDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.ClrType.Name);
        }

        modelBuilder.Entity<KargoFirmalari>().HasKey(e => e.KargoId);
        modelBuilder.Entity<Kullanicilar>().HasKey(e => e.KullaniciId);
        modelBuilder.Entity<Kategoriler>().HasKey(e => e.KategoriId);
        modelBuilder.Entity<Siparisler>().HasKey(e => e.SiparisId);
        modelBuilder.Entity<Bedenler>().HasKey(e => e.BedenId);
        modelBuilder.Entity<UrunBedenStok>().HasKey(e => e.UrunBedenId);

        modelBuilder.Entity<Siparisler>().ToTable(tb => tb.HasTrigger("trg_Siparis"));
        modelBuilder.Entity<SiparisDetaylari>().ToTable(tb => tb.HasTrigger("trg_SiparisDetay"));
        modelBuilder.Entity<Sepet>().ToTable(tb => tb.HasTrigger("trg_Sepet"));

        modelBuilder.Entity<UrunBedenStok>().ToTable(tb => tb.HasTrigger("trg_UrunToplamStokGuncelle"));

        modelBuilder.Entity<Urunler>(entity =>
        {
            entity.HasKey(e => e.UrunId);
            entity.Property(e => e.UrunId).HasColumnName("UrunID");
            entity.Property(e => e.UrunAdi).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Fiyat).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Renk).HasMaxLength(50);
            entity.Property(e => e.KumasTipi).HasMaxLength(100);
            entity.Property(e => e.ResimYolu).HasMaxLength(250);
            entity.Property(e => e.EklenmeTarihi).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.KategoriId).HasMaxLength(50).IsUnicode(false);
        });

        modelBuilder.Entity<Sepet>(entity =>
        {
            entity.HasKey(e => e.SepetId);
            entity.HasOne(d => d.Urun).WithMany(p => p.Sepets)
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Kullanici).WithMany(p => p.Sepets)
                .HasForeignKey(d => d.KullaniciId);
        });

        modelBuilder.Entity<Favoriler>(entity =>
        {
            entity.HasKey(e => e.FavoriId);
            entity.HasOne(d => d.Urun).WithMany(p => p.Favorilers)
                .HasForeignKey(d => d.UrunId);
            entity.HasOne(d => d.Kullanici).WithMany(p => p.Favorilers)
                .HasForeignKey(d => d.KullaniciId);
        });

        modelBuilder.Entity<SiparisDetaylari>(entity =>
        {
            entity.HasKey(e => e.SiparisDetayId);
            entity.Property(e => e.BirimFiyat).HasColumnType("decimal(10, 2)");
            entity.HasOne(d => d.Urun).WithMany(p => p.SiparisDetaylaris)
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Yorumlar>(entity =>
        {
            entity.HasKey(e => e.YorumId);
            entity.Property(e => e.Tarih).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.HasOne(d => d.Urun).WithMany(p => p.Yorumlars)
                .HasForeignKey(d => d.UrunId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}