import axios, { AxiosInstance } from 'axios'

export interface Prospect {
  prospectId: number  // Backend returns int, JavaScript receives as number
  firstName: string
  lastName: string
  email: string
  phone: string
  contacts: number
  status: string
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateProspectRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string
  notes?: string
}

export interface UpdateProspectRequest {
  prospectId: string
  firstName?: string
  lastName?: string
  email?: string
  phone?: string
  notes?: string
}

class ProspectsApi {
  private client: AxiosInstance

  constructor() {
    // Use API Gateway URL from Vite environment variable or fallback to relative path
    const apiUrl = (import.meta as any).env?.VITE_API_URL || '/api'

    this.client = axios.create({
      baseURL: apiUrl,
    })

    // Add JWT token to all requests
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('jwt_token')
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    // Handle 401 errors by clearing token and forcing logout
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          console.warn('[ProspectsApi] 401 Unauthorized - Token expired or invalid. Logging out.')
          localStorage.removeItem('jwt_token')
          sessionStorage.removeItem('auth_redirect')
          // Dispatch custom event to trigger app logout
          window.dispatchEvent(new CustomEvent('auth:logout'))
        }
        return Promise.reject(error)
      }
    )
  }

  async getProspects(): Promise<Prospect[]> {
    const response = await this.client.get<Prospect[]>('/prospects')
    return response.data
  }

  async getProspect(prospectId: string): Promise<Prospect> {
    const response = await this.client.get<Prospect>(`/prospects/${prospectId}`)
    return response.data
  }

  async createProspect(request: CreateProspectRequest): Promise<void> {
    await this.client.post('/prospects', request)
  }

  async updateProspect(request: UpdateProspectRequest): Promise<void> {
    await this.client.put(`/prospects/${request.prospectId}`, request)
  }
}

export const prospectsApi = new ProspectsApi()
