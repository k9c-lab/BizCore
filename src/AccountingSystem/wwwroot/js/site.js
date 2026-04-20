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
