import axios from 'axios';

const PROSPECT_API = 'http://localhost:5110';
const STUDENT_API = 'http://localhost:5120';
const INSTRUCTOR_API = 'http://localhost:5130';

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
  getOutboxMessages: async (serviceUrl: string): Promise<OutboxResponse> => {
    const response = await axios.get<OutboxResponse>(`${serviceUrl}/api/diagnostics/outbox`);
    return response.data;
  },

  // Get all outbox data from all services
  getAllOutboxData: async (): Promise<ServiceOutboxData[]> => {
    const services = [
      { name: 'ProspectService', url: PROSPECT_API },
      { name: 'StudentService', url: STUDENT_API },
      { name: 'InstructorService', url: INSTRUCTOR_API },
    ];

    const results = await Promise.allSettled(
      services.map(async (service) => {
        try {
          const [stats, outbox] = await Promise.all([
            diagnosticsApi.getOutboxStats(service.url),
            diagnosticsApi.getOutboxMessages(service.url),
          ]);

          return {
            serviceName: service.name,
            stats,
            messages: outbox.messages,
          };
        } catch (error) {
          return {
            serviceName: service.name,
            stats: { total: 0, published: 0, pending: 0 },
            messages: [],
            error: error instanceof Error ? error.message : 'Unknown error',
          };
        }
      })
    );

    return results.map((result) =>
      result.status === 'fulfilled' ? result.value : result.reason
    );
  },
};
