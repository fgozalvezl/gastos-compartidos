using GastosCompartidos.Models;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.Data;

/// <summary>
/// Contexto de Entity Framework Core sobre una base SQLite local.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Person> People => Set<Person>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryKeyword> CategoryKeywords => Set<CategoryKeyword>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<IncomeType> IncomeTypes => Set<IncomeType>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // --- Person ---
        b.Entity<Person>().Property(p => p.MonthlyIncome).HasPrecision(18, 2);

        // --- Category ---
        b.Entity<Category>().HasIndex(c => c.Name).IsUnique();

        // --- CategoryKeyword ---
        b.Entity<CategoryKeyword>()
            .HasOne(k => k.Category)
            .WithMany(c => c.Keywords)
            .HasForeignKey(k => k.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CategoryKeyword>().HasIndex(k => new { k.CategoryId, k.Keyword }).IsUnique();
        b.Entity<CategoryKeyword>().HasIndex(k => k.Keyword);

        // --- Expense ---
        b.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);
        b.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Expense>()
            .HasOne(e => e.PaidBy)
            .WithMany(p => p.ExpensesPaid)
            .HasForeignKey(e => e.PaidByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Expense>().HasIndex(e => e.Date);

        // --- ExpenseShare ---
        b.Entity<ExpenseShare>().Property(s => s.Amount).HasPrecision(18, 2);
        b.Entity<ExpenseShare>()
            .HasOne(s => s.Expense)
            .WithMany(e => e.Shares)
            .HasForeignKey(s => s.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<ExpenseShare>()
            .HasOne(s => s.Person)
            .WithMany(p => p.Shares)
            .HasForeignKey(s => s.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- IncomeType ---
        b.Entity<IncomeType>().HasIndex(t => t.Name).IsUnique();

        // --- Income ---
        b.Entity<Income>().Property(i => i.Amount).HasPrecision(18, 2);
        b.Entity<Income>()
            .HasOne(i => i.Person)
            .WithMany(p => p.Incomes)
            .HasForeignKey(i => i.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Income>()
            .HasOne(i => i.IncomeType)
            .WithMany(t => t.Incomes)
            .HasForeignKey(i => i.IncomeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Income>().HasIndex(i => i.Date);

        // --- AppSetting ---
        b.Entity<AppSetting>().HasIndex(s => s.Key).IsUnique();
    }
}
