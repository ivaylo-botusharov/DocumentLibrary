using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentLibrary.Data.Context.EntityTypeConfiguration;
using DocumentLibrary.Data.Entities;
using DocumentLibrary.Infrastructure.AspNetHelpers.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DocumentLibrary.Data.Context
{
    public class DocumentLibraryContext : DbContext
    {
        private readonly IUserService _userService;
        public DocumentLibraryContext(DbContextOptions<DocumentLibraryContext> options, IUserService userService) : 
            base(options)
        {
            _userService = userService;
        }
        
        public DbSet<Book> Books { get; set; }
        
        public DbSet<Genre> Genres { get; set; }
        
        public DbSet<Keyword> Keywords { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new BookEntityTypeConfiguration().Configure(modelBuilder.Entity<Book>());
            new GenreEntityTypeConfiguration().Configure(modelBuilder.Entity<Genre>());
            new KeywordEntityTypeConfiguration().Configure(modelBuilder.Entity<Keyword>());

            foreach (var entityModelType in modelBuilder.Model.GetEntityTypes())
            {
                var entityType = entityModelType.ClrType;
                
                BuildAuditableColumns(modelBuilder, entityType);
                BuildSoftDeleteGlobalQueryFilter(modelBuilder, entityType);
            }
        }

        private void BuildSoftDeleteGlobalQueryFilter(ModelBuilder modelBuilder, Type entityType)
        {
            Expression<Func<bool>> deleted = () => false;
            var parameter = Expression.Parameter(entityType, "e");
            var bodyDeleted = Expression.Equal(
                left: Expression.Call(
                    typeof(EF),
                    nameof(EF.Property), 
                    new[] { typeof(bool) }, 
                    parameter, 
                    Expression.Constant("Deleted")),
                right: deleted.Body);
            
            modelBuilder.Entity(entityType).HasQueryFilter(Expression.Lambda(bodyDeleted, parameter));
        }

        private void BuildAuditableColumns(ModelBuilder modelBuilder, Type entityType)
        {
            modelBuilder.Entity(entityType)
                .Property<DateTime>(@"CreatedOn")
                .HasColumnName(@"CreatedOn")
                .IsRequired()
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<string>(@"CreatedBy")
                .HasColumnName(@"CreatedBy")
                .IsRequired()
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<DateTime?>(@"ModifiedOn")
                .HasColumnName(@"ModifiedOn")
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<string>(@"ModifiedBy")
                .HasColumnName(@"ModifiedBy")
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<DateTime?>(@"DeletedOn")
                .HasColumnName(@"DeletedOn")
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<string>(@"DeletedBy")
                .HasColumnName(@"DeletedBy")
                .ValueGeneratedNever();
                
            modelBuilder.Entity(entityType)
                .Property<bool>(@"Deleted")
                .HasColumnName(@"Deleted")
                .ValueGeneratedNever();
        }
        
        public override int SaveChanges()
        {
            SetAuditableProperties();

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SetAuditableProperties();
            
            return await base.SaveChangesAsync(true, cancellationToken);
        }

        private void SetAuditableProperties()
        {
            ChangeTracker.DetectChanges();

            foreach (var item in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                if (item.Property(AuditingColumn.CreatedOn.ToString()) != null)
                {
                    item.Property(AuditingColumn.CreatedOn.ToString()).CurrentValue = DateTime.Now;
                }

                if (item.Property(AuditingColumn.CreatedBy.ToString()) != null)
                {
                    string currentUserUsername = _userService.Username;

                    if (string.IsNullOrWhiteSpace(currentUserUsername))
                    {
                        throw new Exception("The username for CreatedBy cannot be null, empty or whitespace");
                    }

                    item.Property(AuditingColumn.CreatedBy.ToString()).CurrentValue = currentUserUsername;
                }
            }

            foreach (var item in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            {
                if (item.Property(AuditingColumn.ModifiedOn.ToString()) != null)
                {
                    item.Property(AuditingColumn.ModifiedOn.ToString()).CurrentValue = DateTime.Now;
                }

                if (item.Property(AuditingColumn.ModifiedBy.ToString()) != null)
                {
                    string currentUserUsername = _userService.Username;

                    if (string.IsNullOrWhiteSpace(currentUserUsername))
                    {
                        throw new Exception("The username for ModifiedBy cannot be null, empty or whitespace");
                    }

                    item.Property(AuditingColumn.ModifiedBy.ToString()).CurrentValue = currentUserUsername;
                }
            }

            foreach (var item in ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted))
            {
                if (item.Property(AuditingColumn.DeletedOn.ToString()) != null)
                {
                    item.Property(AuditingColumn.DeletedOn.ToString()).CurrentValue = DateTime.Now;
                }

                if (item.Property(AuditingColumn.DeletedBy.ToString()) != null)
                {
                    string currentUserUsername = _userService.Username;

                    if (string.IsNullOrWhiteSpace(currentUserUsername))
                    {
                        throw new Exception("The username for DeletedBy cannot be null, empty or whitespace");
                    }

                    item.Property(AuditingColumn.DeletedBy.ToString()).CurrentValue = currentUserUsername;
                }
            }
        }
        

        private enum AuditingColumn
        {
            CreatedOn,
            CreatedBy,
            ModifiedOn,
            ModifiedBy,
            DeletedOn,
            DeletedBy
        }
    }
}