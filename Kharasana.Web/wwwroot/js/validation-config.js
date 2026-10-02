// ==========================
// Validation Configuration — ربط مكتبة jQuery Validation مع Bootstrap 5
// ==========================
$.validator.setDefaults({
    highlight: function(element) {
        $(element).addClass('is-invalid').removeClass('is-valid');
    },
    unhighlight: function(element) {
        $(element).removeClass('is-invalid').addClass('is-valid');
    }
});
