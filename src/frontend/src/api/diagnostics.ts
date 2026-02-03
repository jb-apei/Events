import axios from 'axios';

// Use API Gateway URL to proxy to services
const API_GATEWAY_URL = (import.meta as any).env?.VITE_API_URL || 'https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io/api';

export interface OutboxMessage {
  id: number;
  eventId: string;
  eventType: string;
  createdAt: string;
  published: boolean;
  publishedAt: string | null;
  payload: string;
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

export interface ServiceBusMessage {
  messageId: string;
  sequenceNumber: number;
  enqueuedTime: string;
  subject: string;
  correlationId: string;
  deliveryCount: number;
  body: string;
  deadLetterReason?: string;
  deadLetterErrorDescription?: string;
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
        id: msg.id,
        eventId: msg.eventId,
        eventType: msg.eventType,
        createdAt: msg.createdAt,
        published: msg.published,
        publishedAt: msg.publishedAt,
        payload: msg.payload || ''
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
        id: msg.id,
        eventId: msg.eventId,
        eventType: msg.eventType,
        createdAt: msg.createdAt,
        published: msg.published,
        publishedAt: msg.publishedAt,
        payload: msg.payload || ''
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
  // Get Service Bus Queue Messages
  getQueueMessages: async (): Promise<ServiceBusMessage[]> => {
    const token = localStorage.getItem('jwt_token');
    const response = await axios.get<ServiceBusMessage[]>(`${API_GATEWAY_URL}/outbox/queue`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    });
    return response.data;
  },

  // Get Service Bus DLQ Messages
  getDlqMessages: async (): Promise<ServiceBusMessage[]> => {
    const token = localStorage.getItem('jwt_token');
    const response = await axios.get<ServiceBusMessage[]>(`${API_GATEWAY_URL}/outbox/dlq`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    });
    return response.data;
  },};
