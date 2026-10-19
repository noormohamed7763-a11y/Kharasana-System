/**
 * إدارة عمليات الطلبات في واجهة الويب.
 * يشمل: تحديث السعر الإجمالي، التحقق من المدخلات، وإدارة أزرار الحفظ.
 */

document.addEventListener('DOMContentLoaded', function () {
    const editOrderForm = document.getElementById('editOrderForm');
    if (editOrderForm) {
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
    }

    // ============================================================
    // AJAX Price Saving in Details View
    // ============================================================
    const savePriceBtn = document.getElementById('savePriceBtn');
    if (savePriceBtn) {
        const orderId = savePriceBtn.dataset.orderId;
        const unitPriceInput = document.getElementById('unitPriceInput');
        const priceMessage = document.getElementById('priceMessage');

        savePriceBtn.addEventListener('click', async function () {
            const unitPrice = parseFloat(unitPriceInput.value);

            if (isNaN(unitPrice) || unitPrice <= 0) {
                displayMessage(priceMessage, 'danger', 'يرجى إدخال سعر صحيح أكبر من صفر.');
                return;
            }

            this.disabled = true;
            this.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span> جاري الحفظ...`;
            priceMessage.innerHTML = ''; // Clear previous messages

            try {
                const response = await fetch(`/OrderWorkflow/SavePrice/${orderId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrfToken() // Assuming a global function for CSRF token
                    },
                    body: JSON.stringify({ unitPrice: unitPrice })
                });

                const data = await response.json();

                if (data.success) {
                    displayMessage(priceMessage, 'success', data.message);
                } else {
                    displayMessage(priceMessage, 'danger', data.message);
                }
            } catch (error) {
                console.error('Error saving price:', error);
                displayMessage(priceMessage, 'danger', 'حدث خطأ أثناء حفظ السعر.');
            } finally {
                this.disabled = false;
                this.innerHTML = `<i class="bi bi-save"></i> حفظ السعر`;
            }
        });
    }

    // ============================================================
    // AJAX Add Payment in Details View
    // ============================================================
    const addPaymentModal = document.getElementById('addPaymentModal');
    if (addPaymentModal) {
        const savePaymentBtn = addPaymentModal.querySelector('#savePaymentBtn');
        const paymentForm = addPaymentModal.querySelector('#addPaymentForm');
        const orderId = paymentForm.querySelector('input[name="OrderId"]').value;

        savePaymentBtn.addEventListener('click', async function () {
            if (!paymentForm.checkValidity()) {
                paymentForm.classList.add('was-validated');
                return;
            }

            const formData = new FormData(paymentForm);
            const paymentData = Object.fromEntries(formData.entries());

            // Convert Amount to number
            paymentData.Amount = parseFloat(paymentData.Amount);
            // Convert PaymentMethod to number (enum value)
            paymentData.PaymentMethod = parseInt(paymentData.PaymentMethod);

            this.disabled = true;
            this.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span> جاري التسجيل...`;

            try {
                const response = await fetch(`/OrderWorkflow/AddPayment/${orderId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrfToken()
                    },
                    body: JSON.stringify(paymentData)
                });

                const data = await response.json();

                if (data.success) {
                    displayToast('success', data.message || 'تم تسجيل الدفعة بنجاح.');
                    // Reload page to reflect changes, or update financial summary dynamically
                    location.reload();
                } else {
                    displayToast('error', data.message || 'فشل تسجيل الدفعة.');
                }
            } catch (error) {
                console.error('Error adding payment:', error);
                displayToast('error', 'حدث خطأ أثناء تسجيل الدفعة.');
            } finally {
                this.disabled = false;
                this.innerHTML = `تسجيل الدفعة`;
                // Close modal on success or error
                const modal = bootstrap.Modal.getInstance(addPaymentModal);
                if (modal) modal.hide();
            }
        });
    }

    // Helper function to get CSRF token (assuming it's in a meta tag)
    function getCsrfToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    // Helper function to display messages (for price saving)
    function displayMessage(element, type, message) {
        element.textContent = message;
        element.className = ''; // Clear existing classes
        element.classList.add('ms-3', `text-${type}`);
    }

    // Helper function to display toasts (for payments, requires site.js toast setup)
    function displayToast(type, message) {
        // Assuming a global toast function exists in site.js or similar
        // Example: showToast(type, message);
        // For now, let's just log it if no toast system is integrated
        if (typeof showToast === 'function') {
            showToast(type, message);
        } else {
            alert(message);
        }
    }
});
