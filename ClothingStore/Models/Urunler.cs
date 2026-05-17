using ClothingStore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models;

[Table("Urunler")]
public partial class Urunler
{
    [Key]
    public int UrunId { get; set; }

    public string UrunAdi { get; set; } = null!;

    public string? KategoriId { get; set; }

    public int? MarkaId { get; set; }

    public string? Renk { get; set; }

    public string? KumasTipi { get; set; }

    public string? Mevsim { get; set; }

    public decimal Fiyat { get; set; }

    public string? Aciklama { get; set; }

    public int? StokAdedi { get; set; }

    [NotMapped]
    public string StokDurumu
    {
        get
        {
            return StokAdedi <= 20 ? "Tükenmek Üzere" : "Stokta Var";
        }
    }

    public virtual ICollection<UrunBedenStok> UrunBedenStok { get; set; } = new List<UrunBedenStok>();

    public string? ResimYolu { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    [ForeignKey("MarkaId")]
    public virtual Markalar? Marka { get; set; }

    public virtual ICollection<Favoriler> Favorilers { get; set; } = new List<Favoriler>();

    public virtual ICollection<Sepet> Sepets { get; set; } = new List<Sepet>();

    public virtual ICollection<SiparisDetaylari> SiparisDetaylaris { get; set; } = new List<SiparisDetaylari>();

    public virtual ICollection<Yorumlar> Yorumlars { get; set; } = new List<Yorumlar>();
}