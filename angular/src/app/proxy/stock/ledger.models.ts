export enum LedgerMovement {
  Opening = 1, Received = 2, Sold = 3, UsedOnJob = 4, TransferredOut = 5, TransferredIn = 6,
  CountCorrection = 7, Damaged = 8, ReturnedToSupplier = 9, Lost = 10, InternalUse = 11, Reversal = 12, Other = 13,
}

export enum LedgerSource { Screen = 1, Pos = 2, Storefront = 3, Api = 4, Import = 5, System = 6 }

/** Several can apply to one line, so this is a bit field. */
export enum LedgerFlags {
  None = 0, NoReason = 1, NoPhoto = 2, QuicklyReversed = 4, OutsideHours = 8,
  WouldGoBelowZero = 16, LargeValue = 32, SerialNotReceived = 64, NotApproved = 128,
}

export const MOVEMENTS: { value: LedgerMovement; label: string }[] = [
  { value: LedgerMovement.Received, label: 'Received' },
  { value: LedgerMovement.Sold, label: 'Sold' },
  { value: LedgerMovement.UsedOnJob, label: 'Used on job' },
  { value: LedgerMovement.TransferredIn, label: 'Transferred in' },
  { value: LedgerMovement.TransferredOut, label: 'Transferred out' },
  { value: LedgerMovement.CountCorrection, label: 'Count correction' },
  { value: LedgerMovement.Damaged, label: 'Damaged' },
  { value: LedgerMovement.ReturnedToSupplier, label: 'Returned to supplier' },
  { value: LedgerMovement.Lost, label: 'Lost or stolen' },
  { value: LedgerMovement.InternalUse, label: 'Used internally' },
  { value: LedgerMovement.Reversal, label: 'Reversal' },
  { value: LedgerMovement.Opening, label: 'Opening count' },
];

export interface LedgerHeaderDto {
  linesToday: number;
  peopleToday: number;
  machinesToday: number;
  inUnits: number;
  inProducts: number;
  outUnits: number;
  handCorrections: number;
  flaggedOpen: number;
  oldestFlaggedDays?: number | null;
  totalLines: number;
  lastCheckAt?: string | null;
  lastCheckOk: boolean;
  lastCheckLines: number;
}

export interface GetLedgerLinesInput {
  filter?: string;
  movement?: LedgerMovement;
  productId?: number;
  warehouseId?: number;
  userId?: string;
  flaggedOnly?: boolean;
  days?: number;
  skipCount: number;
  maxResultCount: number;
}

export interface LedgerLineDto {
  id: number;
  time: string;
  productId: number;
  productName: string;
  sku?: string | null;
  warehouseName: string;
  movement: LedgerMovement;
  movementLabel: string;
  change: number;
  quantityBefore: number;
  quantityAfter: number;
  userName: string;
  ipAddress?: string | null;
  documentNumber?: string | null;
  flags: LedgerFlags;
  flagLabels: string[];
  reviewed: boolean;
}

export interface LedgerLinesPageDto { totalCount: number; items: LedgerLineDto[]; }

export interface LedgerFieldChangeDto { field: string; was: string; became: string; changed: boolean; }
export interface LedgerPaperDto { icon: string; title: string; detail?: string | null; link: 'stock-entry' | 'ledger-line' | 'none'; linkId?: number | null; }
export interface LedgerReviewDto { time: string; userName: string; action: string; note?: string | null; }

export interface LedgerLineDetailDto {
  line: LedgerLineDto;
  time: string;
  timeZone?: string | null;
  warehouseName: string;
  unitCost: number;
  valueBefore: number;
  valueAfter: number;
  serials: string[];
  reason?: string | null;
  userId?: string | null;
  userEmail?: string | null;
  userRole?: string | null;
  signedInAt?: string | null;
  signedInText?: string | null;
  twoStepUsed: boolean;
  approvedByName?: string | null;
  neededApproval: boolean;
  ipAddress?: string | null;
  device?: string | null;
  sessionId?: string | null;
  cameFrom?: string | null;
  source: LedgerSource;
  sourceLabel: string;
  changes: LedgerFieldChangeDto[];
  paper: LedgerPaperDto[];
  reviews: LedgerReviewDto[];
  hash: string;
  previousHash: string;
  sealOk: boolean;
  serverName?: string | null;
  requestId?: string | null;
  previousLineId?: number | null;
  nextLineId?: number | null;
}

export interface LedgerProductOptionDto { id: number; name: string; sku?: string | null; }
export interface LedgerBalancePointDto { date: string; quantity: number; in: number; out: number; }
export interface LedgerCostLayerDto { lineId: number; date: string; title: string; detail?: string | null; quantity: number; unitCost: number; }

export interface LedgerProductDto {
  product: LedgerProductOptionDto;
  tracksSerials: boolean;
  inStockNow: number;
  whereText: string;
  linesInRange: number;
  movesIn: number;
  movesOut: number;
  handCorrections: number;
  handCorrectionsBy?: string | null;
  stockValue: number;
  averageCost: number;
  reorderLevel?: number | null;
  balance: LedgerBalancePointDto[];
  lines: LedgerLineDto[];
  receipts: LedgerCostLayerDto[];
}

export interface LedgerFlaggedDto {
  line: LedgerLineDto;
  title: string;
  text: string;
  action: 'photo' | 'both-lines' | 'open' | 'investigate' | 'reset';
  actionLabel: string;
  pairLineId?: number | null;
}

export interface LedgerHandCorrectorDto { userId?: string | null; name: string; role?: string | null; count: number; value: number; percent: number; }

export interface LedgerNeedsLookDto {
  lines: LedgerFlaggedDto[];
  openCount: number;
  oldestDays?: number | null;
  oldestText?: string | null;
  correctors: LedgerHandCorrectorDto[];
  correctorNote?: string | null;
  setting: LedgerSettingDto;
}

export interface LedgerPersonDto {
  id: string;
  name: string;
  email?: string | null;
  role: string;
  mayDo: string;
  lastSignedIn?: string | null;
  lastIp?: string | null;
  lastDevice?: string | null;
  twoStep: boolean;
  linesPosted: number;
}

export interface LedgerSignInAttemptDto {
  time: string;
  who: string;
  what: string;
  ip?: string | null;
  device?: string | null;
  tone: 'ok' | 'blocked' | 'warn';
  result: string;
}

export interface LedgerMachineDto { source: LedgerSource; name: string; detail: string; lines: number; lastUsed?: string | null; }

export interface LedgerAccessDto {
  people: LedgerPersonDto[];
  attempts: LedgerSignInAttemptDto[];
  machines: LedgerMachineDto[];
  setting: LedgerSettingDto;
  securityLogsAvailable: boolean;
}

export interface LedgerSealDto { lineId: number; title: string; time: string; hash: string; previousHash: string; isFirst: boolean; }

export interface LedgerCheckResultDto {
  ok: boolean;
  linesChecked: number;
  time: string;
  durationMs: number;
  firstBadLineId?: number | null;
  message: string;
}

export interface LedgerProofDto {
  seals: LedgerSealDto[];
  lastCheck?: LedgerCheckResultDto | null;
  totalLines: number;
  setting: LedgerSettingDto;
  lastExportAt?: string | null;
  lastExportBy?: string | null;
}

export interface LedgerSettingDto {
  flagNoReason: boolean;
  flagNoPhoto: boolean;
  flagQuicklyReversed: boolean;
  quicklyReversedMinutes: number;
  flagOutsideHours: boolean;
  workingFromHour: number;
  workingToHour: number;
  flagBelowZero: boolean;
  flagLargeValue: boolean;
  largeValueOver: number;
  askPasswordAgain: boolean;
  twoPeopleForBigWriteOffs: boolean;
  requireTwoStep: boolean;
  onlyFromOfficeNetworks: boolean;
  allowedNetworks?: string | null;
  emailOwnerOnReversal: boolean;
  keepYears: number;
}

export interface ReviewLedgerLineDto { action: 'checked' | 'asked' | 'reversed'; note?: string | null; }
