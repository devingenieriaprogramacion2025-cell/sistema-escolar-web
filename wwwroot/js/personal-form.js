(function () {
    'use strict';

    function formatRut(value) {
        const clean = value.toUpperCase().replace(/[^0-9K]/g, '').slice(0, 9);
        if (clean.length <= 1) {
            return clean;
        }

        const number = clean.slice(0, -1).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        return `${number}-${clean.slice(-1)}`;
    }

    function formatPhone(value) {
        let digits = value.replace(/\D/g, '');
        if (!digits) {
            return '';
        }

        if (digits.startsWith('56')) {
            digits = digits.slice(2);
        }

        digits = digits.slice(0, 9);
        if (!digits) {
            return '';
        }

        return `+56${digits}`;
    }

    function applyMask(input, formatter) {
        input.addEventListener('input', function () {
            const formatted = formatter(input.value);
            if (input.value !== formatted) {
                input.value = formatted;
            }
        });

        input.addEventListener('blur', function () {
            input.value = formatter(input.value);
        });
    }

    document.querySelectorAll('[data-rut-chileno]').forEach(function (input) {
        applyMask(input, formatRut);
    });

    document.querySelectorAll('[data-telefono-chileno]').forEach(function (input) {
        applyMask(input, formatPhone);
    });
}());
