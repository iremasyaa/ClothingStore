using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models
{
    public class Kombinler
    {
        [Key]
        public int KombinId { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Lütfen kombinize bir isim verin.")]
        [StringLength(100)]
        public string KombinAdi { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ToplamTutar { get; set; }

        public virtual ICollection<KombinDetaylari> KombinDetaylari { get; set; }

        [ForeignKey("KullaniciId")]
        public virtual Kullanicilar Kullanici { get; set; }
    }
}