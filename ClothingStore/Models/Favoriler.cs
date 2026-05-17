

using ClothingStore.Models;
using System;
using System.Collections.Generic;

namespace ClothingStore.Models; 

public partial class Favoriler
{
    public int FavoriId { get; set; }

    public int KullaniciId { get; set; }

    public int UrunId { get; set; }

    public virtual Urunler Urun { get; set; } = null!;

    public virtual Kullanicilar Kullanici { get; set; } = null!;
}