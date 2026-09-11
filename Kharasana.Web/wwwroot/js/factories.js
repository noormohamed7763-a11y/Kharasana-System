document.addEventListener("DOMContentLoaded", () => {

    initializeSearch();
    initializeCreateAccountForm();
    initializeLogoUpload();

});

// ==============================
// البحث داخل جدول المصانع
// ==============================
function initializeSearch() {

    const searchInput = document.getElementById("factorySearchInput");

    if (!searchInput)
        return;

    searchInput.addEventListener("input", function () {

        const filter = this.value.trim().toLowerCase();

        document.querySelectorAll("#factoriesTable tbody tr[data-status]")
            .forEach(row => {

                const text = row.textContent.toLowerCase();

                row.style.display = text.includes(filter)
                    ? ""
                    : "none";
            });
    });
}

// ==============================
// إنشاء حساب مصنع
// ==============================
function initializeCreateAccountForm() {

    const form = document.getElementById("createAccountForm");

    if (!form)
        return;

    form.addEventListener("submit", submitCreateAccount);
}

async function submitCreateAccount(e) {

    e.preventDefault();

    hideError();

    const factoryId = document.getElementById("modalFactoryId").value;
    const fullName = document.getElementById("modalFullName").value.trim();
    const email = document.getElementById("modalEmail").value.trim();
    const phone = document.getElementById("modalPhone").value.trim();
    const password = document.getElementById("modalPassword").value;
    const confirmPassword = document.getElementById("modalConfirmPassword").value;

    if (password.length < 6) {
        showError("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");
        return;
    }

    if (password !== confirmPassword) {
        showError("كلمة المرور وتأكيدها غير متطابقين.");
        return;
    }

    const formData = {
        factoryId: Number(factoryId),
        fullName,
        email,
        phone,
        password,
        confirmPassword,
        role: 1
    };

    try {

        const response = await fetch("/Factories/CreateFactoryAccount", {

            method: "POST",

            credentials: "same-origin",

            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },

            body: JSON.stringify(formData)

        });

        let result = {};

        try {
            result = await response.json();
        }
        catch {
            result = {};
        }

        if (response.ok && (result.success || result.Success)) {

            const modalElement = document.getElementById("createAccountModal");

            if (modalElement) {
                bootstrap.Modal.getInstance(modalElement)?.hide();
            }

            location.reload();

            return;
        }

        showError(
            result.message ||
            result.Message ||
            "تعذر إنشاء الحساب."
        );

    }
    catch (error) {

        console.error(error);

        showError("تعذر الاتصال بالخادم. تأكد من سلامة الاتصال.");
    }
}

// ==============================
// تصفية حسب الحالة
// ==============================
function filterByStatus(status) {

    document.querySelectorAll("#factoriesTable tbody tr[data-status]")
        .forEach(row => {

            const rowStatus = row.dataset.status;

            row.style.display =
                status === "all" || rowStatus === status
                    ? ""
                    : "none";
        });
}

// ==============================
// فتح نافذة إنشاء الحساب
// ==============================
function openCreateAccountModal(factoryId, factoryName) {

    const form = document.getElementById("createAccountForm");

    form?.reset();

    document.getElementById("modalFactoryId").value = factoryId;

    document.getElementById("modalFullName").value =
        `${factoryName} - مدير`;

    hideError();

    const modal = new bootstrap.Modal(
        document.getElementById("createAccountModal")
    );

    modal.show();
}

// ==============================
// رسائل الخطأ
// ==============================
function showError(message) {

    const errorDiv = document.getElementById("modalErrorMessage");

    if (!errorDiv)
        return;

    errorDiv.textContent = message;

    errorDiv.classList.remove("d-none");
}

function hideError() {

    const errorDiv = document.getElementById("modalErrorMessage");

    if (!errorDiv)
        return;

    errorDiv.textContent = "";

    errorDiv.classList.add("d-none");
}

// ==============================
// ✅ رفع شعار المصنع (مع معاينة)
// ==============================
function initializeLogoUpload() {

    const fileInput = document.getElementById("logoFile");

    if (!fileInput)
        return;

    const uploadBtn = document.getElementById("uploadLogoBtn");

    fileInput.addEventListener("change", function () {

        if (this.files && this.files.length > 0) {

            if (uploadBtn) {
                uploadBtn.disabled = false;
            }

            // معاينة الصورة قبل الرفع
            const reader = new FileReader();
            reader.onload = function (e) {
                const preview = document.getElementById("logoPreview");
                if (preview) {
                    preview.src = e.target.result;
                }
            };
            reader.readAsDataURL(this.files[0]);

        } else {

            if (uploadBtn) {
                uploadBtn.disabled = true;
            }

        }

    });

}

// ==============================
// ✅ حذف شعار المصنع (مع تأكيد)
// ==============================
function confirmDeleteLogo() {
    return confirm("هل أنت متأكد من حذف شعار المصنع؟");
}

// ==============================
// ✅ عرض الشعار في الجدول — عند فشل تحميل الصورة
// نستبدلها بظهيرة الأحرف الأولى من اسم المصنع (نفس نمط الصف بدون شعار)
// بدل صورة افتراضية موحدة تجعل كل الصفوف متشابهة.
// ==============================
function handleLogoError(imgElement) {
    imgElement.onerror = null;

    const factoryName = imgElement.getAttribute('data-name') || '';
    const initials = factoryName.trim().slice(0, 2) || '--';

    const avatar = document.createElement('div');
    avatar.className = 'factory-avatar bg-light border rounded-circle d-flex align-items-center justify-content-center text-primary fw-bold';
    avatar.style.cssText = 'width:40px;height:40px;font-size:14px;';
    avatar.setAttribute('aria-label', imgElement.getAttribute('alt') || 'شعار المصنع');
    avatar.textContent = initials;

    imgElement.replaceWith(avatar);
}

// جعل الدوال متاحة عالمياً
window.openCreateAccountModal = openCreateAccountModal;
window.filterByStatus = filterByStatus;
window.confirmDeleteLogo = confirmDeleteLogo;
window.handleLogoError = handleLogoError;