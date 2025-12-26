# OrderService Implementation Plan - Permission-Based Authorization Migration

## Phase 1: Define Permissions & Roles (2 hours)
- Create OrderPermissions.cs
- Create OrderPredefinedRoles.cs
- Define ~15 permissions
- Define 5 roles

## Phase 2: IAM Registration (2 hours)
- Create OrderIAMRegistrationService.cs
- Update Program.cs
- Add IAM configuration
- Test registration

## Phase 3: Update Controllers (3 hours)
- Update OrdersController
- Update LineItemsController
- Replace all [Authorize(Policy)] with [RequirePermission]
- Remove policy configurations

## Phase 4: Update Tests (4 hours)
- Update integration tests
- Add permission-specific tests
- Test role combinations

## Phase 5: Deploy & Verify (2 hours)
- Deploy with feature flag OFF
- Enable feature flag
- Manual testing
- Production rollout

**Total: ~13 hours (~2 days)**
