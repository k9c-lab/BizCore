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
    const iconMap = new Map([
        ["home", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M3 10.5 12 3l9 7.5'/><path d='M5.5 9.5V20h13V9.5'/></svg>"],
        ["financialoverview", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M4 19h16'/><path d='M7 15V9'/><path d='M12 15V5'/><path d='M17 15v-3'/></svg>"],
        ["inventoryoverview", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='m3 8.5 9-5 9 5-9 5z'/><path d='m3 8.5 9 5 9-5'/><path d='M12 13.5v7'/></svg>"],
        ["announcements", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M4 11V6a2 2 0 0 1 2-2h9l5 5v2'/><path d='M14 4v5h5'/><path d='M5 15h14'/><path d='M7 19h10'/></svg>"],
        ["reports", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M6 3h9l4 4v14H6z'/><path d='M15 3v4h4'/><path d='M9 12h6'/><path d='M9 16h6'/><path d='M9 8h2'/></svg>"],
        ["quotations", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><rect x='4' y='3' width='16' height='18' rx='2'/><path d='M8 8h8'/><path d='M8 12h8'/><path d='M8 16h5'/></svg>"],
        ["invoices", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M6 3h12v18l-3-2-3 2-3-2-3 2z'/><path d='M9 8h6'/><path d='M9 12h6'/></svg>"],
        ["cashsales", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M6 7h12l-1 12H7z'/><path d='M9 7a3 3 0 0 1 6 0'/></svg>"],
        ["billingnotes", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><rect x='4' y='4' width='16' height='16' rx='2'/><path d='M8 9h8'/><path d='M8 13h8'/><path d='M8 17h5'/></svg>"],
        ["payments", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><rect x='3' y='6' width='18' height='12' rx='2'/><path d='M3 10h18'/><path d='M16 14h2'/></svg>"],
        ["receipts", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M7 3h10v18l-2.5-1.5L12 21l-2.5-1.5L7 21z'/><path d='M9 8h6'/><path d='M9 12h6'/></svg>"],
        ["purchaserequests", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M6 3h9l4 4v14H6z'/><path d='M15 3v4h4'/><path d='M9 12h6'/><path d='M9 16h4'/></svg>"],
        ["purchaseorders", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M4 7h16'/><path d='M6 7V5h12v2'/><path d='M6 7l1 12h10l1-12'/></svg>"],
        ["receivings", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='m12 3 9 5-9 5-9-5z'/><path d='M3 8v8l9 5 9-5V8'/></svg>"],
        ["supplierpayments", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 3v18'/><path d='M17 7.5c0-1.9-2.2-3.5-5-3.5s-5 1.6-5 3.5 2.2 3.5 5 3.5 5 1.6 5 3.5-2.2 3.5-5 3.5-5-1.6-5-3.5'/></svg>"],
        ["stockinquiry", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='m3 8.5 9-5 9 5-9 5z'/><path d='m3 8.5 9 5 9-5'/><path d='M12 13.5v7'/></svg>"],
        ["serialinquiry", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><circle cx='11' cy='11' r='6'/><path d='m20 20-4.2-4.2'/><path d='M9 11h4'/></svg>"],
        ["stockledger", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 0-3 3z'/><path d='M5 4v16'/><path d='M9 9h6'/><path d='M9 13h6'/></svg>"],
        ["stockaudit", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M4 5h16v10H4z'/><path d='m9 19 3-3 3 3'/><path d='M12 16V9'/></svg>"],
        ["stocktransfers", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M7 7h11'/><path d='m14 4 4 3-4 3'/><path d='M17 17H6'/><path d='m10 14-4 3 4 3'/></svg>"],
        ["stockissues", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='m12 3 9 5-9 5-9-5z'/><path d='M3 8v8l9 5'/><path d='M21 8v4'/><path d='M17 14h8'/></svg>"],
        ["customerclaims", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><circle cx='9' cy='8' r='3'/><path d='M4 19a5 5 0 0 1 10 0'/><path d='M16 11h5'/><path d='M18.5 8.5v5'/></svg>"],
        ["supplierclaims", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 3 5 6v5c0 5 3.4 8.8 7 10 3.6-1.2 7-5 7-10V6z'/><path d='m9.5 12 1.7 1.7 3.3-3.4'/></svg>"],
        ["branches", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M4 21V7l8-4 8 4v14'/><path d='M9 21v-6h6v6'/></svg>"],
        ["customers", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='8' r='4'/><path d='M5 20a7 7 0 0 1 14 0'/></svg>"],
        ["suppliers", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M3 21h18'/><path d='M5 21V7l7-4 7 4v14'/><path d='M9 12h6'/></svg>"],
        ["salespersons", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><circle cx='9' cy='8' r='3'/><path d='M4 19a5 5 0 0 1 10 0'/><path d='M16 7h5'/><path d='M16 11h5'/></svg>"],
        ["treatmentrights", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 21s-6-4.35-6-10a6 6 0 0 1 12 0c0 5.65-6 10-6 10Z'/><path d='M9.5 11.5 11 13l3.5-3.5'/></svg>"],
        ["referringdoctors", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 4v16'/><path d='M7 9h10'/><circle cx='12' cy='12' r='8'/></svg>"],
        ["items", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='m3 8.5 9-5 9 5-9 5z'/><path d='m3 8.5 9 5 9-5'/><path d='M12 13.5v7'/></svg>"],
        ["users", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='8' r='3.5'/><path d='M5 20a7 7 0 0 1 14 0'/></svg>"],
        ["rolepermissions", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><rect x='4' y='5' width='16' height='14' rx='2'/><path d='M8 10h8'/><path d='M8 14h5'/></svg>"],
        ["pricelevels", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 3v18'/><path d='M17 7.5c0-1.9-2.2-3.5-5-3.5s-5 1.6-5 3.5 2.2 3.5 5 3.5 5 1.6 5 3.5-2.2 3.5-5 3.5-5-1.6-5-3.5'/></svg>"],
        ["settings", "<svg viewBox='0 0 24 24' fill='none' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'><path d='M12 15.5A3.5 3.5 0 1 0 12 8.5a3.5 3.5 0 0 0 0 7Z'/><path d='M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 .6 1.7 1.7 0 0 0-.4 1.08V21a2 2 0 1 1-4 0v-.1A1.7 1.7 0 0 0 8.6 19.4a1.7 1.7 0 0 0-1.88.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-.6-1 1.7 1.7 0 0 0-1.08-.4H2.8a2 2 0 1 1 0-4h.1A1.7 1.7 0 0 0 4.6 8.6a1.7 1.7 0 0 0-.34-1.88l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.7 1.7 0 0 0 8.6 4.6c.38 0 .74-.14 1-.4A1.7 1.7 0 0 0 10 3.12V3a2 2 0 1 1 4 0v.1c0 .4.14.78.4 1.08.26.26.62.42 1 .42a1.7 1.7 0 0 0 1.88-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.7 1.7 0 0 0 19.4 8.6c0 .38.14.74.4 1 .3.26.68.4 1.08.4h.1a2 2 0 1 1 0 4h-.1c-.4 0-.78.14-1.08.4-.26.26-.4.62-.4 1Z'/></svg>"]
    ]);

    const normalizeController = href => {
        try {
            const url = new URL(href, window.location.origin);
            const segments = url.pathname.split("/").filter(Boolean);
            if (segments.length === 0) {
                return "home";
            }

            return segments[0].toLowerCase();
        } catch {
            return "";
        }
    };

    const enhanceSidebarIcons = () => {
        document.querySelectorAll(".sidebar-nav a.sidebar-link").forEach(link => {
            if (!(link instanceof HTMLAnchorElement) || link.querySelector(".sidebar-link-icon")) {
                return;
            }

            const controller = normalizeController(link.href);
            const iconMarkup = iconMap.get(controller);
            if (!iconMarkup) {
                return;
            }

            const icon = document.createElement("span");
            icon.className = "sidebar-link-icon";
            icon.setAttribute("aria-hidden", "true");
            icon.innerHTML = iconMarkup;
            link.prepend(icon);
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", enhanceSidebarIcons);
    } else {
        enhanceSidebarIcons();
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
