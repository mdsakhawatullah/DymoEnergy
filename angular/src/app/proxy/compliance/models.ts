export enum ComplianceItemKind {
  Badge = 1,
  DocType = 2,
  Template = 3,
  Retention = 4,
  AccessRule = 5,
  Reminder = 6,
  FilingRecord = 7,
  ImportPaper = 8,
}

export enum ComplianceFilingStatus {
  NotStarted = 0,
  InProgress = 1,
  Filed = 2,
  UpToDate = 3,
}

export enum ComplianceDocStatus {
  Pending = 0,
  Done = 1,
  InProgress = 2,
  Issue = 3,
}

export enum ComplianceFileOwner {
  Licence = 1,
  Certificate = 2,
  Template = 3,
  ImportPaper = 4,
  Filing = 5,
}

export interface ComplianceLabelDto {
  key: string;
  group: string;
  caption: string;
  value: string;
}

export interface ComplianceSettingDto {
  accentColor: string;
  expiringDays: number;
  importRequiredDocs?: string;
  labels: ComplianceLabelDto[];
}

export interface UpdateComplianceSettingDto {
  accentColor: string;
  expiringDays: number;
  importRequiredDocs?: string | null;
  labels: Record<string, string>;
}

export type ComplianceTone = 'ok' | 'soon' | 'expired' | 'none' | 'unset' | 'missing' | 'overdue' | 'done';

export interface ComplianceLicenceDto {
  id: number;
  name: string;
  description?: string;
  number?: string;
  issuedBy?: string;
  owner?: string;
  issuedOn?: string | null;
  validUntil?: string | null;
  hasExpiry: boolean;
  order: number;
  daysLeft?: number | null;
  tone: ComplianceTone;
  statusText: string;
  fraction: number;
}

export interface CreateUpdateComplianceLicenceDto {
  name: string;
  description?: string | null;
  number?: string | null;
  issuedBy?: string | null;
  owner?: string | null;
  issuedOn?: string | null;
  validUntil?: string | null;
  hasExpiry: boolean;
  order: number;
}

export interface ComplianceFilingDto {
  id: number;
  title: string;
  detail?: string;
  dueDate: string;
  status: ComplianceFilingStatus;
  owner?: string;
  actionLabel?: string;
  order: number;
  dueInDays: number;
  tone: ComplianceTone;
}

export interface CreateUpdateComplianceFilingDto {
  title: string;
  detail?: string | null;
  dueDate: string;
  status: ComplianceFilingStatus;
  owner?: string | null;
  actionLabel?: string | null;
  order: number;
}

export interface ComplianceFilingStatsDto {
  dueThisMonth: number;
  nextDueDays?: number | null;
  onTime: number;
  onTimeTotal: number;
  lateCount: number;
  lateTitle?: string;
  lateDetail?: string;
}

export interface ComplianceCertificateDto {
  id: number;
  productName: string;
  category?: string;
  supplier?: string;
  badges: string[];
  testReport?: string;
  validUntil?: string | null;
  fileCount: number;
  tone: ComplianceTone;
  validText: string;
  needsAttention: boolean;
}

export interface CreateUpdateComplianceCertificateDto {
  productName: string;
  category?: string | null;
  supplier?: string | null;
  badges: string[];
  testReport?: string | null;
  validUntil?: string | null;
}

export interface ComplianceListItemDto {
  id: number;
  kind: ComplianceItemKind;
  title: string;
  description?: string;
  extra?: string;
  color?: string;
  number?: number | null;
  flag: boolean;
  order: number;
}

export interface CreateUpdateComplianceListItemDto {
  kind: ComplianceItemKind;
  title: string;
  description?: string | null;
  extra?: string | null;
  color?: string | null;
  number?: number | null;
  flag: boolean;
  order: number;
}

export interface ComplianceDocCellDto {
  docTypeId: number;
  status: ComplianceDocStatus;
}

export interface ComplianceProjectPackDto {
  projectId: number;
  code: string;
  name: string;
  stageName: string;
  stageColor: string;
  isHandedOver: boolean;
  donePercent: number;
  cells: ComplianceDocCellDto[];
}

export interface ComplianceFileDto {
  id: number;
  ownerKind: ComplianceFileOwner;
  ownerId: number;
  fileName: string;
  url: string;
  sizeBytes: number;
  creationTime: string;
}

export interface AddComplianceFileDto {
  ownerKind: ComplianceFileOwner;
  ownerId: number;
  fileName: string;
  url: string;
  sizeBytes: number;
  replaceExisting: boolean;
}

export interface ComplianceSectionSummaryDto {
  done: number;
  total: number;
  note: string;
  tone: 'red' | 'amber' | 'green';
  badge: number;
}

export interface ComplianceSummaryDto {
  statusKey: 'good' | 'almost' | 'bad';
  statusDetail: string;
  licences: ComplianceSectionSummaryDto;
  filings: ComplianceSectionSummaryDto;
  certificates: ComplianceSectionSummaryDto;
  packs: ComplianceSectionSummaryDto;
  alert?: { text: string; licenceId?: number | null } | null;
}

export interface CompliancePageDto {
  setting: ComplianceSettingDto;
  summary: ComplianceSummaryDto;
  licences: ComplianceLicenceDto[];
  filings: ComplianceFilingDto[];
  filingStats: ComplianceFilingStatsDto;
  certificates: ComplianceCertificateDto[];
  items: ComplianceListItemDto[];
  packs: ComplianceProjectPackDto[];
  files: ComplianceFileDto[];
}
