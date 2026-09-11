document.addEventListener("DOMContentLoaded", function () {
    const rootStyles = getComputedStyle(document.documentElement);
    const primary = rootStyles.getPropertyValue('--primary').trim();
    const success = rootStyles.getPropertyValue('--success').trim();
    const warning = rootStyles.getPropertyValue('--warning').trim();
    const info = rootStyles.getPropertyValue('--info').trim();
    const fontMain = rootStyles.getPropertyValue('--font-main').trim() || 'Cairo';

    // لون بنفسجي ثابت يتطابق مع kpi-icon-purple في التصميم
    const purple = '#a855f7';

    // 1. Bar Chart: System Overview (نظرة عامة على النظام)
    // بيانات حقيقية من الـ ViewModel: مصانع / عملاء / موظفو المصانع / سائقون / طلبات
    const overviewCanvas = document.getElementById('systemOverviewChart');
    if (overviewCanvas) {
        try {
            const values = JSON.parse(overviewCanvas.dataset.chartValues || '[]');
            if (Array.isArray(values) && values.length > 0) {
                const palette = [primary, info, purple, success, warning];
                new Chart(overviewCanvas.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: values.map(v => v.label),
                        datasets: [{
                            label: 'الإجمالي',
                            data: values.map(v => v.count),
                            backgroundColor: palette.slice(0, values.length),
                            borderRadius: 6,
                            maxBarThickness: 46
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                displayColors: false,
                                bodyFont: { family: fontMain }
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                grid: { borderDash: [4, 4] },
                                ticks: { precision: 0, font: { family: fontMain } }
                            },
                            x: {
                                grid: { display: false },
                                ticks: { font: { family: fontMain } }
                            }
                        }
                    }
                });
            }
        } catch (e) {
            console.error("Error parsing System Overview JSON:", e);
        }
    }

    // 2. Doughnut Chart: System Status (حالة النظام)
    // توزيع المستخدمين حسب الدور: عملاء / موظفو المصانع / سائقون
    const statusCanvas = document.getElementById('systemStatusChart');
    if (statusCanvas) {
        try {
            const values = JSON.parse(statusCanvas.dataset.chartValues || '[]');
            if (Array.isArray(values) && values.length > 0) {
                new Chart(statusCanvas.getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: values.map(v => v.label),
                        datasets: [{
                            data: values.map(v => v.count),
                            backgroundColor: [info, purple, success],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'bottom',
                                labels: { boxWidth: 12, padding: 15, font: { family: fontMain } }
                            }
                        },
                        cutout: '70%'
                    }
                });
            }
        } catch (e) {
            console.error("Error parsing System Status JSON:", e);
        }
    }

    // 3. Doughnut Chart: Factory Active Orders (حالة الطلبات النشطة)
    // بيانات حقيقية من الـ ViewModel: جديدة / قيد التنفيذ / جاهزة للتسليم / قيد النقل
    const factoryCanvas = document.getElementById('factoryOrdersChart');
    if (factoryCanvas) {
        try {
            const values = JSON.parse(factoryCanvas.dataset.chartValues || '[]');
            const hasData = Array.isArray(values) && values.some(v => (v.count || 0) > 0);
            if (hasData) {
                new Chart(factoryCanvas.getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: values.map(v => v.label),
                        datasets: [{
                            data: values.map(v => v.count),
                            backgroundColor: [primary, warning, info, purple],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'bottom',
                                labels: { boxWidth: 12, padding: 15, font: { family: fontMain } }
                            }
                        },
                        cutout: '70%'
                    }
                });
            } else {
                // لا توجد طلبات نشطة — نعرض حالة فارغة صادقة بدلاً من دائرة صفراء
                const wrap = factoryCanvas.closest('.chart-container');
                if (wrap) {
                    factoryCanvas.style.display = 'none';
                    const empty = document.createElement('div');
                    empty.className = 'chart-empty-state';
                    empty.innerHTML = '<i class="bi bi-inbox"></i><span>لا توجد طلبات نشطة مسجلة بعد</span>';
                    wrap.appendChild(empty);
                }
            }
        } catch (e) {
            console.error("Error parsing Factory Orders JSON:", e);
        }
    }

    // ===== Count-up animation for KPI values =====
    // يُحرَّك العداد من 0 إلى القيمة المعروضة. يُحترم prefers-reduced-motion
    // فلا يُشغَّل العدّ أبداً لمن يفضّل تقليل الحركة.
    if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        const counters = document.querySelectorAll('.kpi-value[data-count]');
        counters.forEach(counter => {
            const target = parseFloat(counter.getAttribute('data-count'));
            if (isNaN(target)) return;

            const duration = 900;
            const start = performance.now();
            const isDecimal = counter.getAttribute('data-count').includes('.');
            const step = (now) => {
                const progress = Math.min((now - start) / duration, 1);
                // ease-out لتسارع لطيف في البداية وتباطؤ في النهاية
                const eased = 1 - Math.pow(1 - progress, 3);
                const value = target * eased;
                // أرقام غربية بمحدّد آلاف — مطابقة لتنسيق N0 المستخدم في بقية الواجهة
                counter.textContent = isDecimal
                    ? value.toFixed(1)
                    : Math.round(value).toLocaleString('en-US');
                if (progress < 1) requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        });
    }
});