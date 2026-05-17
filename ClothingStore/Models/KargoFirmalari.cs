
using System;
using System.Collections.Generic;

namespace ClothingStore.Models;

public partial class KargoFirmalari
{
    public int KargoId { get; set; }

    public string FirmaAdi { get; set; } = null!;

    public virtual ICollection<Siparisler> Siparislers { get; set; } = new List<Siparisler>();
}
