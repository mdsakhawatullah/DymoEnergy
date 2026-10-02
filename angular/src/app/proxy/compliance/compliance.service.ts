import { RestService } from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  AddComplianceFileDto,
  ComplianceCertificateDto,
  ComplianceFileDto,
  ComplianceFilingDto,
  ComplianceLicenceDto,
  ComplianceListItemDto,
  CompliancePageDto,
  ComplianceSettingDto,
  ComplianceDocStatus,
  ComplianceFilingStatus,
  CreateUpdateComplianceCertificateDto,
  CreateUpdateComplianceFilingDto,
  CreateUpdateComplianceLicenceDto,
  CreateUpdateComplianceListItemDto,
  UpdateComplianceSettingDto,
} from './models';

const BASE = '/api/app/compliance';

@Injectable({ providedIn: 'root' })
export class ComplianceService {
  apiName = 'Default';
  private readonly filesUrl = `${environment.apis['default'].url}/api/app/compliance-files`;

  constructor(
    private restService: RestService,
    private http: HttpClient,
  ) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  getPage = () => this.req<CompliancePageDto>({ method: 'GET', url: `${BASE}/page` });

  updateSetting = (input: UpdateComplianceSettingDto) =>
    this.req<ComplianceSettingDto>({ method: 'PUT', url: `${BASE}/setting`, body: input });

  // ── Licences ────────────────────────────────────────────────────────────
  createLicence = (input: CreateUpdateComplianceLicenceDto) =>
    this.req<ComplianceLicenceDto>({ method: 'POST', url: `${BASE}/licence`, body: input });
  updateLicence = (id: number, input: CreateUpdateComplianceLicenceDto) =>
    this.req<ComplianceLicenceDto>({ method: 'PUT', url: `${BASE}/${id}/licence`, body: input });
  deleteLicence = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/licence` });

  // ── Filings ─────────────────────────────────────────────────────────────
  createFiling = (input: CreateUpdateComplianceFilingDto) =>
    this.req<ComplianceFilingDto>({ method: 'POST', url: `${BASE}/filing`, body: input });
  updateFiling = (id: number, input: CreateUpdateComplianceFilingDto) =>
    this.req<ComplianceFilingDto>({ method: 'PUT', url: `${BASE}/${id}/filing`, body: input });
  updateFilingStatus = (id: number, status: ComplianceFilingStatus) =>
    this.req<ComplianceFilingDto>({ method: 'PUT', url: `${BASE}/${id}/filing-status`, body: { status } });
  deleteFiling = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/filing` });

  // ── Certificates ────────────────────────────────────────────────────────
  createCertificate = (input: CreateUpdateComplianceCertificateDto) =>
    this.req<ComplianceCertificateDto>({ method: 'POST', url: `${BASE}/certificate`, body: input });
  updateCertificate = (id: number, input: CreateUpdateComplianceCertificateDto) =>
    this.req<ComplianceCertificateDto>({ method: 'PUT', url: `${BASE}/${id}/certificate`, body: input });
  deleteCertificate = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/certificate` });

  // ── Generic lists ───────────────────────────────────────────────────────
  createItem = (input: CreateUpdateComplianceListItemDto) =>
    this.req<ComplianceListItemDto>({ method: 'POST', url: `${BASE}/item`, body: input });
  updateItem = (id: number, input: CreateUpdateComplianceListItemDto) =>
    this.req<ComplianceListItemDto>({ method: 'PUT', url: `${BASE}/${id}/item`, body: input });
  deleteItem = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/item` });

  // ── Project packs ───────────────────────────────────────────────────────
  setProjectDocument = (projectId: number, docTypeId: number, status: ComplianceDocStatus) =>
    this.req<void>({ method: 'POST', url: `${BASE}/set-project-document`, body: { projectId, docTypeId, status } });

  // ── Files ───────────────────────────────────────────────────────────────
  addFile = (input: AddComplianceFileDto) =>
    this.req<ComplianceFileDto>({ method: 'POST', url: `${BASE}/file`, body: input });
  deleteFile = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/file` });

  uploadFile(file: File): Observable<{ url: string; fileName: string; sizeBytes: number }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string; fileName: string; sizeBytes: number }>(`${this.filesUrl}/upload`, form);
  }

  downloadAll(): Observable<Blob> {
    return this.http.get(`${this.filesUrl}/download-all`, { responseType: 'blob' });
  }
}
