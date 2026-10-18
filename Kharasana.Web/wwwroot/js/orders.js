/**
 * إدارة عمليات الطلبات في واجهة الويب.
 * يشمل: تحديث السعر الإجمالي، التحقق من المدخلات، وإدارة أزرار الحفظ.
 */

document.addEventListener('DOMContentLoaded', function () {
    const editOrderForm = document.getElementById('editOrderForm');
    if (!editOrderForm) return;

    // قراءة البيانات من data-attributes لتجنب الاعتماد المباشر على Razor داخل JS
    const config = {
        quantityId: editOrderForm.dataset.quantityId,
        unitPrice: parseFloat(editOrderForm.dataset.unitPrice) || 0,
        totalPricePreviewId: 'totalPricePreview',
        submitBtnId: 'submitBtn'
    };

    const quantityInput = document.getElementById(config.quantityId);
    const totalPricePreview = document.getElementById(config.totalPricePreviewId);
    const submitBtn = document.getElementById(config.submitBtnId);

    /**
     * تحديث عرض السعر الإجمالي بناءً على الكمية وسعر المتر
     */
    function updateTotalPrice() {
        if (!quantityInput || !totalPricePreview) return;

        const quantity = parseFloat(quantityInput.value) || 0;
        const total = config.unitPrice * quantity;

        totalPricePreview.value = total > 0
            ? total.toLocaleString('ar-YE') + ' ر.ي'
            : '-';
    }

    // ربط أحداث التغيير في الكمية
    if (quantityInput) {
        ['input', 'change'].forEach(evt =>
            quantityInput.addEventListener(evt, updateTotalPrice)
        );

        // التفعيل الأولي
        updateTotalPrice();

        // التحقق من صحة الكمية عند الخروج من الحقل
        quantityInput.addEventListener('blur', function () {
            const value = parseFloat(this.value);
            if (isNaN(value) || value <= 0) {
                this.classList.add('is-invalid');
                const feedback = this.nextElementSibling;
                if (feedback && feedback.classList.contains('text-danger')) {
                    feedback.textContent = 'الكمية يجب أن تكون أكبر من صفر';
                }
            } else {
                this.classList.remove('is-invalid');
            }
        });
    }

    // إدارة حالة زر الإرسال لمنع التكرار وإظهار مؤشر التحميل
    if (editOrderForm && submitBtn) {
        editOrderForm.addEventListener('submit', function () {
            submitBtn.disabled = true;
            submitBtn.innerHTML = `
                <span class="spinner-border spinner-border-sm me-2"></span>
                جاري الحفظ...
            `;
        });
    }
});
