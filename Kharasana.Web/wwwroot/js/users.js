// Kharasana.Web — Users
// تبديل حقول نموذج المستخدم حسب الدور المختار.
// يُستخدم من صفحتَي إنشاء وتعديل المستخدم (Create/Edit) —
// نفس عناصر النموذج ونفس المعرفات في الصفحتين.
// أسماء الأدوار تُقرأ من سمات data-role-* على عنصر الدور نفسه.
//
// السلوك مطابق للسكريبت السابق (jQuery toggle/show/hide):
// العناصر مخفية مبدئياً بنمط مضمّن style="display: none"،
// فتُظهرها هذه الدالة بإسقاط النمط المضمّن (inline display)
// لا بإضافة صنف — وإلا ظلّ النمط المضمّن يغلب الصنف.

(function () {
    var roleSelect = document.getElementById('Role');
    if (!roleSelect) return;

    var driverRole = roleSelect.dataset.roleDriver || 'Driver';
    var employeeRole = roleSelect.dataset.roleEmployee || 'FactoryEmployee';

    function setVisible(element, visible) {
        if (!element) return;
        // إسقاط النمط المضمّن يُعيد العرض الافتراضي —
        // و'' يخفي — كما تفعل jQuery show()/hide().
        element.style.display = visible ? '' : 'none';
    }

    function toggleFields(role) {
        var isDriver = role === driverRole;
        var isEmployee = role === employeeRole;
        var needsFactory = isDriver || isEmployee;

        setVisible(document.getElementById('licenseNumberGroup'), isDriver);
        setVisible(document.getElementById('driverStatusGroup'), isDriver);
        setVisible(document.getElementById('factoryGroup'), needsFactory);
        setVisible(document.getElementById('factoryRequired'), needsFactory);
        setVisible(document.getElementById('factoryNote'), needsFactory);
    }

    roleSelect.addEventListener('change', function () {
        toggleFields(this.value);
    });

    toggleFields(roleSelect.value);
})();
