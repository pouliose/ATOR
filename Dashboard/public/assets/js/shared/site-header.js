const navigationItems = [
  { id: "tracks", label: "Current tracks", href: "/" },
  { id: "history", label: "Track history", href: "/history.html" },
  { id: "alerts", label: "Alerts", href: "/alerts.html" },
];

class AtorSiteHeader extends HTMLElement {
  connectedCallback() {
    if (this.hasChildNodes()) {
      return;
    }

    const header = document.createElement("header");
    header.className = "topbar";

    const brand = document.createElement("div");
    brand.className = "brand";
    brand.setAttribute("aria-label", "ATOR Operations");

    const brandMark = document.createElement("span");
    brandMark.className = "brand-mark";
    brandMark.setAttribute("aria-hidden", "true");
    brandMark.textContent = "A";

    const brandName = document.createElement("span");
    brandName.textContent = "ATOR Operations";
    brand.append(brandMark, brandName);

    const navigation = document.createElement("nav");
    navigation.className = "topbar-nav";
    navigation.setAttribute("aria-label", "Main navigation");

    for (const item of navigationItems) {
      const link = document.createElement("a");
      link.className = "nav-link";
      link.href = item.href;
      link.textContent = item.label;
      if (item.id === this.getAttribute("current-page")) {
        link.classList.add("nav-link-active");
        link.setAttribute("aria-current", "page");
      }

      navigation.append(link);
    }

    header.append(brand, navigation);
    this.replaceChildren(header);
  }
}

customElements.define("ator-site-header", AtorSiteHeader);
