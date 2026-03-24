window.Kraftresults = {
    showDialog: function (id) {
        const dialog = document.getElementById(id);
        if (dialog && !dialog.open) {
            if (!dialog._cancelHandler) {
                dialog._cancelHandler = function(e) { e.preventDefault(); };
                dialog.addEventListener('cancel', dialog._cancelHandler);
            }
            dialog._triggerElement = document.activeElement;
            dialog.showModal();
        }
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