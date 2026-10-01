import { Component, Input } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsFilterDto } from '../../../proxy/analytics/models';
import { ReportRunnerService } from '../report-runner.service';

interface ReportDef { key: string; title: string; description: string; }
interface ReportGroup { title: string; tone: 'green' | 'blue' | 'amber' | 'purple'; reports: ReportDef[]; }

/** Catalogue of ready-made reports. Keys match AnalyticsAppService.GetReportAsync. */
const GROUPS: ReportGroup[] = [
  {
    title: 'Sales', tone: 'green', reports: [
      { key: 'sales-by-day',         title: 'Sales by day',         description: 'Every day’s invoices, sales, collected and VAT' },
      { key: 'sales-by-product',     title: 'Sales by product',     description: 'Units and sales for each product' },
      { key: 'sales-by-salesperson', title: 'Sales by salesperson', description: 'Invoices and sales per staff member' },
      { key: 'quotes-to-sales',      title: 'Quotes to sales',      description: 'Quote requests and whether they bought' },
      { key: 'discounts',            title: 'Discounts given',      description: 'Every discount, promotion and coupon used' },
    ],
  },
  {
    title: 'Products & stock', tone: 'blue', reports: [
      { key: 'stock-value',      title: 'Stock value',      description: 'What your stock is worth, by product' },
      { key: 'stock-to-reorder', title: 'Stock to reorder', description: 'Items that will run out in the next 2 weeks' },
      { key: 'slow-stock',       title: 'Slow stock',       description: 'Items with no sale in 60 days' },
      { key: 'serial-numbers',   title: 'Serial numbers',   description: 'Every serial sold, with customer and warranty' },
    ],
  },
  {
    title: 'Money', tone: 'amber', reports: [
      { key: 'collections', title: 'Collections',          description: 'Money in by method and day' },
      { key: 'dues-by-age', title: 'Dues by age',          description: 'Who owes what, and how late' },
      { key: 'vat-summary', title: 'VAT summary (Mushak)', description: 'VAT included in sales, for the monthly return' },
      { key: 'refunds',     title: 'Refunds',              description: 'Refunded and returned sales' },
    ],
  },
  {
    title: 'Customers', tone: 'purple', reports: [
      { key: 'top-customers',   title: 'Top customers',        description: 'Biggest buyers and their orders' },
      { key: 'new-customers',   title: 'New customers',        description: 'First-time buyers, their channel and spend' },
      { key: 'warranty-ending', title: 'Warranty ending soon', description: 'Warranties ending in the next 90 days' },
    ],
  },
];

@Component({
  selector:    'an-reports-tab',
  templateUrl: './reports-tab.component.html',
  styleUrls:   ['../analytics-shared.css', './reports-tab.component.css'],
  imports:     [SharedModule],
})
export class ReportsTabComponent {
  @Input({ required: true }) filter!: AnalyticsFilterDto;
  @Input() periodLabel = '';

  readonly groups = GROUPS;

  constructor(readonly runner: ReportRunnerService) {}
}
