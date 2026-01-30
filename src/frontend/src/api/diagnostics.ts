import axios from 'axios';

// Use API Gateway URL to proxy to services
const API_GATEWAY_URL = (import.meta as any).env?.VITE_API_URL || 'https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/api';

export interface OutboxMessage {
  id: number;
  eventId: string;
  eventType: string;
  createdAt: string;
  published: boolean;
  publishedAt: string | null;
  payloadPreview: string;
}

export interface OutboxStats {
  total: number;
  published: number;
  pending: number;
}

export interface OutboxResponse {
  total: number;
  messages: OutboxMessage[];
}

export interface ServiceOutboxData {
  serviceName: string;
  stats: OutboxStats;
  messages: OutboxMessage[];
  error?: string;
}

export const diagnosticsApi = {
  // Get outbox stats for a service
  getOutboxStats: async (serviceUrl: string): Promise<OutboxStats> => {
    const response = await axios.get<OutboxStats>(`${serviceUrl}/api/diagnostics/outbox/stats`);
    return response.data;
  },

  // Get outbox messages for a service
  getOutboxMessages: async (_serviceUrl: string): Promise<OutboxResponse> => {
    // Use API Gateway's outbox endpoint instead of calling services directly
    const token = localStorage.getItem('jwt_token');
    const response = await axios.get<any[]>(`${API_GATEWAY_URL}/outbox`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    });

    // Transform API Gateway response to match expected format
    return {
      total: response.data.length,
      messages: response.data.map(msg => ({
        id: 0, // Not used from aggregated view
        eventId: msg.id,
        eventType: msg.eventType,
        createdAt: msg.createdAt,
        published: msg.published,
        publishedAt: msg.publishedAt,
        payloadPreview: msg.payload ? msg.payload.substring(0, 100) : ''
      }))
    };
  },

  // Get all outbox data from all services
  getAllOutboxData: async (): Promise<ServiceOutboxData[]> => {
    try {
      const token = localStorage.getItem('jwt_token');
      const response = await axios.get<any[]>(`${API_GATEWAY_URL}/outbox`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {}
      });

      const messages = response.data.map(msg => ({
        id: 0,
        eventId: msg.id,
        eventType: msg.eventType,
        createdAt: msg.createdAt,
        published: msg.published,
        publishedAt: msg.publishedAt,
        payloadPreview: msg.payload ? msg.payload.substring(0, 100) : ''
      }));

      return [{
        serviceName: 'ProspectService',
        stats: {
          total: messages.length,
          published: messages.filter(m => m.published).length,
          pending: messages.filter(m => !m.published).length
        },
        messages
      }];
    } catch (error) {
      console.error('[DiagnosticsApi] Failed to fetch outbox:', error);
      return [{
        serviceName: 'ProspectService',
        stats: { total: 0, published: 0, pending: 0 },
        messages: [],
        error: error instanceof Error ? error.message : 'Failed to fetch outbox data'
      }];
    }
  },
};
