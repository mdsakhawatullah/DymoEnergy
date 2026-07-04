import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="coming-soon-container">
      <div class="coming-soon-content">
        <i class="fas fa-tools"></i>
        <h1>{{ title }}</h1>
        <p>This feature is coming soon!</p>
      </div>
    </div>
  `,
  styles: [`
    .coming-soon-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .coming-soon-content {
      text-align: center;
      color: white;
      padding: 2rem;
      border-radius: 10px;
      background: rgba(0, 0, 0, 0.2);
    }

    i {
      font-size: 4rem;
      margin-bottom: 1rem;
      display: block;
    }

    h1 {
      font-size: 2.5rem;
      margin-bottom: 1rem;
    }

    p {
      font-size: 1.25rem;
    }
  `],
})
export class ComingSoonComponent {
  title = 'Coming Soon';

  constructor() {}
}
