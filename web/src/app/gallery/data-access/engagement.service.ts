import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CreateEngagementRequest, EngagementResponse } from '../models/gallery.models';

@Injectable({ providedIn: 'root' })
export class EngagementService {
  private readonly api = inject(ApiService);

  createEngagement(request: CreateEngagementRequest): Observable<EngagementResponse> {
    return this.api.post<EngagementResponse>('engagements', request);
  }
}
