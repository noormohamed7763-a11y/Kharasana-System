document.addEventListener("DOMContentLoaded", function () {
    const wrapper = document.querySelector(".wrapper");
    const desktopToggle = document.getElementById("sidebarToggle");
    const mobileToggle = document.getElementById("sidebarMobileToggle");

    const isMobileViewport = () => window.innerWidth <= 992;

    const updateAriaExpanded = (isOpen) => {
        desktopToggle?.setAttribute("aria-expanded", isMobileViewport() ? String(isOpen) : "false");
        mobileToggle?.setAttribute("aria-expanded", String(isOpen));
    };

    const syncAriaState = () => {
        const isOpen = wrapper.classList.contains("sidebar-mobile-open");
        updateAriaExpanded(isOpen);
    };

    // Desktop: الـ Sidebar ثابت وموسّع دائماً (260px) — لا نستعيد الحالة المحفوظة.
    // Mobile: لا نطبق sidebar-collapsed أبداً (يتعارض مع off-canvas sidebar-mobile-open).
    // نزيل أي حالة مطوية متبقية من جلسة سابقة على كلا النوعين.
    //
    // ولا نكتب شيئاً في localStorage: الحالة لا تُقرأ في أي موضع (الحذف أعلاه هو كل
    // الاستعمال)، وكانت الكتابة بلا try/catch تُرمي SecurityError حين يُحجب تخزين
    // الموقع (وضع خاص/حجب بيانات) فيتوقف معالج DOMContentLoaded كاملاً — أي يتعطل
    // زر الشريط الجانبي على الجوال مع كل معالجات الإغلاق.
    wrapper.classList.remove("sidebar-collapsed");
    syncAriaState();

    desktopToggle?.addEventListener("click", function () {
        // على الشاشات الصغيرة: يُفتح/يُغلق شريط التنقل المنزلق
        if (isMobileViewport()) {
            wrapper.classList.toggle("sidebar-mobile-open");
            syncAriaState();
        }
        // على سطح المكتب: لا شيء — الـ Sidebar موسّع دائماً (لا طي ولا توسيع)
    });

    // Mobile off-canvas toggle
    mobileToggle?.addEventListener("click", function () {
        wrapper.classList.toggle("sidebar-mobile-open");
        syncAriaState();
    });

    // Close mobile sidebar when clicking outside
    document.addEventListener("click", function (e) {
        if (
            wrapper.classList.contains("sidebar-mobile-open") &&
            !e.target.closest(".sidebar") &&
            !e.target.closest("#sidebarMobileToggle") &&
            !e.target.closest("#sidebarToggle")
        ) {
            wrapper.classList.remove("sidebar-mobile-open");
            syncAriaState();
        }
    });

    // Close mobile sidebar with Escape key
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && wrapper.classList.contains("sidebar-mobile-open")) {
            wrapper.classList.remove("sidebar-mobile-open");
            syncAriaState();
            desktopToggle?.focus();
        }
    });

    // Handle viewport resize: ensure correct state when crossing 992px breakpoint
    window.addEventListener("resize", function () {
        if (isMobileViewport()) {
            // On mobile: remove collapsed, use off-canvas only
            wrapper.classList.remove("sidebar-collapsed");
        }
        // On desktop: لا يوجد collapsed — الـ Sidebar موسّع دائماً
        if (!isMobileViewport() && wrapper.classList.contains("sidebar-mobile-open")) {
            wrapper.classList.remove("sidebar-mobile-open");
        }
        syncAriaState();
    });
});