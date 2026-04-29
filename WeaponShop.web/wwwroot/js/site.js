(() => {
    const toggles = Array.from(document.querySelectorAll("[data-nav-toggle]"));
    if (toggles.length === 0) {
        return;
    }

    const closeNav = (toggle, panel, header) => {
        toggle.setAttribute("aria-expanded", "false");
        panel.setAttribute("hidden", "");
        header?.classList.remove("is-nav-open");
    };

    const openNav = (toggle, panel, header) => {
        toggle.setAttribute("aria-expanded", "true");
        panel.removeAttribute("hidden");
        header?.classList.add("is-nav-open");
    };

    toggles.forEach((toggle) => {
        const targetId = toggle.getAttribute("data-nav-target");
        if (!targetId) {
            return;
        }

        const panel = document.getElementById(targetId);
        const header = toggle.closest(".site-header");
        if (!panel) {
            return;
        }

        const syncWithViewport = () => {
            if (window.innerWidth <= 920) {
                if (toggle.getAttribute("aria-expanded") !== "true") {
                    panel.setAttribute("hidden", "");
                }
            } else {
                panel.removeAttribute("hidden");
                toggle.setAttribute("aria-expanded", "false");
                header?.classList.remove("is-nav-open");
            }
        };

        toggle.addEventListener("click", () => {
            const isExpanded = toggle.getAttribute("aria-expanded") === "true";
            if (isExpanded) {
                closeNav(toggle, panel, header);
            } else {
                openNav(toggle, panel, header);
            }
        });

        window.addEventListener("resize", syncWithViewport);

        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                closeNav(toggle, panel, header);
            }
        });

        syncWithViewport();
    });
})();
