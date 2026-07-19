import { Component, AfterViewInit, OnDestroy } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent implements AfterViewInit, OnDestroy {
  private observer: MutationObserver | null = null;

  ngAfterViewInit() {
    this.observer = new MutationObserver(() => this.wireHamburger());
    this.observer.observe(document.body, { childList: true, subtree: true });
    setTimeout(() => this.wireHamburger(), 500);
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }

  private wireHamburger() {
    const breadcrumbContainer = document.querySelector('.lpx-breadcrumb-container');
    if (!breadcrumbContainer || breadcrumbContainer.querySelector('.topbar-hamburger')) return;

    const btn = document.createElement('button');
    btn.className = 'topbar-hamburger';
    btn.innerHTML = '<i class="bi bi-filter-left"></i>';
    btn.addEventListener('click', () => {
      const wrapper = document.getElementById('lpx-wrapper');
      if (!wrapper) return;
      wrapper.classList.toggle('sidebar-minimized');
      const isMinimized = wrapper.classList.contains('sidebar-minimized');
      const sc = wrapper.querySelector('.lpx-sidebar-container') as HTMLElement;
      if (sc) {
        if (isMinimized) {
          sc.style.setProperty('left', '-208px', 'important');
        } else {
          sc.style.removeProperty('left');
        }
      }
    });
    breadcrumbContainer.insertBefore(btn, breadcrumbContainer.firstChild);

    // Hide the original sidebar hamburger
    const sidebarHamburger = document.querySelector('.menu-collapse-icon') as HTMLElement;
    if (sidebarHamburger) {
      sidebarHamburger.style.display = 'none';
    }
  }
}
