// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
    const storageKey = "bizcore.sidebar.scrollTop";

    const restoreSidebarScroll = () => {
        const sidebarNav = document.querySelector(".sidebar-nav");
        if (!sidebarNav) {
            return;
        }

        const savedScrollTop = Number.parseInt(sessionStorage.getItem(storageKey) || "0", 10);
        if (!Number.isNaN(savedScrollTop) && savedScrollTop > 0) {
            sidebarNav.scrollTop = savedScrollTop;
        }

        const saveScrollTop = () => {
            sessionStorage.setItem(storageKey, String(sidebarNav.scrollTop));
        };

        sidebarNav.addEventListener("scroll", saveScrollTop, { passive: true });
        sidebarNav.querySelectorAll("a.sidebar-link").forEach(link => {
            link.addEventListener("click", saveScrollTop);
        });
        window.addEventListener("beforeunload", saveScrollTop);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", restoreSidebarScroll);
    } else {
        restoreSidebarScroll();
    }
})();

(() => {
    const integerQtySelector = ".js-integer-qty";
    const blockedKeys = new Set(["-", "+", ".", ",", "e", "E"]);

    const getMinValue = input => {
        const rawValue = input.dataset.qtyMin;
        const parsedValue = Number.parseInt(rawValue || "", 10);
        return Number.isNaN(parsedValue) ? 0 : parsedValue;
    };

    const sanitizeValue = input => {
        const minValue = getMinValue(input);
        const numericValue = Number.parseFloat((input.value || "").replace(/,/g, ""));

        if (!Number.isFinite(numericValue)) {
            input.value = minValue > 0 ? String(minValue) : "";
            return;
        }

        const parsedValue = Math.trunc(Math.abs(numericValue));
        input.value = String(Math.max(parsedValue, minValue));
    };

    const applyAttributes = root => {
        root.querySelectorAll(integerQtySelector).forEach(input => {
            if (!(input instanceof HTMLInputElement)) {
                return;
            }

            const minValue = getMinValue(input);
            input.setAttribute("inputmode", "numeric");
            input.setAttribute("step", "1");
            input.setAttribute("min", String(minValue));
        });
    };

    const bindIntegerQtyInputs = root => {
        applyAttributes(root);

        root.addEventListener("keydown", event => {
            const input = event.target;
            if (!(input instanceof HTMLInputElement) || !input.matches(integerQtySelector)) {
                return;
            }

            if (blockedKeys.has(event.key)) {
                event.preventDefault();
            }
        });

        root.addEventListener("input", event => {
            const input = event.target;
            if (!(input instanceof HTMLInputElement) || !input.matches(integerQtySelector)) {
                return;
            }

            sanitizeValue(input);
        });

        root.addEventListener("blur", event => {
            const input = event.target;
            if (!(input instanceof HTMLInputElement) || !input.matches(integerQtySelector)) {
                return;
            }

            sanitizeValue(input);
        }, true);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => bindIntegerQtyInputs(document));
    } else {
        bindIntegerQtyInputs(document);
    }
})();

(() => {
    const toggleSelector = ".js-confirm-popover-toggle";
    let activeToggle = null;

    const positionPopover = toggle => {
        if (!(toggle instanceof HTMLElement)) {
            return;
        }

        const targetId = toggle.dataset.confirmTarget;
        if (!targetId) {
            return;
        }

        const popover = document.getElementById(targetId);
        if (!popover || popover.hidden) {
            return;
        }

        const spacing = 10;
        const viewportPadding = 12;
        const toggleRect = toggle.getBoundingClientRect();
        const popoverWidth = popover.offsetWidth || 280;
        const popoverHeight = popover.offsetHeight || 180;
        const maxLeft = Math.max(viewportPadding, window.innerWidth - popoverWidth - viewportPadding);
        const preferredLeft = toggleRect.right - popoverWidth;
        const left = Math.max(viewportPadding, Math.min(preferredLeft, maxLeft));
        const fitsBelow = toggleRect.bottom + spacing + popoverHeight <= window.innerHeight - viewportPadding;
        const top = fitsBelow
            ? toggleRect.bottom + spacing
            : Math.max(viewportPadding, toggleRect.top - popoverHeight - spacing);

        popover.style.left = `${left}px`;
        popover.style.top = `${top}px`;
    };

    const closePopover = toggle => {
        if (!(toggle instanceof HTMLElement)) {
            return;
        }

        const targetId = toggle.dataset.confirmTarget;
        if (!targetId) {
            return;
        }

        const popover = document.getElementById(targetId);
        if (!popover) {
            return;
        }

        popover.hidden = true;
        popover.classList.remove("is-open");
        popover.style.left = "";
        popover.style.top = "";
        toggle.setAttribute("aria-expanded", "false");
        if (activeToggle === toggle) {
            activeToggle = null;
        }
    };

    const openPopover = toggle => {
        if (!(toggle instanceof HTMLElement)) {
            return;
        }

        document.querySelectorAll(toggleSelector).forEach(otherToggle => {
            if (otherToggle !== toggle) {
                closePopover(otherToggle);
            }
        });

        const targetId = toggle.dataset.confirmTarget;
        if (!targetId) {
            return;
        }

        const popover = document.getElementById(targetId);
        if (!popover) {
            return;
        }

        popover.hidden = false;
        popover.classList.add("is-open");
        toggle.setAttribute("aria-expanded", "true");
        activeToggle = toggle;
        positionPopover(toggle);
    };

    const bindConfirmPopovers = root => {
        root.addEventListener("click", event => {
            const cancelButton = event.target.closest(".js-confirm-popover-cancel");
            if (cancelButton) {
                const shell = cancelButton.closest(".confirm-popover-shell");
                const toggle = shell?.querySelector(toggleSelector);
                closePopover(toggle);
                return;
            }

            const toggle = event.target.closest(toggleSelector);
            if (toggle) {
                const isExpanded = toggle.getAttribute("aria-expanded") === "true";
                if (isExpanded) {
                    closePopover(toggle);
                } else {
                    openPopover(toggle);
                }
                return;
            }

            document.querySelectorAll(toggleSelector).forEach(existingToggle => {
                if (!event.target.closest(".confirm-popover-shell")) {
                    closePopover(existingToggle);
                }
            });
        });

        root.addEventListener("keydown", event => {
            if (event.key !== "Escape") {
                return;
            }

            document.querySelectorAll(toggleSelector).forEach(toggle => closePopover(toggle));
        });

        window.addEventListener("resize", () => {
            if (activeToggle) {
                positionPopover(activeToggle);
            }
        });

        window.addEventListener("scroll", () => {
            if (activeToggle) {
                positionPopover(activeToggle);
            }
        }, true);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => bindConfirmPopovers(document));
    } else {
        bindConfirmPopovers(document);
    }
})();
