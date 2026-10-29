# Senior Code Review — Web Layer (Updated)

**Review Date:** 2026-09-20 | **Verification Date:** 2026-10-29

---

## 1. Executive Summary

| Area | Grade | Notes |
|------|-------|-------|
| Architecture & Layering | **A-** | Sound pattern; issues with DTO duplication remain. |
| Security | **A** | **Upgraded from C+**. Critical XSS and Mass Assignment issues fixed. |
| Error Handling | **B+** | Consistent API exception handling. |
| API Design | **B+** | RESTful; DTOs remain duplicated. |
| Code Quality | **B+** | **Upgraded**. CSP-safe JS patterns implemented. |
| Performance | **B+** | **Upgraded**. Concurrent token issue resolved in `ApiClient`. |
| Testability | **C** | No unit tests for Web layer. |

---

## 2. Status of Critical Issues (Verified Fixes)

All critical vulnerabilities identified in the initial review have been verified as resolved:

1.  **[FIXED] DOM-based XSS in `site.js` `showToast()`**:
    -   Verified that `innerHTML` has been replaced with `textContent` for dynamic parameters.
2.  **[FIXED] Mass assignment in `OrdersController.Create()`**:
    -   Verified that the controller explicitly overrides `model.FactoryId` from the session context, preventing privilege escalation.
3.  **[FIXED] Token race condition in `ApiClient`**:
    -   Verified that the `ApiClient` now adds the `Authorization` header to individual `HttpRequestMessage` instances instead of modifying the shared `HttpClient.DefaultRequestHeaders`.

---

## 3. Status of High/Medium Issues

-   **[FIXED] CSP Weakness**: Removed `'unsafe-inline'` and implemented a nonce-based CSP in `SecurityHeadersMiddleware.cs`.
-   **[FIXED] Inline Event Handlers**: Replaced inline handlers (`onclick`, `oninput`, etc.) in JS and views with `data-*` attribute delegation in `site.js`.
-   **[IN PROGRESS / REMAINS] DTO Duplication**: Duplicated DTOs between Web and Application layers remain.
-   **[IN PROGRESS / REMAINS] Inconsistent Exception Handling**: Minor inconsistencies persist in `ClientsController`.

---

## 4. Recommendations (Updated)

### P0 — COMPLETED (Security)
1.  **Done.** Fixed DOM-based XSS in `site.js`.
2.  **Done.** Fixed Mass Assignment in `OrdersController`.
3.  **Done.** Fixed Token race condition in `ApiClient`.

### P1 — Priority Fixes (Remaining)
4.  **Consolidate duplicate DTOs**: Remove Web-layer copies of `OrderDto`, `AdminDashboardDto`, `FactoryDashboardDto`, `FactoryDto`.
5.  **Address lingering ViewModels**: Ensure all status validation logic (previously flagged as magic numbers) is correctly moved to domain/application layers.
6.  **Add Test Coverage**: Implement unit tests for core Web controllers and services.

---

*This report reflects verified status as of 2026-10-29.*
🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Code <noreply@anthropic.com>
