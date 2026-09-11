document.addEventListener("DOMContentLoaded", function () {
    const wrapper = document.querySelector(".wrapper");
    const desktopToggle = document.getElementById("sidebarToggle");
    const mobileToggle = document.getElementById("sidebarMobileToggle");
    const STORAGE_KEY = "kharasana_sidebar_collapsed";

    const isMobileViewport = () => window.innerWidth <= 992;

    // Desktop: الـ Sidebar ثابت وموسّع دائماً (260px) — لا نستعيد الحالة المحفوظة.
    // Mobile: لا نطبق sidebar-collapsed أبداً (يتعارض مع off-canvas sidebar-mobile-open).
    // نزيل أي حالة مطوية متبقية من جلسة سابقة على كلا النوعين.
    wrapper.classList.remove("sidebar-collapsed");
    localStorage.setItem(STORAGE_KEY, "0");

    desktopToggle?.addEventListener("click", function () {
        // على الشاشات الصغيرة: يُفتح/يُغلق شريط التنقل المنزلق
        if (isMobileViewport()) {
            wrapper.classList.toggle("sidebar-mobile-open");
        }
        // على سطح المكتب: لا شيء — الـ Sidebar موسّع دائماً (لا طي ولا توسيع)
    });

    // Mobile off-canvas toggle
    mobileToggle?.addEventListener("click", function () {
        wrapper.classList.toggle("sidebar-mobile-open");
    });

    // Close mobile sidebar when clicking outside
    document.addEventListener("click", function (e) {
        if (
            wrapper.classList.contains("sidebar-mobile-open") &&
            !e.target.closest(".sidebar") &&
            !e.target.closest("#sidebarMobileToggle")
        ) {
            wrapper.classList.remove("sidebar-mobile-open");
        }
    });

    // Handle viewport resize: ensure correct state when crossing 992px breakpoint
    window.addEventListener("resize", function () {
        if (isMobileViewport()) {
            // On mobile: remove collapsed, use off-canvas only
            wrapper.classList.remove("sidebar-collapsed");
            localStorage.setItem(STORAGE_KEY, "0");
        }
        // On desktop: لا يوجد collapsed — الـ Sidebar موسّع دائماً
    });
});