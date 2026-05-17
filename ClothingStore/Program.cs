/* =================================================================================================================
 * DOSYA ADI: Program.cs (Uygulama Giriþ Noktasý ve Yapýlandýrma)
 * AMACI: Uygulamanýn baþlangýç ayarlarýný yapar, gerekli servisleri (Dependency Injection) sisteme kaydeder 
 * ve HTTP istek hattýný (Request Pipeline/Middleware) oluþturarak uygulamayý ayaða kaldýrýr.
 * * YAPILANDIRILAN SERVÝSLER (Dependency Injection):
 * - AddControllersWithViews: MVC (Model-View-Controller) mimarisinin çalýþmasý için gerekli servisleri yükler.
 * - AddDbContext: 'ClothingStoreDbContext' aracýlýðýyla veritabaný baðlantýsýný yönetir; 'appsettings.json' 
 * içerisindeki 'DefaultConnection' dizesini kullanarak SQL Server entegrasyonu saðlar.
 * - AddSession: Kullanýcý oturum verilerinin (Sepet miktarý, Kullanýcý ID vb.) sunucu tarafýnda tutulmasýný saðlar.
 * - AddHttpContextAccessor: Razor View'lar veya Controller dýþýndaki sýnýflar içinden mevcut HTTP baðlamýna 
 * (oturum verilerine, isteklere) eriþim yetkisi verir.
 * * MIDDLEWARE (ÝSTEK ÝÞLEME HATTI):
 * 1. Hata Yönetimi: Geliþtirme ortamý dýþýnda '/Home/Error' sayfasýna yönlendirme ve HSTS güvenlik protokolü aktivasyonu.
 * 2. Statik Dosyalar (UseStaticFiles): CSS, JavaScript ve resim dosyalarýnýn (/wwwroot) dýþ dünyaya açýlmasý.
 * 3. Oturum Yönetimi (UseSession): Kimlik doðrulama ve yetkilendirme öncesinde 'Session' verilerinin okunabilmesi.
 * 4. Yönlendirme (Routing): Gelen URL'leri 'Controller/Action/Id' þablonuna göre ilgili kod bloklarýna haritalama.
 * * TASARIM VE FONKSÝYONEL DETAYLAR:
 * - Esnek Baþlatma: Uygulamanýn hem yerel geliþtirme hem de canlý sunucu ortamýnda farklý güvenlik katmanlarýyla 
 * çalýþabilmesini saðlayan dinamik yapý.
 * - Varsayýlan Rota: Uygulama açýldýðýnda otomatik olarak 'Home' controller'ýndaki 'Index' action'ýnýn 
 * tetiklenmesini saðlayan merkezi rota tanýmý.
 ================================================================================================================= */

using ClothingStore.Models; 
using Microsoft.EntityFrameworkCore; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ClothingStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();