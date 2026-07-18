using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services;

internal static class OrderReferenceDataSeeder
{
    public static async Task SeedAsync(OrderDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureServiceCategoryAsync(
            dbContext,
            1,
            "3D Printing",
            "3D printing manufacturing services.",
            cancellationToken);

        await EnsureServiceCategoryAsync(
            dbContext,
            2,
            "CNC Machining",
            "CNC machining manufacturing services.",
            cancellationToken);

        await EnsureProcessTypeAsync(
            dbContext,
            1,
            1,
            "FDM",
            "Fused deposition modeling.",
            cancellationToken);

        await EnsureProcessTypeAsync(
            dbContext,
            2,
            1,
            "SLA",
            "Stereolithography.",
            cancellationToken);

        await EnsureProcessTypeAsync(
            dbContext,
            7,
            2,
            "Laser Cutting",
            "Laser cutting process.",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await ResetIdentitySequencesAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureServiceCategoryAsync(
        OrderDbContext dbContext,
        int categoryId,
        string name,
        string description,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ServiceCategories
            .AnyAsync(category => category.CategoryId == categoryId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ServiceCategories.Add(new ServiceCategory
        {
            CategoryId = categoryId,
            Name = name,
            Description = description,
            IsActive = true,
        });
    }

    private static async Task EnsureProcessTypeAsync(
        OrderDbContext dbContext,
        int processTypeId,
        int serviceCategoryId,
        string name,
        string description,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ProcessTypes
            .AnyAsync(processType => processType.ProcessTypeId == processTypeId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ProcessTypes.Add(new ProcessType
        {
            ProcessTypeId = processTypeId,
            ServiceCategoryId = serviceCategoryId,
            Name = name,
            Description = description,
            IsActive = true,
        });
    }

    private static async Task ResetIdentitySequencesAsync(
        OrderDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(
                pg_get_serial_sequence('service_categories', 'category_id'),
                GREATEST((SELECT MAX(category_id) FROM service_categories), 1),
                true)
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(
                pg_get_serial_sequence('process_types', 'process_type_id'),
                GREATEST((SELECT MAX(process_type_id) FROM process_types), 1),
                true)
            """,
            cancellationToken);
    }
}
