

using ClothingStore.Models;
using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class Yorumlar
{
    public int YorumId { get; set; }

    public int KullaniciId { get; set; }

    public int UrunId { get; set; }

    public string? YorumMetni { get; set; }

    public int? Puan { get; set; }

    public DateTime? Tarih { get; set; }

    public bool? OnayDurumu { get; set; }

    public virtual Urunler Urun { get; set; } = null!;

    public virtual Kullanicilar Kullanici { get; set; } = null!;
}