import { Component, AfterViewInit, OnDestroy, inject } from '@angular/core';
import { ConfigStateService, DynamicLayoutComponent, RoutesService, eLayoutType } from '@abp/ng.core';
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
  private routes = inject(RoutesService);
  private config = inject(ConfigStateService);

  ngAfterViewInit() {
    this.fixAdministrationMenu();
    this.observer = new MutationObserver(() => this.wireHamburger());
    this.observer.observe(document.body, { childList: true, subtree: true });
    setTimeout(() => this.wireHamburger(), 500);
  }

  // ABP's default Administration/Identity/Settings menu entries ship with
  // Font Awesome 4/5 icon classes (fa-id-card-o, fa-cog) that don't exist in
  // the bundled FA6 free package, and no `layout` set (so navigating into
  // them loses the sidebar). Patch them in place after all modules have
  // registered their routes.
  private fixAdministrationMenu() {
    this.routes.patch('AbpUiNavigation::Menu:Administration', {
      iconClass: 'bi bi-gear-wide-connected',
      layout: eLayoutType.application,
    });
    this.routes.patch('AbpIdentity::Menu:IdentityManagement', {
      iconClass: 'bi bi-person-badge',
      layout: eLayoutType.application,
    });
    this.routes.patch('AbpIdentity::Roles', {
      iconClass: 'bi bi-person-badge',
      layout: eLayoutType.application,
    });
    this.routes.patch('AbpIdentity::Users', {
      iconClass: 'bi bi-person',
      layout: eLayoutType.application,
    });
    this.routes.patch('AbpSettingManagement::Settings', {
      iconClass: 'bi bi-gear',
      layout: eLayoutType.application,
    });
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }

  private wireHamburger() {
    this.wireTopbarExtras();
    this.wireUserAvatar();

    const breadcrumbContainer = document.querySelector('.lpx-breadcrumb-container');
    if (!breadcrumbContainer || breadcrumbContainer.querySelector('.topbar-hamburger')) return;

    const btn = document.createElement('button');
    btn.className = 'topbar-hamburger';
    btn.setAttribute('aria-label', 'Collapse sidebar');
    btn.innerHTML =
      '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.8" stroke-linecap="round" aria-hidden="true">' +
      '<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16"/></svg>' +
      '<span class="topbar-divider"></span>';
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

  // The top bar in the design carries a search field and a notification bell
  // that ABP's toolbar does not provide, so they are inserted alongside it.
  private wireTopbarExtras() {
    const topbar = document.querySelector('.lpx-topbar');
    // ABP only renders its toolbar once the session resolves, so anchor to the
    // top bar itself and slot in before the toolbar when it shows up.
    if (!topbar || topbar.querySelector('.topbar-actions')) return;

    const actions = document.createElement('div');
    actions.className = 'topbar-actions';

    const search = document.createElement('label');
    search.className = 'topbar-search';
    search.innerHTML =
      '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.8" aria-hidden="true"><circle cx="11" cy="11" r="7"/>' +
      '<path d="M20 20l-3.5-3.5" stroke-linecap="round"/></svg>' +
      '<input type="search" aria-label="Search" placeholder="Search orders, products, customers…" />' +
      '<span class="topbar-search__kbd">Ctrl K</span>';

    const bell = document.createElement('button');
    bell.type = 'button';
    bell.className = 'topbar-bell';
    bell.setAttribute('aria-label', 'Notifications');
    bell.innerHTML =
      '<svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.7" stroke-linejoin="round" aria-hidden="true">' +
      '<path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 0 1-3.4 0"/></svg>' +
      '<span class="topbar-bell__dot"></span>';

    actions.append(search, bell);

    const toolbar = topbar.querySelector('lpx-toolbar-container, .lpx-topbar-content');
    if (toolbar) {
      topbar.insertBefore(actions, toolbar);
    } else {
      topbar.append(actions);
    }
  }

  // ABP's profile item shows a generic person glyph; the design calls for a
  // forest-green circle with the user's initials, which CSS alone can't derive.
  private wireUserAvatar() {
    const profile = document.querySelector('.lpx-topbar abp-user-profile');
    const link = profile?.closest('.lpx-menu-item-link') as HTMLElement | null;
    if (!link || link.querySelector('.topbar-avatar')) return;

    const user = this.config.getOne('currentUser');
    const name: string = user?.name || user?.userName || '';
    if (!name) return;

    const parts = name.trim().split(/\s+/);
    const initials = (parts.length > 1
      ? parts[0][0] + parts[1][0]
      : name.slice(0, 2)
    ).toUpperCase();

    const badge = document.createElement('span');
    badge.className = 'topbar-avatar';
    badge.textContent = initials;
    link.prepend(badge);
    link.classList.add('has-topbar-avatar');
  }
}
