import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="cs-wrap">

      <div class="cs-graphic">
        <div class="cs-orbit outer">
          <div class="cs-dot d1"></div>
          <div class="cs-dot d2"></div>
          <div class="cs-dot d3"></div>
        </div>
        <div class="cs-orbit inner">
          <div class="cs-dot d4"></div>
          <div class="cs-dot d5"></div>
        </div>
        <div class="cs-center-icon">
          <i class="fas fa-layer-group"></i>
        </div>
      </div>

      <h2 class="cs-title">Under Construction</h2>
      <p class="cs-sub">This section is being built and will be available soon.</p>

      <div class="cs-pills">
        <span><i class="fas fa-check-circle"></i> Designed</span>
        <span class="active"><i class="fas fa-spinner fa-spin"></i> In Development</span>
        <span class="muted"><i class="far fa-circle"></i> Launching</span>
      </div>

    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: calc(100vh - 120px);
      overflow: hidden;
    }

    .cs-wrap {
      height: 100%;
      background: #fff;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 1.2rem;
    }

    /* Orbit graphic */
    .cs-graphic {
      position: relative;
      width: 160px;
      height: 160px;
    }

    .cs-orbit {
      position: absolute;
      border-radius: 50%;
      border: 1.5px dashed #d1d5db;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      animation: spin 12s linear infinite;
    }

    .cs-orbit.outer { width: 150px; height: 150px; }
    .cs-orbit.inner { width: 90px; height: 90px; animation-direction: reverse; animation-duration: 8s; }

    @keyframes spin { to { transform: translate(-50%, -50%) rotate(360deg); } }

    .cs-dot {
      position: absolute;
      width: 10px; height: 10px;
      border-radius: 50%;
    }

    .d1 { background: #4a6cf7; top: -5px; left: 50%; margin-left: -5px; }
    .d2 { background: #f59e0b; bottom: 10px; right: 5px; }
    .d3 { background: #10b981; bottom: 10px; left: 5px; }
    .d4 { background: #ec4899; top: -5px; left: 50%; margin-left: -5px; }
    .d5 { background: #8b5cf6; bottom: -5px; left: 50%; margin-left: -5px; }

    .cs-center-icon {
      position: absolute;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      width: 52px; height: 52px;
      background: #f0f4ff;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.4rem;
      color: #4a6cf7;
    }

    /* Text */
    .cs-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }

    .cs-sub {
      font-size: 0.87rem;
      color: #6b7280;
      margin: 0;
    }

    /* Pills */
    .cs-pills {
      display: flex;
      gap: 0.6rem;
    }

    .cs-pills span {
      font-size: 0.78rem;
      font-weight: 500;
      padding: 0.3rem 0.75rem;
      border-radius: 999px;
      background: #ecfdf5;
      color: #059669;
      border: 1px solid #d1fae5;
      display: flex;
      align-items: center;
      gap: 0.3rem;
    }

    .cs-pills span.active {
      background: #eff6ff;
      color: #2563eb;
      border-color: #bfdbfe;
    }

    .cs-pills span.muted {
      background: #f9fafb;
      color: #9ca3af;
      border-color: #e5e7eb;
    }
  `],
})
export class ComingSoonComponent {}
