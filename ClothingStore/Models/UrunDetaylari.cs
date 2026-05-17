

using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class UrunDetaylari
{
    public int UrunId { get; set; }

    public string UrunAdi { get; set; } = null!;

    public decimal Fiyat { get; set; }

    public string? ResimYolu { get; set; }

    public string? Ozet { get; set; }

    public int SayfaSayisi { get; set; }

    public int KategoriId { get; set; }

    public int YazarId { get; set; }

    public int YayineviId { get; set; }

    public string? StokDurumu { get; set; }

    public decimal? OrtalamaPuan { get; set; }

    public string YazarAdi { get; set; } = null!;

    public string YayineviAdi { get; set; } = null!;

    public string KategoriAdi { get; set; } = null!;
}
