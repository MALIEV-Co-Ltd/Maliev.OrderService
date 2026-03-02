$ErrorActionPreference = 'Stop'
Set-Location "B:\maliev\Maliev.OrderService"

# Restore configurations from git with namespace transforms
$configs = @(
    'AuditLogConfiguration',
    'NotificationSubscriptionConfiguration',
    'Order3DDesignAttributesConfiguration',
    'Order3DPrintingAttributesConfiguration',
    'Order3DScanningAttributesConfiguration',
    'OrderCncMachiningAttributesConfiguration',
    'OrderConfiguration',
    'OrderFileConfiguration',
    'OrderNoteConfiguration',
    'OrderSheetMetalAttributesConfiguration',
    'OrderStatusConfiguration',
    'ProcessTypeConfiguration',
    'ServiceCategoryConfiguration'
)

foreach ($c in $configs) {
    $content = git show "HEAD:Maliev.OrderService.Data/Configurations/${c}.cs" 2>&1
    $content = $content -replace 'Maliev\.OrderService\.Data\.Configurations', 'Maliev.OrderService.Infrastructure.Persistence.Configurations'
    $content = $content -replace 'Maliev\.OrderService\.Data\.Models', 'Maliev.OrderService.Domain.Entities'
    $content = $content -replace 'using Maliev\.OrderService\.Data;', ''
    Set-Content -Path "Maliev.OrderService.Infrastructure/Persistence/Configurations/${c}.cs" -Value $content -Encoding UTF8
    Write-Host "Config: $c"
}

# Restore OrderDbContext
$ctx = git show "HEAD:Maliev.OrderService.Data/OrderDbContext.cs" 2>&1
$ctx = $ctx -replace 'using Maliev\.OrderService\.Data\.Configurations;', 'using Maliev.OrderService.Infrastructure.Persistence.Configurations;'
$ctx = $ctx -replace 'using Maliev\.OrderService\.Data\.Models;', 'using Maliev.OrderService.Domain.Entities;'
$ctx = $ctx -replace 'namespace Maliev\.OrderService\.Data', 'namespace Maliev.OrderService.Infrastructure.Persistence'
Set-Content -Path "Maliev.OrderService.Infrastructure/Persistence/OrderDbContext.cs" -Value $ctx -Encoding UTF8
Write-Host "Done: OrderDbContext"

# Restore OrderDbContextFactory
$factory = git show "HEAD:Maliev.OrderService.Data/OrderDbContextFactory.cs" 2>&1
$factory = $factory -replace 'namespace Maliev\.OrderService\.Data', 'namespace Maliev.OrderService.Infrastructure.Persistence'
Set-Content -Path "Maliev.OrderService.Infrastructure/Persistence/OrderDbContextFactory.cs" -Value $factory -Encoding UTF8
Write-Host "Done: OrderDbContextFactory"

# Restore migrations
$migrations = @(
    '20260106142146_InitialCreate.Designer',
    '20260106142146_InitialCreate',
    '20260111110147_UpdateConcurrencyAndNaming.Designer',
    '20260111110147_UpdateConcurrencyAndNaming',
    '20260216133023_AddCustomerPoFieldsToOrder.Designer',
    '20260216133023_AddCustomerPoFieldsToOrder',
    'OrderDbContextModelSnapshot'
)

foreach ($m in $migrations) {
    $content = git show "HEAD:Maliev.OrderService.Data/Migrations/${m}.cs" 2>&1
    $content = $content -replace 'Maliev\.OrderService\.Data\.Migrations', 'Maliev.OrderService.Infrastructure.Persistence.Migrations'
    $content = $content -replace 'Maliev\.OrderService\.Data', 'Maliev.OrderService.Infrastructure.Persistence'
    Set-Content -Path "Maliev.OrderService.Infrastructure/Persistence/Migrations/${m}.cs" -Value $content -Encoding UTF8
    Write-Host "Migration: $m"
}

Write-Host "`nAll files restored successfully!"
