using Microsoft.EntityFrameworkCore;
using VeriFinans.Models;

namespace VeriFinans.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- KATEGORİ SEED DATA (SİMETRİK VE DÜZENLİ YAPI) ---

            modelBuilder.Entity<Category>().HasData(
                // --- LEVEL 1: ANA KATEGORİLER ---
                new Category { Id = 1, Name = "Gençler Apt.", Type = 1, Level = 1 },
                new Category { Id = 2, Name = "Tekirdağ", Type = 1, Level = 1 },
                new Category { Id = 3, Name = "Özberk Apt.", Type = 1, Level = 1 },
                new Category { Id = 4, Name = "Market", Type = 1, Level = 1 },
                new Category { Id = 5, Name = "Kasap", Type = 1, Level = 1 },
                new Category { Id = 6, Name = "Sigorta", Type = 1, Level = 1 },
                new Category { Id = 7, Name = "Vergi", Type = 1, Level = 1 },
                new Category { Id = 8, Name = "Eczane", Type = 1, Level = 1 },
                new Category { Id = 9, Name = "Kediler", Type = 1, Level = 1 },
                new Category { Id = 10, Name = "Telefonlar", Type = 1, Level = 1 },
                new Category { Id = 11, Name = "Araba", Type = 1, Level = 1 },
                new Category { Id = 100, Name = "Maaş", Type = 0, Level = 1 }, // Gelir örneği

                // --- LEVEL 2: ALT GRUPLAR ---
                // Sigorta Altları
                new Category { Id = 12, Name = "Gençler", ParentId = 6, Type = 1, Level = 2 },
                new Category { Id = 13, Name = "Yazlık", ParentId = 6, Type = 1, Level = 2 },
                new Category { Id = 14, Name = "Özberk", ParentId = 6, Type = 1, Level = 2 },

                // Telefon Altları
                new Category { Id = 15, Name = "Sercan", ParentId = 10, Type = 1, Level = 2 },
                new Category { Id = 16, Name = "Hakkı", ParentId = 10, Type = 1, Level = 2 },
                new Category { Id = 17, Name = "Ayşem", ParentId = 10, Type = 1, Level = 2 },

                // Araba Altları (Plakalar)
                new Category { Id = 18, Name = "FR 2104", ParentId = 11, Type = 1, Level = 2 },
                new Category { Id = 19, Name = "KC 105", ParentId = 11, Type = 1, Level = 2 },

                // --- LEVEL 3: İŞLEM TÜRLERİ ---

                // Gençler Apt İşlemleri (Tüm liste)
                new Category { Id = 20, Name = "Aidat", ParentId = 1, Type = 1, Level = 2 },
                new Category { Id = 21, Name = "Doğalgaz", ParentId = 1, Type = 1, Level = 2 },
                new Category { Id = 22, Name = "Elektrik", ParentId = 1, Type = 1, Level = 2 },
                new Category { Id = 23, Name = "Su", ParentId = 1, Type = 1, Level = 2 },
                new Category { Id = 24, Name = "İnternet", ParentId = 1, Type = 1, Level = 2 },

                // Tekirdağ İşlemleri (Gençler ile aynı yapıldı)
                new Category { Id = 25, Name = "Aidat", ParentId = 2, Type = 1, Level = 2 },
                new Category { Id = 26, Name = "Doğalgaz", ParentId = 2, Type = 1, Level = 2 },
                new Category { Id = 33, Name = "Elektrik", ParentId = 2, Type = 1, Level = 2 },
                new Category { Id = 34, Name = "Su", ParentId = 2, Type = 1, Level = 2 },
                new Category { Id = 35, Name = "İnternet", ParentId = 2, Type = 1, Level = 2 },

                // Özberk Apt. İşlemleri (Gençler ile aynı yapıldı)
                new Category { Id = 36, Name = "Aidat", ParentId = 3, Type = 1, Level = 2 },
                new Category { Id = 37, Name = "Doğalgaz", ParentId = 3, Type = 1, Level = 2 },
                new Category { Id = 38, Name = "Elektrik", ParentId = 3, Type = 1, Level = 2 },
                new Category { Id = 39, Name = "Su", ParentId = 3, Type = 1, Level = 2 },
                new Category { Id = 40, Name = "İnternet", ParentId = 3, Type = 1, Level = 2 },

                // Sigorta Detayları
                new Category { Id = 27, Name = "DASK", ParentId = 12, Type = 1, Level = 3 },
                new Category { Id = 28, Name = "Yangın", ParentId = 12, Type = 1, Level = 3 },

                // Araba 1 (FR 2104) Detayları
                new Category { Id = 29, Name = "Kasko", ParentId = 18, Type = 1, Level = 3 },
                new Category { Id = 30, Name = "Trafik Sigortası", ParentId = 18, Type = 1, Level = 3 },
                new Category { Id = 31, Name = "MTV", ParentId = 18, Type = 1, Level = 3 },
                new Category { Id = 32, Name = "Benzin", ParentId = 18, Type = 1, Level = 3 },

                // Araba 2 (KC 105) Detayları (FR 2104 ile aynı yapıldı)
                new Category { Id = 41, Name = "Kasko", ParentId = 19, Type = 1, Level = 3 },
                new Category { Id = 42, Name = "Trafik Sigortası", ParentId = 19, Type = 1, Level = 3 },
                new Category { Id = 43, Name = "MTV", ParentId = 19, Type = 1, Level = 3 },
                new Category { Id = 44, Name = "Benzin", ParentId = 19, Type = 1, Level = 3 }
            );

            // İlişkileri netleştirelim (Self-referencing Category)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}