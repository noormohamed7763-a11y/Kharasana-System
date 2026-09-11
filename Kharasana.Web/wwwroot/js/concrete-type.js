document.addEventListener("DOMContentLoaded", () => {

    // ==========================
    // Elements
    // ==========================

    const standardSection = document.getElementById("standardConcreteSection");
    const customSection = document.getElementById("customConcreteSection");

    const btnCustom = document.getElementById("btnCustomType");
    const btnStandard = document.getElementById("btnStandardType");

    const select = document.getElementById("concreteTypeSelect");

    const usageText = document.getElementById("usageText");
    const strengthText = document.getElementById("strengthText");

    const hiddenName = document.querySelector("input[name='Name']");
    const hiddenStrength = document.querySelector("input[name='Strength']");
    const hiddenIsCustom = document.getElementById("IsCustomType");

    const customName = document.querySelector("input[name='CustomName']");
    const customStrength = document.querySelector("input[name='CustomStrength']");

    const unitPrice = document.querySelector("input[name='UnitPrice']");
    const imageUrl = document.querySelector("input[name='ImageUrl']");

    const previewName = document.getElementById("previewName");
    const previewUsage = document.getElementById("previewUsage");
    const previewStrength = document.getElementById("previewStrength");
    const previewPrice = document.getElementById("previewPrice");
    const previewImage = document.getElementById("previewImage");

    // الصورة الافتراضية المُرقّمة نسخةً من الوسم (تتضمن ?v=Hash) — مصدر وحيد للافتراضية
    const defaultImage = previewImage?.src;

    // ==========================
    // Functions
    // ==========================

    function updatePrice() {

        if (!previewPrice || !unitPrice)
            return;

        const value = parseFloat(unitPrice.value);

        previewPrice.textContent =
            isNaN(value)
                ? "0 ر.ي"
                : value.toLocaleString() + " ر.ي";
    }

    function updateImage() {

        if (!previewImage || !imageUrl)
            return;

        previewImage.src =
            imageUrl.value.trim() !== ""
                ? imageUrl.value
                : defaultImage;
    }

    function updateStandardPreview() {

        if (!select)
            return;

        const option = select.options[select.selectedIndex];

        if (!option || !option.value) {

            usageText.textContent = "اختر نوع الخرسانة لعرض الاستخدام";
            strengthText.textContent = "-";

            previewName.textContent = "لم يتم اختيار نوع";
            previewUsage.textContent = "اختر نوع الخرسانة";
            previewStrength.textContent = "-";

            hiddenName.value = "";
            hiddenStrength.value = "";

            return;
        }

        const usage = option.dataset.usage || "-";
        const strength = option.dataset.strength || "-";

        usageText.textContent = usage;
        strengthText.textContent = strength + " MPa";

        previewName.textContent = option.value;
        previewUsage.textContent = usage;
        previewStrength.textContent = strength + " MPa";

        hiddenName.value = option.value;
        hiddenStrength.value = strength;
    }

    function updateCustomPreview() {

        previewName.textContent =
            customName.value || "نوع مخصص";

        previewUsage.textContent =
            "نوع خرسانة مخصص";

        previewStrength.textContent =
            (customStrength.value || "-") + " MPa";
    }

    // ==========================
    // Switch To Custom
    // ==========================

    btnCustom?.addEventListener("click", () => {

        standardSection.style.display = "none";
        customSection.style.display = "block";

        if (hiddenIsCustom)
            hiddenIsCustom.value = "true";

        updateCustomPreview();

    });

    // ==========================
    // Switch To Standard
    // ==========================

    btnStandard?.addEventListener("click", () => {

        customSection.style.display = "none";
        standardSection.style.display = "block";

        if (hiddenIsCustom)
            hiddenIsCustom.value = "false";

        updateStandardPreview();

    });

    // ==========================
    // Standard Selection
    // ==========================

    select?.addEventListener("change", updateStandardPreview);

    // ==========================
    // Custom Inputs
    // ==========================

    customName?.addEventListener("input", updateCustomPreview);

    customStrength?.addEventListener("input", updateCustomPreview);

    // ==========================
    // Price
    // ==========================

    unitPrice?.addEventListener("input", updatePrice);

    // ==========================
    // Image
    // ==========================

    imageUrl?.addEventListener("input", updateImage);

    // ==========================
    // Submit
    // ==========================

    document
        .getElementById("createConcreteTypeForm")
        ?.addEventListener("submit", () => {

            if (hiddenIsCustom?.value === "true") {

                hiddenName.value = customName.value.trim();
                hiddenStrength.value = customStrength.value;

            }
            else {

                updateStandardPreview();

            }

        });

    // ==========================
    // Initial Load
    // ==========================

    if (hiddenIsCustom)
        hiddenIsCustom.value = "false";

    updateStandardPreview();
    updatePrice();
    updateImage();

});