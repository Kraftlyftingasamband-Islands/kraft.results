export function initialize(dotNetRef) {
    const handler = (e) => {
        if (e.key === 'Escape') {
            dotNetRef.invokeMethodAsync('CloseFromJs');
        }
    };
    document.addEventListener('keydown', handler);
    return { dispose: () => document.removeEventListener('keydown', handler) };
}
