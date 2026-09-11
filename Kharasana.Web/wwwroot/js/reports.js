document.addEventListener("DOMContentLoaded", function () {
    const rootStyles = getComputedStyle(document.documentElement);
    const primary = rootStyles.getPropertyValue('--primary').trim();
    const success = rootStyles.getPropertyValue('--success').trim();
    const warning = rootStyles.getPropertyValue('--warning').trim();
    const info = rootStyles.getPropertyValue('--info').trim();
    const fontMain = rootStyles.getPropertyValue('--font-main').trim() || 'Cairo';
    const purple = '#a855f7';
    const danger = rootStyles.getPropertyValue('--danger').trim() || '#ef4444';

    // 1. Doughnut Chart: توزيع الطلبات حسب الحالة
    const statusCanvas = document.getElementById('reportsStatusChart');
    if (statusCanvas) {
        try {
            const values = JSON.parse(statusCanvas.dataset.chartValues || '[]');
            const hasData = Array.isArray(values) && values.some(v => (v.count || 0) > 0);
            if (hasData) {
                const palette = [primary, warning, info, danger, '#9ca3af', purple, success, '#64748b'];
                new Chart(statusCanvas.getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: values.map(v => v.label),
                        datasets: [{
                            data: values.map(v => v.count),
                            backgroundColor: palette.slice(0, values.length),
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
                            },
                            tooltip: {
                                displayColors: true,
                                bodyFont: { family: fontMain },
                                callbacks: {
                                    label: (ctx) => ` ${ctx.label}: ${ctx.parsed} طلب`
                                }
                            }
                        },
                        cutout: '65%'
                    }
                });
            } else {
                const wrap = statusCanvas.closest('.chart-container');
                if (wrap) {
                    statusCanvas.style.display = 'none';
                    const empty = document.createElement('div');
                    empty.className = 'chart-empty-state';
                    empty.innerHTML = '<i class="bi bi-inbox"></i><span>لا توجد طلبات مسجلة بعد</span>';
                    wrap.appendChild(empty);
                }
            }
        } catch (e) {
            console.error("Error parsing Reports Status JSON:", e);
        }
    }

    // 2. Bar Chart: الطلبات حسب نوع الخرسانة
    const concreteCanvas = document.getElementById('reportsConcreteTypeChart');
    if (concreteCanvas) {
        try {
            const values = JSON.parse(concreteCanvas.dataset.chartValues || '[]');
            const hasData = Array.isArray(values) && values.some(v => (v.count || 0) > 0);
            if (hasData) {
                new Chart(concreteCanvas.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: values.map(v => v.label),
                        datasets: [{
                            label: 'عدد الطلبات',
                            data: values.map(v => v.count),
                            backgroundColor: primary,
                            borderRadius: 6,
                            maxBarThickness: 42
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                displayColors: false,
                                bodyFont: { family: fontMain },
                                callbacks: {
                                    label: (ctx) => ` ${ctx.parsed.y} طلب`
                                }
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
            } else {
                const wrap = concreteCanvas.closest('.chart-container');
                if (wrap) {
                    concreteCanvas.style.display = 'none';
                    const empty = document.createElement('div');
                    empty.className = 'chart-empty-state';
                    empty.innerHTML = '<i class="bi bi-inbox"></i><span>لا توجد أنواع خرسانة مسجلة بعد</span>';
                    wrap.appendChild(empty);
                }
            }
        } catch (e) {
            console.error("Error parsing Reports ConcreteType JSON:", e);
        }
    }

    // ===== Count-up animation لكروت الأرقام ===== (مطابقة للوحة التحكم)
    if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        const counters = document.querySelectorAll('.kpi-value[data-count]');
        counters.forEach(counter => {
            const target = parseFloat(counter.getAttribute('data-count'));
            if (isNaN(target)) return;

            const duration = 900;
            const start = performance.now();
            const step = (now) => {
                const progress = Math.min((now - start) / duration, 1);
                const eased = 1 - Math.pow(1 - progress, 3);
                counter.textContent = Math.round(target * eased).toLocaleString('en-US');
                if (progress < 1) requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        });
    }
});