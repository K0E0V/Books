// wwwroot/js/isbn-mask.js
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('input[data-isbn-mask]').forEach(initIsbnMask);
    });

    function digitsOf(value) {
        return value.replace(/[^0-9Xx]/g, '').toUpperCase();
    }

    function countDigits(value) {
        return digitsOf(value).length;
    }

    function join(parts) {
        return parts.filter(function (p) { return p.length > 0; }).join('-');
    }

    function format(digits) {
        if (digits.length === 0) return '';

        // ISBN-13: 000-0-000-00000-0
        if (digits.length > 10 || digits.indexOf('978') === 0 || digits.indexOf('979') === 0) {
            return join([
                digits.slice(0, 3),
                digits.slice(3, 4),
                digits.slice(4, 7),
                digits.slice(7, 12),
                digits.slice(12, 13)
            ]);
        }

        // ISBN-10: 0-000-00000-0
        return join([
            digits.slice(0, 1),
            digits.slice(1, 4),
            digits.slice(4, 9),
            digits.slice(9, 10)
        ]);
    }

    // Позиция сразу после n-й по счёту цифры
    function caretAfterDigits(value, n) {
        var seen = 0;
        for (var i = 0; i < value.length; i++) {
            if (/[0-9X]/i.test(value[i])) {
                if (seen === n) return i;
                seen++;
            }
        }
        return value.length;
    }

    function initIsbnMask(input) {
        input.addEventListener('keydown', function (e) {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            if (['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Tab'].indexOf(e.key) !== -1) return;
            if (!/^[0-9xX]$/.test(e.key)) e.preventDefault();
        });

        input.addEventListener('input', function () {
            var caret = input.selectionStart || 0;
            // Сколько цифр было до каретки ДО форматирования
            var beforeDigits = countDigits(input.value.slice(0, caret));

            var digits = digitsOf(input.value);
            if (digits.length > 13) digits = digits.slice(0, 13);

            var formatted = format(digits);

            // Ничего не изменилось — не трогаем value и каретку
            if (formatted === input.value) return;

            input.value = formatted;

            var pos = caretAfterDigits(formatted, beforeDigits);
            input.setSelectionRange(pos, pos);
        });

        input.addEventListener('blur', function () {
            if (countDigits(input.value) === 0) input.value = '';
        });
    }
})();