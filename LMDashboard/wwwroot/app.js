// Client-side helpers that save Blazor Server round trips. Delegated from document so they
// keep working as Blazor re-renders the elements.

// Blazor can't call showModal() itself; LinkDialog invokes these through JS interop.
window.lmDialog = {
    open: (dialog) => { if (!dialog.open) dialog.showModal(); },
    close: (dialog) => { if (dialog.open) dialog.close(); }
};

// A click landing on the dialog element itself, rather than its content, is a backdrop click.
// The press must start there too, so a text selection dragged out of a field doesn't close it.
// It raises "cancel" like Esc does, and LinkDialog lets the server close the dialog.
let pressedOnBackdrop = false;

document.addEventListener("pointerdown", (event) => {
    pressedOnBackdrop = event.target.matches?.("dialog.lm-dialog") ?? false;
});

document.addEventListener("click", (event) => {
    if (pressedOnBackdrop && event.target.matches?.("dialog.lm-dialog")) {
        event.target.dispatchEvent(new Event("cancel", { cancelable: true }));
    }
    pressedOnBackdrop = false;
});

// Enter submits the dialog's form, but @bind only reads a field on "change", which browsers
// don't reliably fire before an Enter submit. Commit the focused field first so Save sees what
// was just typed. A window capture listener runs ahead of Blazor's own handlers.
window.addEventListener("submit", (event) => {
    const field = document.activeElement;
    if (event.target.closest?.(".lm-dialog") && field?.form === event.target
        && field.matches("input:not([type=checkbox]):not([type=radio])")) {
        field.dispatchEvent(new Event("change", { bubbles: true }));
    }
}, true);

const searchDelayMs = 150;
let searchTimer;

document.addEventListener("input", (event) => {
    const el = event.target;

    // Search binds on "change"; raise it once typing pauses rather than per keystroke.
    if (el.matches?.(".lm-search-input")) {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => el.dispatchEvent(new Event("change", { bubbles: true })), searchDelayMs);
        return;
    }

    // Brightness previews locally while dragging; the server only hears the final "change".
    if (el.matches?.(".lm-brightness-slider")) {
        el.closest(".lm-dashboard")?.style.setProperty("--lm-brightness", el.value);

        // Update the existing text node in place so Blazor's reference to it stays valid.
        const readout = el.parentElement?.querySelector(".lm-brightness-value")?.firstChild;
        if (readout) {
            readout.nodeValue = Number(el.value).toFixed(2);
        }
    }
});
