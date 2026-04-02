export function initialize(dotNetRef) {
    const onKeyDown = (e) => {
        if (e.key === 'Escape') {
            dotNetRef.invokeMethodAsync('CloseFromJs');
            focusTrigger();
        }
    };

    const onClick = (e) => {
        const wrapper = document.getElementById('user-dropdown-wrapper');
        const dropdownMenu = document.getElementById('user-dropdown');
        if (wrapper && dropdownMenu && !wrapper.contains(e.target)) {
            dotNetRef.invokeMethodAsync('CloseFromJs');
            focusTrigger();
        }
    };

    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('click', onClick, true);

    return {
        dispose: () => {
            document.removeEventListener('keydown', onKeyDown);
            document.removeEventListener('click', onClick, true);
        }
    };
}

export function focusFirstDropdownItem() {
    const item = document.querySelector('#user-dropdown a, #user-dropdown button');
    item?.focus();
}

function focusTrigger() {
    const trigger = document.querySelector('.user-button');
    trigger?.focus();
}
