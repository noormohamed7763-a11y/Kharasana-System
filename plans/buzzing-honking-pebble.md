# Plan: Fix Test Failures After N+1 Optimization

## Context
The recent optimization of `OrderRepository.GetPagedAsync` to use projection (`OrderDto`) caused failures in existing tests that were still expecting entity properties (`Order`).
I fixed the assertions in the main integration and unit tests, but three other tests are failing:
1. `ValidatorWiringTests`: Likely an issue with DTO validation registration.
2. `FormValidationScriptsTests`: Likely due to changes in views or validation wiring.
3. `ConcreteTypeServiceTests`: `ConcreteTypeNameIndex` mismatch in the index filter.

I need to investigate these three issues to ensure they are addressed correctly, without regressing the main N+1 fix.

## Phase 1: Exploration
- Investigate `ValidatorWiringTests.cs` to understand why `RegisterFactoryDto` validator is reported as unreachable.
- Investigate `FormValidationScriptsTests.cs` to see why the `Create.cshtml` view is failing validation script checks.
- Investigate `ConcreteTypeServiceTests.cs` to understand why the index filter test fails and how it relates to the recent changes (if at all).

## Phase 2: Implementation Design
- Based on findings, determine if these are genuine bugs or side effects that need updated test expectations.
- Propose minimal, clean fixes.

## Phase 3: Verification
- Run all tests to confirm all pass.
