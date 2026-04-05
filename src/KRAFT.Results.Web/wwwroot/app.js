window.Kraftresults = {
    showDialog: function (id) {
        const dialog = document.getElementById(id);
        if (dialog && !dialog.open) {
            if (!dialog._cancelHandler) {
                dialog._cancelHandler = function(e) {
                    if (dialog.querySelector('[aria-busy="true"]')) {
                        e.preventDefault();
                    }
                };
                dialog.addEventListener('cancel', dialog._cancelHandler);
            }
            dialog._triggerElement = document.activeElement;
            dialog.showModal();
        }
    },
    selectInput: function (id) {
        const el = document.getElementById(id);
        if (el) { el.select(); }
    },
    setupAttemptInput: function (id) {
        const el = document.getElementById(id);
        if (!el) { return; }
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Tab' || e.key === 'Enter') {
                e.preventDefault();
            }
        });
        el.select();
    },
    closeDialog: function (id) {
        const dialog = document.getElementById(id);
        if (dialog && dialog.open) {
            const trigger = dialog._triggerElement;
            dialog.close();
            if (trigger && typeof trigger.focus === 'function') {
                trigger.focus();
            }
        }
    }
};