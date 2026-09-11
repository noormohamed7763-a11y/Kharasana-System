/**
 * Kharasana ERP – Login Page Scripts
 * Factory Desktop Edition
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {

        // ======================================
        // 1. التركيز التلقائي على حقل المستخدم
        // ======================================
        const emailInput = document.querySelector("input[name='EmailOrPhone']");
        if (emailInput) {
            emailInput.focus();
        }

        // ======================================
        // 2. إظهار وإخفاء كلمة المرور
        // ======================================
        const passwordInput = document.getElementById('passwordInput');
        const toggleBtn = document.getElementById('togglePassword');

        if (passwordInput && toggleBtn) {
            toggleBtn.addEventListener('click', () => {
                const isPassword = passwordInput.getAttribute('type') === 'password';
                passwordInput.setAttribute('type', isPassword ? 'text' : 'password');

                const icon = toggleBtn.querySelector('i');
                if (icon) {
                    icon.classList.toggle('bi-eye');
                    icon.classList.toggle('bi-eye-slash');
                }
            });
        }

        // ======================================
        // 3. تعطيل زر الدخول مع التحقق
        // ======================================
        const loginForm = document.querySelector('form');
        const loginBtn = document.getElementById('loginButton');
        const loginText = document.getElementById('loginText');
        const loadingText = document.getElementById('loadingText');

        if (loginForm && loginBtn) {
            loginForm.addEventListener('submit', function (e) {
                // التحقق من صحة النموذج قبل التعطيل
                if (!loginForm.checkValidity()) {
                    e.preventDefault();
                    return;
                }

                // تعطيل الزر وإظهار التحميل
                loginBtn.disabled = true;
                if (loginText && loadingText) {
                    loginText.classList.add('d-none');
                    loadingText.classList.remove('d-none');
                }
            });
        }

        // ======================================
        // 4. منع إعادة الإرسال عند العودة للخلف
        // ======================================
        if (window.history && window.history.replaceState) {
            window.history.replaceState(null, null, window.location.href);
        }
    });

})();