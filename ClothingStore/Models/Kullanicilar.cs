

using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class Kullanicilar
{
    public int KullaniciId { get; set; }

    public string Ad { get; set; } = null!;

    public string Soyad { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Sifre { get; set; } = null!;

    public string? Telefon { get; set; }

    public string? Adres { get; set; }

    public string? Rol { get; set; } = null!;

    public DateTime? KayitTarihi { get; set; }

    public virtual ICollection<Favoriler> Favorilers { get; set; } = new List<Favoriler>();

    public virtual ICollection<Sepet> Sepets { get; set; } = new List<Sepet>();

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();

    public virtual ICollection<Yorumlar> Yorumlars { get; set; } = new List<Yorumlar>();
}
