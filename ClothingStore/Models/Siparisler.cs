

using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class Siparisler
{
    public int SiparisId { get; set; }

    public int KullaniciId { get; set; }

    public DateTime? Tarih { get; set; }

    public decimal ToplamTutar { get; set; }

    public string? Durum { get; set; }

    public int? KargoId { get; set; }

    public string? OdemeTipi { get; set; }

    public string TeslimatAdresi { get; set; } = null!;

    public virtual KargoFirmalari? Kargo { get; set; }

    public virtual Kullanicilar Kullanici { get; set; } = null!;

    public virtual ICollection<SiparisDetaylari> SiparisDetaylaris { get; set; } = new List<SiparisDetaylari>();
}
