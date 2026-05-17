

using ClothingStore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models;

public partial class Sepet
{
    public int SepetId { get; set; }

    public int KullaniciId { get; set; }

    public int UrunId { get; set; }

    public int? Adet { get; set; }

    public int? BedenId { get; set; }

    [ForeignKey("BedenId")]
    public virtual Bedenler? Beden { get; set; }

    public DateTime? EklenmeTarihi { get; set; }

    public virtual Urunler Urun { get; set; } = null!;

    public virtual Kullanicilar Kullanici { get; set; } = null!;
}