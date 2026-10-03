// ==========================
// Confirmation Modal — يُستخدم بدل confirm() في كل الصفحات
// ==========================
(function () {
    let pendingForm = null;
    let pendingButton = null;
    let confirmModal = null;

    function ensureModal() {
        if (confirmModal) return;

        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'confirmActionModal';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog">' +
                '<div class="modal-content">' +
                    '<div class="modal-header">' +
                        '<h5 class="modal-title">تأكيد</h5>' +
                        '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="إغلاق"></button>' +
                    '</div>' +
                    '<div class="modal-body" id="confirmModalBody"></div>' +
                    '<div class="modal-footer">' +
                        '<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">إلغاء</button>' +
                        '<button type="button" class="btn btn-danger" id="confirmModalYes">تأكيد</button>' +
                    '</div>' +
                '</div>' +
            '</div>';
        document.body.appendChild(modal);

        confirmModal = new bootstrap.Modal(modal);

        modal.addEventListener('hidden.bs.modal', function () {
            pendingForm = null;
            pendingButton = null;
        });

        document.getElementById('confirmModalYes').addEventListener('click', function () {
            confirmModal.hide();
            if (pendingForm) {
                pendingForm.submit();
                pendingForm = null;
            } else if (pendingButton) {
                // ✅ تنفيذ مؤجل لزر [data-confirm]: النقر الأول فتح الـ Modal،
                //    وهذا النقر (من داخل الـ Modal) يُنفّذ الإجراء. العلامة
                //    __confirming تمنع إعادة فتح الـ Modal في نفس الزر.
                const btn = pendingButton;
                pendingButton = null;
                btn.dataset.confirming = 'true';
                btn.click();
                delete btn.dataset.confirming;
            }
        });
    }

    document.addEventListener('submit', function (e) {
        const form = e.target.closest ? e.target.closest('form[data-confirm]') : null;
        if (!form) return;

        e.preventDefault();
        const message = form.getAttribute('data-confirm');
        pendingForm = form;
        pendingButton = null;
        ensureModal();
        document.getElementById('confirmModalBody').textContent = message || 'هل أنت متأكد؟';
        confirmModal.show();
    }, true);

    // أزرار [data-confirm]: النقر الأول يفتح الـ Modal بدل confirm()،
    // والنقر الثاني (من زر «تأكيد» في الـ Modal) يُكمل الإجراء الأصلي.
    document.addEventListener('click', function (e) {
        const btn = e.target.closest ? e.target.closest('button[data-confirm]') : null;
        if (!btn) return;
        if (btn.dataset.confirming === 'true') return; // نقر التأكيد من داخل الـ Modal

        e.preventDefault();
        e.stopImmediatePropagation();
        pendingForm = null;
        pendingButton = btn;
        ensureModal();
        document.getElementById('confirmModalBody').textContent =
            btn.getAttribute('data-confirm') || 'هل أنت متأكد؟';
        confirmModal.show();
    }, true);
})();

// ==========================
// Toast — إشعارات فورية
// ==========================
function showToast(title, message, type) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    type = type || 'success';
    const id = 'toast-' + Date.now();
    const toastEl = document.createElement('div');
    toastEl.className = 'toast align-items-center text-white bg-' + type + ' border-0 m-2';
    toastEl.id = id;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');

    const dFlex = document.createElement('div');
    dFlex.className = 'd-flex';

    const bodyDiv = document.createElement('div');
    bodyDiv.className = 'toast-body';

    const titleEl = document.createElement('strong');
    titleEl.textContent = title;
    bodyDiv.appendChild(titleEl);
    bodyDiv.appendChild(document.createElement('br'));

    const messageEl = document.createElement('span');
    messageEl.textContent = message;
    bodyDiv.appendChild(messageEl);

    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'btn-close btn-close-white me-2 m-auto';
    closeBtn.setAttribute('data-bs-dismiss', 'toast');
    closeBtn.setAttribute('aria-label', 'إغلاق');

    dFlex.appendChild(bodyDiv);
    dFlex.appendChild(closeBtn);
    toastEl.appendChild(dFlex);

    container.appendChild(toastEl);
    const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 4000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', function () {
        toastEl.remove();
    });
}

// ==========================
// Tooltips — تفعيل تلقائي لجميع الأدوات
// ==========================
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });
});

// ==========================
// Keyboard Shortcuts
// ==========================
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const modal = bootstrap.Modal.getInstance(document.getElementById('confirmActionModal'));
        if (modal) modal.hide();
    }
    if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        const search = document.querySelector('#search, #tableSearch, input[type="search"]');
        if (search) { search.focus(); search.select(); }
    }
    if (e.key === '/' && document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA') {
        e.preventDefault();
        const search = document.querySelector('#search, #tableSearch, input[type="search"]');
        if (search) { search.focus(); search.select(); }
    }
});

// ==========================
// Empty Table State — إظهار رسالة عند عدم وجود نتائج
// ==========================
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table.erp-table').forEach(function (table) {
            const tbody = table.querySelector('tbody');
            if (!tbody) return;

            const observer = new MutationObserver(function () {
                checkEmpty(tbody, table);
            });
            observer.observe(tbody, { childList: true, attributes: true, subtree: true });
            checkEmpty(tbody, table);
        });
    });

    function checkEmpty(tbody, table) {
        // صفّ «لا نتائج» نفسه يُستثنى من العدّ: كان يُحتسب صفًا ظاهرًا فيُزال،
        // وإزالته تجعل العدّ صفرًا فيُضاف من جديد — والمراقب يرصد كل إضافة/إزالة،
        // فيدور الاثنان في حلقة لا تنتهي بمجرد أن تُخفى كل الصفوف (أول بحث بلا نتائج).
        const rows = Array.from(tbody.querySelectorAll('tr'))
            .filter(function (row) { return !row.classList.contains('no-results-row'); });

        const visibleCount = rows.filter(function (row) {
            return row.style.display !== 'none';
        }).length;

        let noResults = tbody.querySelector('.no-results-row');
        if (visibleCount === 0 && rows.length > 0) {
            if (!noResults) {
                const colCount = table.querySelector('thead th') ? table.querySelectorAll('thead th').length : 10;
                noResults = document.createElement('tr');
                noResults.className = 'no-results-row';
                noResults.innerHTML = '<td colspan="' + colCount + '" class="text-center text-muted py-4">' +
                    '<i class="bi bi-search" style="font-size: 2rem; opacity: 0.4;"></i>' +
                    '<p class="mt-2 mb-0">لم يتم العثور على نتائج</p>' +
                    '</td>';
                tbody.appendChild(noResults);
            }
        } else if (noResults) {
            noResults.remove();
        }
    }
})();

// ==========================
// Driver Status Quick Filters — فلاتر سريعة للسائقين
// ==========================
(function () {
    document.addEventListener('click', function (e) {
        const btn = e.target.closest ? e.target.closest('.driver-filter-btn') : null;
        if (!btn) return;

        const filter = btn.dataset.filter;

        document.querySelectorAll('#driverTableBody tr[data-status]').forEach(function (row) {
            if (filter === 'All' || row.dataset.status === filter) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });

        document.querySelectorAll('.driver-filter-btn').forEach(function (b) {
            b.classList.remove('active');
        });
        btn.classList.add('active');
    });
})();

// ==========================
// Order Status Quick Filters — فلاتر سريعة لطلبات المصنع
// ==========================
(function () {
    document.addEventListener('click', function (e) {
        const btn = e.target.closest ? e.target.closest('.order-filter-btn') : null;
        if (!btn) return;

        const filter = btn.dataset.filter;

        const rows = document.querySelectorAll('#factoryOrdersTableBody tr[data-status]');
        let visibleCount = 0;

        rows.forEach(function (row) {
            const show = filter === 'All' || row.dataset.status === filter;
            row.style.display = show ? '' : 'none';
            if (show) visibleCount++;
        });

        // نتيجة فارغة بعد التصفية: القائمة ليست فارغة (وإلا ظهرت رسالة "لا توجد طلبات
        // مسجلة") لكن لا صف يطابق الفلتر — فبدون هذا يبقى الجدول فارغًا بلا تفسير.
        const emptyRow = document.getElementById('factoryOrdersFilterEmpty');
        if (emptyRow) {
            emptyRow.classList.toggle('d-none', !(rows.length > 0 && visibleCount === 0));
        }

        document.querySelectorAll('.order-filter-btn').forEach(function (b) {
            b.classList.remove('active', 'btn-primary');
            b.classList.add('btn-outline-primary');
        });
        btn.classList.remove('btn-outline-primary');
        btn.classList.add('active', 'btn-primary');
    });
})();

// ==========================
// Driver Toggle — تحديث فوري بدون Modal
// ==========================
(function () {
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.btn-toggle-active');
        if (!btn) return;

        // ✅ حارس طلب جارٍ: كل نداء يقلب الحالة على الخادم (IsActive = !IsActive)، فالنقر
        //    المزدوج كان يرسل POSTين ويعيد السجل إلى حالته الأولى بينما تعرض الواجهة
        //    نتيجة النقرة الأخيرة وحدها — فيفترق ما تراه العين عمّا في قاعدة البيانات.
        if (btn.dataset.busy === 'true') return;
        btn.dataset.busy = 'true';

        const driverId = btn.dataset.driverId;
        const isActive = btn.dataset.isActive === 'true';
        const icon = btn.querySelector('i');
        const newIsActive = !isActive;

        // تحديث فوري — لا تنتظر الاستجابة
        btn.dataset.isActive = newIsActive;
        btn.classList.remove(isActive ? 'text-danger' : 'text-success');
        btn.classList.add(newIsActive ? 'text-danger' : 'text-success');
        if (icon) icon.className = 'bi ' + (newIsActive ? 'bi-stop-circle' : 'bi-play-circle');

        // إرسال الطلب في الخلفية
        try {
            const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            const response = await fetch('/Drivers/ToggleActive?id=' + driverId, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    // نفس اتفاقية AJAX المتبعة في orders.js — بدونها يعيد الـ controller
                    // إعادة توجيه (صفحة HTML بحالة 200) فيقرأها الكود نجاحاً وهي ليست كذلك.
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': tokenEl ? tokenEl.value : ''
                }
            });

            // لا يكفي response.ok وحده: عند انتهاء الجلسة يعيد الخادم إعادة توجيه إلى
            // Account/Login فتصل صفحة HTML بحالة 200 وتُقرأ نجاحاً زائفاً. لذلك نقرأ JSON
            // ونتحقق من success — نفس ما يفعله orders.js في handleAjaxResponse.
            const data = await response.json();

            if (!response.ok || !data.success) throw new Error(data?.message || 'Request failed');

            showToast('تم التحديث',
                newIsActive ? 'تم تفعيل حساب السائق.' : 'تم إيقاف حساب السائق.',
                'success');
        } catch (err) {
            // التراجع عند الفشل
            btn.dataset.isActive = isActive;
            btn.classList.remove(newIsActive ? 'text-danger' : 'text-success');
            btn.classList.add(isActive ? 'text-danger' : 'text-success');
            if (icon) icon.className = 'bi ' + (isActive ? 'bi-stop-circle' : 'bi-play-circle');
            showToast('خطأ', 'تعذر تحديث حالة السائق. حاول مرة أخرى.', 'danger');
        } finally {
            btn.dataset.busy = 'false';
        }
    });
})();

// ==========================
// Inline Event Handler Replacement — data-* delegation
// Replaces all onclick/onchange/oninput inline handlers for CSP compliance
// ==========================
(function () {
    'use strict';

    // data-click-fn: calls window[fn]() on button click
    document.addEventListener('click', function (e) {
        var el = e.target.closest('[data-click-fn]');
        if (!el) return;
        var fn = el.getAttribute('data-click-fn');
        if (fn && typeof window[fn] === 'function') window[fn]();
    });

    // data-change-fn: calls window[fn](element.value) on change
    document.addEventListener('change', function (e) {
        var el = e.target.closest('[data-change-fn]');
        if (!el) return;
        var fn = el.getAttribute('data-change-fn');
        if (fn && typeof window[fn] === 'function') window[fn](el.value);
    });

    // data-input-fn: calls window[fn](element.value) on input
    document.addEventListener('input', function (e) {
        var el = e.target.closest('[data-input-fn]');
        if (!el) return;
        var fn = el.getAttribute('data-input-fn');
        if (fn && typeof window[fn] === 'function') window[fn](el.value);
    });

    // data-factory-id: calls openCreateAccountModal(id, name) on button click
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-factory-id]');
        if (!btn) return;
        if (typeof window.openCreateAccountModal === 'function') {
            window.openCreateAccountModal(
                btn.getAttribute('data-factory-id'),
                btn.getAttribute('data-factory-name'));
        }
    });

    // #retryButton: reload the page
    document.addEventListener('click', function (e) {
        if (e.target.id === 'retryButton') {
            e.preventDefault();
            location.reload();
        }
    });

    // (زر طباعة تقرير السائق #printReportBtn أُزيل من هنا: صار يستعمل
    //  data-print مثل بقية أزرار الطباعة — معالج واحد أعلى الملف.)
})();

// ==========================
// Image Error Handling — CSP-safe fallback for img onerror
// Replaces inline onerror="handleLogoError(this)" / onerror="this.remove()" etc.
// ==========================
(function () {
    document.addEventListener('error', function (e) {
        if (e.target.tagName !== 'IMG') return;

        var img = e.target;
        var mode = img.dataset.imgError;
        if (!mode) return;

        img.onerror = null; // prevent infinite loop

        switch (mode) {
            case 'logo':
                if (typeof window.handleLogoError === 'function') {
                    window.handleLogoError(img);
                }
                break;
            case 'remove':
                img.remove();
                break;
            case 'hide-next':
                img.classList.add('d-none');
                if (img.nextElementSibling) {
                    img.nextElementSibling.classList.remove('d-none');
                }
                break;
        }
    }, true);
})();

// ==========================
// Prefetch on Hover — تحميل مسبق عند التمرير
// ==========================
(function () {
    const prefetched = new Set();

    document.addEventListener('mouseover', function (e) {
        const link = e.target.closest('a[href]');

        // ✅ HTMLAnchorElement فقط: مرساة SVG تُعيد href كائنًا (SVGAnimatedString) لا
        //    نصًّا، فيُحوَّل إلى "[object SVGAnimatedString]" ويُرسَل طلبًا بلا معنى.
        if (!(link instanceof HTMLAnchorElement)) return;

        // ✅ روابط نفس الأصل فقط: الطلب الخارجي يحجبه CSP (connect-src 'self') فيظهر
        //    خطأ في طرفية المتصفح بلا أي فائدة، ويكشف نطاق الصفحة لطرف ثالث.
        if (link.origin !== location.origin) return;

        // مرساة داخل الصفحة أو رابط تنزيل — لا تحميل مسبق لهما
        const href = link.getAttribute('href');
        if (!href || href.startsWith('#')) return;
        if (link.hasAttribute('download')) return;

        const url = link.href;
        if (!url || url === location.href) return;

        if (prefetched.has(url)) return;
        prefetched.add(url);

        setTimeout(function () {
            fetch(url, {
                method: 'GET',
                credentials: 'same-origin',
                priority: 'low'
            }).catch(function () {});
        }, 100);
    });
})();

// ==========================
// Copy to Clipboard Helper
// ==========================
document.addEventListener('click', function (e) {
    const copyBtn = e.target.closest('[data-copy]');
    if (!copyBtn) return;

    const textToCopy = copyBtn.getAttribute('data-copy');
    if (!textToCopy) return;

    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(textToCopy).then(function () {
            const originalHtml = copyBtn.innerHTML;
            copyBtn.innerHTML = '<i class="bi bi-check-lg text-success"></i>';
            setTimeout(function () {
                copyBtn.innerHTML = originalHtml;
            }, 1800);
            if (typeof showToast === 'function') {
                showToast('تم النسخ بنجاح', textToCopy, 'success');
            }
        }).catch(function () {
            copyFallback(textToCopy, copyBtn);
        });
    } else {
        copyFallback(textToCopy, copyBtn);
    }
});

function copyFallback(text, btn) {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.select();
    try {
        document.execCommand('copy');
        if (typeof showToast === 'function') {
            showToast('تم النسخ بنجاح', text, 'success');
        }
    } catch (err) {}
    document.body.removeChild(textarea);
}

// ==========================
// Print Helper — data-print
// بديل onclick="window.print()" الذي تحجبه سياسة CSP (script-src بلا unsafe-inline).
// نفس أسلوب data-copy: معالج واحد مُفوَّض على document لكل أزرار الطباعة.
// ==========================
document.addEventListener('click', function (e) {
    const printBtn = e.target.closest('[data-print]');
    if (!printBtn) return;

    e.preventDefault();
    window.print();
});

// ==========================
// Toolbar Filters — data-toolbar-autosubmit
// إرسال نموذج شريط البحث فور تغيير أي قائمة فلترة.
// بديل onchange="applyFilters()" المضمّن الذي تحجبه سياسة CSP، وبديل دالة
// applyFilters التي كانت في orders.js: كانت تنسخ قيمة القائمة إلى حقل مخفي
// مرآة ثم تُرسل النموذج — مساران للقيمة نفسها. الآن القائمة نفسها تحمل name
// فتُرسل مع النموذج، وهذا المعالج يُرسله فقط.
// ==========================
document.addEventListener('change', function (e) {
    const filter = e.target.closest('[data-toolbar-autosubmit]');
    if (!filter) return;

    const form = filter.form;
    if (form) form.submit();
});
