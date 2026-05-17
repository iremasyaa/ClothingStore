using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace ClothingStore.Models;

public partial class SiparisDetaylari
{
    [Key]
    [Column("DetayID")]
    public int SiparisDetayId { get; set; }

    [Column("SiparisID")]
    public int SiparisId { get; set; }

    [Column("UrunId")]
    public int UrunId { get; set; }

    [ForeignKey("BedenId")]
    public virtual Bedenler? Beden { get; set; }

    public int? BedenId { get; set; }

    public int Adet { get; set; }

    public decimal BirimFiyat { get; set; }

    public virtual Urunler Urun { get; set; } = null!;
    public virtual Siparisler Siparis { get; set; } = null!;
}