using ClothingStore.Models;
using System.Collections.Generic;

namespace ClothingStore.Models 
{
    public class AnasayfaViewModel
    {
        public List<Urunler> YeniGelenler { get; set; }
        public List<Urunler> CokSatanlar { get; set; }
        public List<Markalar> Markalar { get; set; }
        public List<Kategoriler> Kategoriler { get; set; }
    }
}