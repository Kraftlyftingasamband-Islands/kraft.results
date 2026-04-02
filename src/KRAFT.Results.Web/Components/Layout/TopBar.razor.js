export function initialize(dotNetRef) {
    const onKeyDown = (e) => {
        if (e.key === 'Escape') {
            dotNetRef.invokeMethodAsync('CloseFromJs');
        }
    };

    const onClick = (e) => {
        const dropdown = document.getElementById('user-dropdown-wrapper');
        if (dropdown && !dropdown.contains(e.target)) {
            dotNetRef.invokeMethodAsync('CloseFromJs');
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
