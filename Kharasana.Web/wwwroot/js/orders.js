/**
 * Kharasana ERP - Orders Management Scripts
 * يدير عمليات تعيين السائق، تغيير الحالة، الرفض، الإلغاء، وغيرها باستخدام AJAX
 * @version 2.3 - Security fixes for XSS prevention
 */

document.addEventListener("DOMContentLoaded", function () {

    // ============================================================
    // 1. CONFIGURATION - الإعدادات
    // ============================================================
    const CONFIG = {
        toastDuration: 5000,
        reloadDelay: 1000,
        statusMessages: {
            approve: '✅ تمت موافقة العميل بنجاح',
            reject: '❌ تم رفض الطلب بنجاح',
            cancel: '🚫 تم إلغاء الطلب بنجاح',
            'start-delivery': '🚚 تم بدء التوصيل بنجاح',
            deliver: '📦 تم تسليم الطلب بنجاح',
            close: '🔒 تم إغلاق الطلب بنجاح',
            delete: '🗑️ تم حذف الطلب بنجاح'
        }
    };


    // ============================================================
    // 2. HELPER FUNCTIONS - دوال مساعدة
    // ============================================================

    /**
     * الحصول على AntiForgeryToken من النموذج أو من الصفحة
     * @returns {string|null} رمز الحماية أو null
     */
    function getRequestVerificationToken() {
        // البحث في النموذج أولاً
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput && tokenInput.value) {
            return tokenInput.value;
        }

        // البحث في أي مكان آخر في الصفحة
        const metaToken = document.querySelector('meta[name="csrf-token"]');
        if (metaToken) {
            return metaToken.getAttribute('content');
        }

        // محاولة الحصول من أي نموذج في الصفحة
        const formToken = document.querySelector('form input[name="__RequestVerificationToken"]');
        if (formToken) {
            return formToken.value;
        }

        return null;
    }

    /**
     * عرض رسالة للمستخدم باستخدام Toast أو Alert
     * @param {string} message - نص الرسالة
     * @param {string} type - نوع الرسالة (success, error, warning, info)
     */
    function showMessage(message, type = "success") {
        // محاولة استخدام Toast Container
        const toastContainer = document.getElementById('toastContainer');

        if (toastContainer) {
            const toast = document.createElement('div');
            const bgColor = type === 'success' ? 'bg-success' :
                type === 'error' ? 'bg-danger' :
                    type === 'warning' ? 'bg-warning' : 'bg-info';

            toast.className = `toast align-items-center text-white ${bgColor} border-0 show`;
            toast.role = 'alert';
            toast.setAttribute('aria-live', 'assertive');
            toast.setAttribute('aria-atomic', 'true');

            // ✅ بناء DOM بشكل آمن بدلاً من innerHTML
            const dFlex = document.createElement('div');
            dFlex.className = 'd-flex';

            const toastBody = document.createElement('div');
            toastBody.className = 'toast-body';

            const icon = type === 'success' ? 'bi-check-circle-fill' :
                type === 'error' ? 'bi-exclamation-triangle-fill' :
                    type === 'warning' ? 'bi-exclamation-circle-fill' : 'bi-info-circle-fill';

            const iconElement = document.createElement('i');
            iconElement.className = `bi ${icon} me-2`;
            toastBody.appendChild(iconElement);

            // ✅ إضافة النص بشكل آمن باستخدام textContent
            const textNode = document.createTextNode(message);
            toastBody.appendChild(textNode);

            dFlex.appendChild(toastBody);

            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'btn-close btn-close-white me-2 m-auto';
            closeBtn.setAttribute('data-bs-dismiss', 'toast');
            closeBtn.setAttribute('aria-label', 'Close');
            dFlex.appendChild(closeBtn);

            toast.appendChild(dFlex);
            toastContainer.appendChild(toast);
            setTimeout(() => toast.remove(), CONFIG.toastDuration);
        } else {
            const alertDiv = document.getElementById('messageContainer');
            if (alertDiv) {
                const alertClass = type === 'success' ? 'alert-success' :
                    type === 'error' ? 'alert-danger' :
                        type === 'warning' ? 'alert-warning' : 'alert-info';

                alertDiv.className = `alert ${alertClass} alert-dismissible fade show`;
                alertDiv.style.display = 'block';

                // ✅ مسح المحتوى القديم وبناء DOM آمن
                alertDiv.innerHTML = '';

                const icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
                const iconElement = document.createElement('i');
                iconElement.className = `bi ${icon} me-2`;
                alertDiv.appendChild(iconElement);

                const textNode = document.createTextNode(message);
                alertDiv.appendChild(textNode);

                const closeBtn = document.createElement('button');
                closeBtn.type = 'button';
                closeBtn.className = 'btn-close';
                closeBtn.setAttribute('data-bs-dismiss', 'alert');
                closeBtn.setAttribute('aria-label', 'Close');
                alertDiv.appendChild(closeBtn);

                setTimeout(() => alertDiv.style.display = 'none', CONFIG.toastDuration);
            } else {
                alert(message);
            }
        }
    }

    /**
     * عرض رسالة مضمنة في عنصر محدد
     * @param {string} targetId - معرف العنصر
     * @param {string} message - نص الرسالة
     * @param {string} type - نوع الرسالة (success, error)
     */
    function showInlineMessage(targetId, message, type = 'error') {
        const el = document.getElementById(targetId);
        if (!el) return;
        const cls = type === 'success' ? 'alert alert-success' : 'alert alert-danger';
        el.className = cls;

        // ✅ مسح المحتوى القديم وإضافة النص بشكل آمن
        el.innerHTML = '';
        const textNode = document.createTextNode(message);
        el.appendChild(textNode);

        el.style.display = 'block';
        setTimeout(() => { el.style.display = 'none'; }, CONFIG.toastDuration);
    }

    /**
     * تحديث حالة الطلب في الجدول دون إعادة تحميل الصفحة
     */
    function updateOrderStatus(orderId, statusText, statusClass) {
        const row = document.querySelector(`#order-${orderId}`);
        if (row) {
            const badge = row.querySelector('.status-badge');
            if (badge) {
                badge.textContent = statusText;
                badge.className = `status-badge ${statusClass}`;
            }
        }
    }

    /**
     * تحديث اسم السائق في الجدول دون إعادة تحميل الصفحة
     */
    function updateDriverName(orderId, driverName) {
        const row = document.querySelector(`#order-${orderId}`);
        if (row) {
            const driverCell = row.querySelector('.driver-name');
            if (driverCell) {
                driverCell.textContent = driverName;
            }
        }
    }

    /**
     * معالجة استجابة AJAX
     */
    async function handleAjaxResponse(response, onSuccess, onError) {
        try {
            const data = await response.json();

            if (response.ok && data.success) {
                if (onSuccess) onSuccess(data);
                showMessage(data.message || 'تمت العملية بنجاح', 'success');
            } else {
                const errorMsg = data.message || data.error || 'حدث خطأ أثناء العملية';
                if (onError) onError(data);
                showMessage(errorMsg, 'error');
            }
        } catch (error) {
            console.error('Error parsing response:', error);
            showMessage('حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.', 'error');
            if (onError) onError({ message: 'حدث خطأ غير متوقع' });
        }
    }

    /**
     * تعطيل أو تمكين الزر
     */
    function setButtonState(button, disabled, text = null) {
        if (!button) return;
        button.disabled = disabled;
        if (text !== null) {
            button.innerHTML = text;
        }
    }

    /**
     * الحصول على نص الزر أثناء التحميل مع Spinner
     * @param {string} text - النص المعروض
     * @returns {string} - HTML آمن (نص ثابت فقط)
     */
    function getLoadingText(text = 'جاري التنفيذ...') {
        // ✅ آمن لأن النص ثابت وليس من إدخال المستخدم أو API
        return `<span class="spinner-border spinner-border-sm me-2"></span> ${text}`;
    }


    // ============================================================
    // 3. ASSIGN DRIVER - تعيين سائق (باستخدام form)
    // ============================================================

    document.querySelectorAll('form[data-ajax-assign="true"]').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn?.innerHTML || 'تعيين';
            if (submitBtn) {
                setButtonState(submitBtn, true, getLoadingText('جاري التعيين...'));
            }

            try {
                const orderId = this.querySelector('input[name="id"]')?.value;
                const url = this.action || `/Orders/AssignDriver/${orderId}`;
                const token = getRequestVerificationToken();

                const formData = new FormData(this);
                const json = {};
                formData.forEach((value, key) => {
                    if (key !== '__RequestVerificationToken') {
                        json[key] = value;
                    }
                });

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token || ''
                    },
                    body: JSON.stringify(json)
                });

                await handleAjaxResponse(
                    response,
                    (data) => {
                        const driverSelect = this.querySelector('select[name="DriverId"]');
                        const selectedOption = driverSelect?.options[driverSelect.selectedIndex];
                        if (selectedOption && orderId) {
                            updateDriverName(orderId, selectedOption.text);
                        }

                        if (driverSelect) driverSelect.value = '';
                        const truckPlateInput = this.querySelector('input[name="TruckPlate"]');
                        if (truckPlateInput) truckPlateInput.value = '';

                        const modal = this.closest('.modal');
                        if (modal) {
                            const bootstrapModal = bootstrap.Modal.getInstance(modal);
                            if (bootstrapModal) bootstrapModal.hide();
                        }

                        setTimeout(() => location.reload(), CONFIG.reloadDelay);
                    },
                    (error) => {
                        const message = error.message || 'فشل تعيين السائق';
                        showInlineMessage('assignMessage', message, 'error');
                        showMessage(message, 'error');
                    }
                );

            } catch (error) {
                console.error('Error in AssignDriver:', error);
                showMessage('حدث خطأ أثناء الاتصال بالخادم', 'error');
            } finally {
                if (submitBtn) {
                    setButtonState(submitBtn, false, originalText);
                }
            }
        });
    });


    // ============================================================
    // 4. CHANGE STATUS - تغيير حالة الطلب (باستخدام form)
    // ============================================================

    document.querySelectorAll('form[data-ajax-status="true"]').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn?.innerHTML || 'تحديث';
            if (submitBtn) {
                setButtonState(submitBtn, true, getLoadingText('جاري التحديث...'));
            }

            try {
                const orderId = this.querySelector('input[name="id"]')?.value;
                const url = this.action || `/Orders/ChangeStatus/${orderId}`;
                const token = getRequestVerificationToken();

                const formData = new FormData(this);
                const json = {};
                formData.forEach((value, key) => {
                    if (key !== '__RequestVerificationToken') {
                        json[key] = value;
                    }
                });

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token || ''
                    },
                    body: JSON.stringify(json)
                });

                await handleAjaxResponse(
                    response,
                    (data) => {
                        const statusSelect = this.querySelector('select[name="Status"]');
                        if (statusSelect) {
                            const selectedOption = statusSelect.options[statusSelect.selectedIndex];
                            if (selectedOption && orderId) {
                                const statusText = selectedOption.text;
                                const statusClass = selectedOption.dataset.class || 'status-default';
                                updateOrderStatus(orderId, statusText, statusClass);
                            }
                            statusSelect.value = '';
                        }

                        setTimeout(() => location.reload(), CONFIG.reloadDelay);
                    },
                    (error) => {
                        const message = error.message || 'فشل تحديث الحالة';
                        showInlineMessage('statusMessage', message, 'error');
                        showMessage(message, 'error');
                    }
                );

            } catch (error) {
                console.error('Error in ChangeStatus:', error);
                showMessage('حدث خطأ أثناء الاتصال بالخادم', 'error');
            } finally {
                if (submitBtn) {
                    setButtonState(submitBtn, false, originalText);
                }
            }
        });
    });


    // ============================================================
    // 5. ACTION BUTTONS - أزرار الإجراءات (data-action)
    // ============================================================

    const actionButtons = document.querySelectorAll('[data-action]');

    actionButtons.forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();

            const action = this.dataset.action;
            const orderId = this.dataset.orderId;
            const token = getRequestVerificationToken();

            const confirmMessages = {
                approve: 'هل أنت متأكد من موافقة العميل على هذا الطلب؟',
                reject: 'هل أنت متأكد من رفض هذا الطلب؟',
                cancel: '⚠️ هل أنت متأكد من إلغاء هذا الطلب؟',
                'start-delivery': 'هل أنت متأكد من بدء التوصيل؟',
                deliver: 'هل أنت متأكد من تسليم الطلب؟',
                close: 'هل أنت متأكد من إغلاق هذا الطلب؟'
            };

            if (confirmMessages[action] && !confirm(confirmMessages[action])) {
                return;
            }

            if (action === 'reject') {
                const reason = prompt('يرجى كتابة سبب الرفض (اختياري):');
                if (reason === null) return;
                this.dataset.reason = reason || '';
            }

            const originalText = this.innerHTML;
            setButtonState(this, true, getLoadingText('جاري التنفيذ...'));

            try {
                const actionMap = {
                    approve: `/Orders/Approve/${orderId}`,
                    reject: `/Orders/Reject/${orderId}`,
                    cancel: `/Orders/Cancel/${orderId}`,
                    'start-delivery': `/Orders/StartDelivery/${orderId}`,
                    deliver: `/Orders/Deliver/${orderId}`,
                    close: `/Orders/Close/${orderId}`
                };

                const url = actionMap[action];
                if (!url) {
                    showMessage(`إجراء غير معروف: ${action}`, 'error');
                    return;
                }

                const body = action === 'reject'
                    ? JSON.stringify({ reason: this.dataset.reason || '' })
                    : undefined;

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token || ''
                    },
                    ...(body && { body })
                });

                const result = await response.json();

                if (result.success) {
                    const successMsg = CONFIG.statusMessages[action] || '✅ تمت العملية بنجاح';
                    showMessage(successMsg, 'success');
                    setTimeout(() => location.reload(), CONFIG.reloadDelay);
                } else {
                    showMessage(`❌ ${result.message || 'فشلت العملية'}`, 'error');
                }
            } catch (error) {
                console.error(`Error in ${action}:`, error);
                showMessage('❌ حدث خطأ أثناء الاتصال بالخادم', 'error');
            } finally {
                setButtonState(this, false, originalText);
            }
        });
    });


    // ============================================================
    // 6. DELETE ORDER - حذف الطلب مع تأكيد (form)
    // ============================================================

    document.querySelectorAll('form[data-ajax-delete="true"]').forEach(form => {
        form.addEventListener('submit', function (e) {
            if (!confirm('⚠️ هل أنت متأكد من حذف هذا الطلب؟\nلا يمكن التراجع عن هذا الإجراء.')) {
                e.preventDefault();
                return;
            }

            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                const originalText = submitBtn.innerHTML;
                setButtonState(submitBtn, true, getLoadingText('جاري الحذف...'));
            }
        });
    });


    // ============================================================
    // 7. SAVE PRICE - حفظ السعر (في صفحة التفاصيل)
    // ============================================================

    const savePriceBtn = document.getElementById('savePriceBtn');
    if (savePriceBtn) {
        savePriceBtn.addEventListener('click', async function () {
            const orderId = this.dataset.orderId;
            const unitPriceInput = document.getElementById('unitPriceInput');
            const unitPrice = unitPriceInput?.value;
            const messageDiv = document.getElementById('priceMessage');

            // ✅ عرض رسالة خطأ بشكل آمن
            if (!unitPrice || parseFloat(unitPrice) <= 0) {
                if (messageDiv) {
                    messageDiv.innerHTML = '';
                    const span = document.createElement('span');
                    span.className = 'text-danger';
                    const textNode = document.createTextNode('⚠️ يرجى إدخال سعر صحيح (أكبر من صفر)');
                    span.appendChild(textNode);
                    messageDiv.appendChild(span);
                }
                return;
            }

            const token = getRequestVerificationToken();
            const originalText = this.innerHTML;
            setButtonState(this, true, getLoadingText('جاري الحفظ...'));

            try {
                const response = await fetch(`/Orders/SavePrice/${orderId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token || ''
                    },
                    body: JSON.stringify({ unitPrice: parseFloat(unitPrice) })
                });

                const result = await response.json();

                if (result.success) {
                    showMessage('✅ تم حفظ السعر بنجاح', 'success');
                    setTimeout(() => location.reload(), CONFIG.reloadDelay);
                } else {
                    if (messageDiv) {
                        messageDiv.innerHTML = '';
                        const span = document.createElement('span');
                        span.className = 'text-danger';
                        const textNode = document.createTextNode(`❌ ${result.message || 'فشل حفظ السعر'}`);
                        span.appendChild(textNode);
                        messageDiv.appendChild(span);
                    }
                }
            } catch (error) {
                console.error('SavePrice error:', error);
                if (messageDiv) {
                    messageDiv.innerHTML = '';
                    const span = document.createElement('span');
                    span.className = 'text-danger';
                    const textNode = document.createTextNode('❌ حدث خطأ أثناء الاتصال بالخادم');
                    span.appendChild(textNode);
                    messageDiv.appendChild(span);
                }
            } finally {
                setButtonState(this, false, originalText);
            }
        });
    }


    // ============================================================
    // 8. FALLBACK - تعطيل الأزرار عند إرسال أي نموذج عادي
    // ============================================================

    document.querySelectorAll('form').forEach(form => {
        const isAjaxForm = form.dataset.ajaxAssign === 'true' ||
            form.dataset.ajaxStatus === 'true' ||
            form.dataset.ajaxDelete === 'true';

        if (isAjaxForm) return;

        form.addEventListener('submit', function () {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                const originalText = submitBtn.innerHTML;
                setButtonState(submitBtn, true, getLoadingText('جاري الإرسال...'));
                setTimeout(() => {
                    if (submitBtn.disabled) {
                        setButtonState(submitBtn, false, originalText);
                    }
                }, 3000);
            }
        });
    });


    // ============================================================
    // 9. KEYBOARD SHORTCUTS - اختصارات لوحة المفاتيح
    // ============================================================

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            const saveBtn = document.getElementById('savePriceBtn');
            if (saveBtn && document.activeElement?.id === 'unitPriceInput') {
                e.preventDefault();
                saveBtn.click();
            }
        }
    });


    // ============================================================
    // 10. LOGGING - معلومات التحميل
    // ============================================================

    console.log('✅ Orders.js v2.3 loaded successfully');
    console.log(`📋 Found ${document.querySelectorAll('form[data-ajax-assign="true"]').length} assign forms`);
    console.log(`📋 Found ${document.querySelectorAll('form[data-ajax-status="true"]').length} status forms`);
    console.log(`📋 Found ${document.querySelectorAll('form[data-ajax-delete="true"]').length} delete forms`);
    console.log(`📋 Found ${document.querySelectorAll('[data-action]').length} action buttons`);
});


// ============================================================
// 11. EXPORT FUNCTIONS - دوال التصدير (للاستخدام في Index)
// ============================================================

/**
 * تصدير الطلبات إلى Excel
 */
function exportOrders(search = '', status = '', factoryId = '') {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (status) params.append('status', status);
    if (factoryId) params.append('factoryId', factoryId);

    window.location.href = `/Orders/Export?${params.toString()}`;
}

/**
 * تصدير الطلبات إلى PDF
 */
function exportOrdersPdf(search = '', status = '', factoryId = '') {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (status) params.append('status', status);
    if (factoryId) params.append('factoryId', factoryId);

    window.location.href = `/Orders/ExportPdf?${params.toString()}`;
}

/**
 * تطبيق الفلاتر وإعادة تحميل الصفحة
 */
function applyFilters() {
    const status = document.getElementById('statusFilter')?.value || '';
    const factory = document.getElementById('factoryFilter')?.value || '';

    const statusInput = document.getElementById('statusInput');
    const factoryInput = document.getElementById('factoryInput');

    if (statusInput) statusInput.value = status;
    if (factoryInput) factoryInput.value = factory;

    const searchForm = document.getElementById('searchForm');
    if (searchForm) searchForm.submit();
}