using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models
{
    public class Dolap
    {
        [Key]
        public int DolapId { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Lütfen bir isim verin")]
        public string UrunAdi { get; set; }

        [Required(ErrorMessage = "Lütfen kategori seçin")]
        public string KategoriId { get; set; }

        public string ResimYolu { get; set; }

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        public virtual Kategoriler Kategori { get; set; }
    }
}