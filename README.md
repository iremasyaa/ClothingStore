# ClothingStore — Personalized E-Commerce & Hybrid Match-Up Platform

ClothingStore is a full-featured web application designed to go beyond traditional e-commerce platforms by introducing an innovative **"Hybrid Outfit Studio"** experience. The platform allows users not only to purchase new clothing retail products but also to digitize their physical wardrobe by uploading their personal apparel to mix and match items on a single screen. This integration aims to promote conscious consumption and deliver a smarter, highly personalized shopping experience.

## 🚀 Core Features

### 🔹 Hybrid Outfit Studio (Digital Wardrobe Integration)
* **Personal Digital Wardrobe:** Users can photograph and upload their physical clothes into the system. These personal items are securely archived, categorized, filtered, and can be edited anytime.
* **Interactive Virtual Mannequin:** Users can display a newly selected retail product from the storefront alongside an item from their digital wardrobe on a virtual mannequin to visually test aesthetic compatibility in real time.
* **Outfit Archive Management:** Customers can save their custom combinations to their profiles to build and refine their personal style catalogs.
* **Integrated Purchasing:** Users can add all retail store items featured within a saved outfit combination to the shopping cart and complete the checkout process with a single click.

### 🔹 E-Commerce & Administrative Operations
* **Multi-Role Management:** Advanced user authorization infrastructure built upon "Administrator (Admin) - Customer" roles.
* **Comprehensive Admin Panel:** Advanced CRUD operations for administrators to manage products, inventory, categories, brands, and order tracking.
* **Responsive Interface:** A modern user interface optimized to display perfectly (100% responsive) across mobile, tablet, and desktop viewports.
* **Advanced Search & Filtering:** High-performance product search combined with dynamic, asynchronous category, size, and brand-based filtering and sorting algorithms.

## 🛠️ Tech Stack

* **Backend Framework:** ASP.NET Core MVC (C#)
* **Database & ORM:** Microsoft SQL Server (MSSQL) & Entity Framework Core (LINQ)
* **Frontend:** HTML5, CSS3, JavaScript (AJAX & JSON for asynchronous actions)
* **UI & Design Libraries:** Bootstrap 5 & FontAwesome
* **State Management:** ASP.NET Core Session for authentication and shopping cart persistence

## 🗄️ Database Architecture & SQL Engineering

The database layer is deeply structured on Microsoft SQL Server using an Entity Framework Core approach to maximize data integrity and optimize query performance:

* **Relational Table Architecture:** Designed with 15 interconnected tables using junction tables for many-to-many relationships to prevent data redundancy and ensure data normalization:
  `Urunler`, `Kategoriler`, `Markalar`, `Bedenler`, `UrunBedenStok`, `Kullanicilar`, `Siparisler`, `SiparisDetaylari`, `Sepet`, `Favoriler`, `Yorumlar`, `Dolap`, `Kombinler`, `KombinDetaylari`, `KargoFirmalari`.
* **Database Triggers:** Implemented server-side to ensure real-time data consistency:
  * `TRG_StoktanDus`: Automatically decrements inventory quantities from the stock once an order is placed.
  * `trg_UrunToplamStokGuncelle`: Automatically synchronizes general product stock levels in real time.
* **Stored Procedures:** Critical backend queries are pre-compiled server-side to enhance application velocity and completely mitigate SQL Injection vulnerabilities:
  * Implemented procedures: `sp_SiparisOlustur`, `sp_UrunAra`, `sp_SepeteEkle`, `sp_YorumEkle`, `sp_AdminIstatistik`, `sp_UrunFiltrele`, `sp_UyeKayit`.
* **Functions & Views:**
  * Modular logic and calculations are handled via database functions: `fn_OrtalamaPuan`, `fn_StokDurum`, and `fn_SiparisDurum`.
  * The storefront item display is optimized through the `View_AktifUrunler` database view to prevent unnecessary data overhead.
