// wwwroot/js/form-scroll.js
(function () {
    'use strict';

    var KEY = 'books-form-scroll';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.querySelector('form[data-keep-scroll]');
        var saved = sessionStorage.getItem(KEY);

        if (saved !== null) {
            sessionStorage.removeItem(KEY);
            // Возвращаем прокрутку только если это та же форма,
            // а не страница Details после успешного сохранения
            if (form) {
                window.scrollTo(0, parseInt(saved, 10) || 0);
            }
        }

        if (form) {
            form.addEventListener('submit', function () {
                sessionStorage.setItem(KEY, String(window.scrollY));
            });
        }
    });
})();